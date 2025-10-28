Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Data
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Linq

' ===== Servicio =====
Public Class CreaCliente

    Public Enum TipoCliente
        PF = 1
        PJ = 2
    End Enum

    ' ===== Config =====
    Public Property BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH_CREATE As String = "/BRMCustCustomServices/resources/BRMAccountCustomServicesREST/createCustomer"
    Private Const PROTOCOL_PREFIX As String = "ORACLE_SAP_TEST_"

    ' ===== Dependencias =====
    Private Shared ReadOnly _http As HttpClient = New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
    Private ReadOnly _db As BrmOracleQuery = New BrmOracleQuery()

    ' ===== Logging =====
    Private _logger As IAppLogger

    Public Property Logger As IAppLogger
        Get
            Return _logger
        End Get
        Set(value As IAppLogger)
            _logger = value
        End Set
    End Property

    ' ===== Telemetría/OUT =====
    Public Property LastRequestJson As String
    Public Property LastResponseBody As String
    Public Property LastHttpStatus As Integer?
    Public Property ErrorMessage As String
    Public Property OnOut As Action(Of String) ' (el Form asigna AddressOf OutSink)
    Public Property LastTipoSolicitado As Nullable(Of TipoCliente)
    Public Property LastTipoResuelto As Nullable(Of TipoCliente)
    Public Property LastDocumentoGenerado As String
    Public Property LastDocumentoEsPF As Nullable(Of Boolean)

    Private Sub OUT(line As String)
        Try
            If OnOut IsNot Nothing Then OnOut.Invoke(line)
        Catch
        End Try
    End Sub

    ' ===== API =====
    Public Async Function CrearAsync(tipo As TipoCliente,
                                     Optional ufPreferida As String = Nothing,
                                     Optional persist As Boolean = True) As Task(Of CrearClienteResult)

        Dim result As New CrearClienteResult()

        Try
            LastTipoSolicitado = tipo
            LastTipoResuelto = Nothing
            LastDocumentoGenerado = Nothing
            LastDocumentoEsPF = Nothing

            OUT("[CREATE][TRACE] TipoCliente argumento bruto: " & tipo.ToString())
            Dim resolvedTipo As TipoCliente = tipo
            If Not [Enum].IsDefined(GetType(TipoCliente), resolvedTipo) Then
                OUT("[CREATE][WARN] TipoCliente no reconocido (" & CInt(tipo).ToString() & "), usando PF por defecto.")
                resolvedTipo = TipoCliente.PF
            End If

            LastTipoResuelto = resolvedTipo

            OUT("[CREATE][FLOW] TipoCliente recibido: " & resolvedTipo.ToString())

            ' 1) Payload exacto
            Dim payload As JObject = Await BuildPayloadAsync(resolvedTipo, ufPreferida).ConfigureAwait(False)
            Dim json As String = payload.ToString(Formatting.None)
            LastDocumentoGenerado = payload.Value(Of String)("AC_FLD_CPF_CNPJ")
            LastDocumentoEsPF = (resolvedTipo = TipoCliente.PF)

            Dim docLength As Integer = If(LastDocumentoGenerado Is Nothing, 0, LastDocumentoGenerado.Length)
            If resolvedTipo = TipoCliente.PF AndAlso docLength <> 11 Then
                OUT("[CREATE][WARN] Longitud inesperada para CPF: " & docLength.ToString())
            ElseIf resolvedTipo = TipoCliente.PJ AndAlso docLength <> 14 Then
                OUT("[CREATE][WARN] Longitud inesperada para CNPJ: " & docLength.ToString())
            Else
                OUT("[CREATE][CHECK] Documento generado: " & LastDocumentoGenerado & " (len=" & docLength.ToString() & ")")
            End If

            Dim pjFields As String() = {"AC_FLD_INSCRICAO_ESTADUAL", "AC_FLD_REPRESENTANTE_LEGAL", "AC_FLD_REPRESENTANTE_LEGAL_CPF", "AC_FLD_CNAE_CLIENTE"}
            If resolvedTipo = TipoCliente.PJ Then
                Dim missing = pjFields.Where(Function(f) Not payload.ContainsKey(f) OrElse String.IsNullOrWhiteSpace(payload.Value(Of String)(f))).ToArray()
                If missing.Length > 0 Then
                    OUT("[CREATE][WARN] Campos PJ faltantes o vacíos: " & String.Join(", ", missing))
                Else
                    OUT("[CREATE][CHECK] Campos PJ presentes: " & String.Join(", ", pjFields))
                End If
            Else
                Dim stray = pjFields.Where(Function(f) payload.ContainsKey(f)).ToArray()
                If stray.Length > 0 Then
                    OUT("[CREATE][WARN] Campos PJ detectados para PF: " & String.Join(", ", stray))
                End If
            End If

            LastRequestJson = json
            LogRequest(json)
            OUT(">>> [CREATE][JSON] payload disponible en Log_Debug.")

            If Not persist Then
                result.Success = True
                result.Documento = payload.Value(Of String)("AC_FLD_CPF_CNPJ")
                result.ProtocolId = payload.Value(Of String)("AC_FLD_PROTOCOL_ID")
                result.AccountPoid = String.Empty
                OUT("[CREATE][DRY-RUN] persist=False, POST omitido.")
                Return result
            End If

            ' 2) POST
            Dim endpoint As String = BASE_URL.TrimEnd("/"c) & PATH_CREATE
            Using req As New HttpRequestMessage(HttpMethod.Post, endpoint)
                req.Headers.Accept.Clear()
                req.Headers.Accept.ParseAdd("application/json")
                req.Content = New StringContent(json, Encoding.UTF8, "application/json")

                Using resp = Await _http.SendAsync(req).ConfigureAwait(False)
                    Dim body As String = Await resp.Content.ReadAsStringAsync().ConfigureAwait(False)
                    LastHttpStatus = CInt(resp.StatusCode)
                    LastResponseBody = body

                    OUT("<<< [CREATE][HTTP] " & LastHttpStatus.GetValueOrDefault().ToString())
                    OUT("<<< [CREATE][RESP] respuesta disponible en Log_Debug.")
                    If _logger IsNot Nothing Then
                        Try
                            _logger.LogJson(body, "CREATE_RESPONSE")
                        Catch
                        End Try
                    End If

                    ' 3) Validar en BD por CPF/CNPJ
                    Dim doc As String = payload.Value(Of String)("AC_FLD_CPF_CNPJ")
                    result.Documento = doc
                    result.ProtocolId = payload.Value(Of String)("AC_FLD_PROTOCOL_ID")
                    Dim poid As String = String.Empty
                    Const maxValidationAttempts As Integer = 5
                    For attempt As Integer = 1 To maxValidationAttempts
                        poid = ObtenerPoidPorDocumento(doc)
                        If Not String.IsNullOrWhiteSpace(poid) Then
                            Dim successMsg As String = String.Format("[CREATE][CHECK] intento {0}: confirmado account_poid.", attempt)
                            OUT(successMsg)
                            If _logger IsNot Nothing Then
                                Try
                                    _logger.LogData(New With {
                                        .Operacion = "CrearCliente.ValidacionCuenta",
                                        .Intento = attempt,
                                        .AccountPoid = poid
                                    }, "CREATE_CHECK")
                                Catch
                                End Try
                            End If
                            Exit For
                        End If

                        Dim pendingMsg As String = String.Format("[CREATE][CHECK] intento {0} sin confirmación. Esperando 2s...", attempt)
                        OUT(pendingMsg)
                        If _logger IsNot Nothing Then
                            Try
                                _logger.LogData(New With {
                                    .Operacion = "CrearCliente.ValidacionCuenta",
                                    .Intento = attempt,
                                    .Estado = "Retry",
                                    .Documento = doc
                                }, "CREATE_CHECK")
                            Catch
                            End Try
                        End If

                        If attempt < maxValidationAttempts Then
                            Await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(False)
                        End If
                    Next

                    If String.IsNullOrWhiteSpace(poid) Then
                        OUT("[CREATE][ERROR] AccountPoid no confirmado tras múltiples intentos.")
                        If _logger IsNot Nothing Then
                            Try
                                _logger.LogError("AccountPoid no disponible tras validación en base de datos.", Nothing, New With {
                                    .Operacion = "CrearCliente.ValidacionCuenta",
                                    .Intentos = maxValidationAttempts,
                                    .Documento = doc
                                })
                            Catch
                            End Try
                        End If
                    End If

                    result.AccountPoid = poid
                    result.HttpStatus = LastHttpStatus
                    result.RawBody = LastResponseBody
                    result.Success = (Not String.IsNullOrWhiteSpace(poid))
                End Using
            End Using

        Catch ex As Exception
            ErrorMessage = ex.Message
            OUT("[CREATE][ERROR] " & ErrorMessage)
            If _logger IsNot Nothing Then
                Try
                    _logger.LogError(ErrorMessage, ex, New With {.Operacion = "CrearCliente"})
                Catch
                End Try
            End If
            result.Success = False
            result.HttpStatus = LastHttpStatus
            result.RawBody = LastResponseBody
        End Try

        Return result
    End Function

    ' ===== Build JSON =====
    Private Async Function BuildPayloadAsync(tipo As TipoCliente, ufPreferida As String) As Task(Of JObject)
        Dim seed = PickSeedRow(ufPreferida)
        Dim name As String = If(String.IsNullOrWhiteSpace(seed.Name), "ORLANDO ROMERO", seed.Name.ToUpperInvariant())
        Dim email As String = If(String.IsNullOrWhiteSpace(seed.Email), "demo@nio.local", seed.Email)
        Dim phoneDigits As String = SoloDigitos(If(String.IsNullOrWhiteSpace(seed.Phone), "47998555123", seed.Phone))
        Dim ufSeed As String = If(String.IsNullOrWhiteSpace(seed.UF), "DF", seed.UF.ToUpperInvariant())
        Dim uf As String = If(String.IsNullOrWhiteSpace(ufPreferida), ufSeed, ufPreferida.ToUpperInvariant())
        Dim city As String = If(String.IsNullOrWhiteSpace(seed.City), "BRASILIA", seed.City.ToUpperInvariant())
        Dim zipRaw As String = If(String.IsNullOrWhiteSpace(seed.Zip), "70040900", seed.Zip)
        Dim zip As String = SoloDigitos(zipRaw)
        If String.IsNullOrWhiteSpace(zip) Then zip = "70040900"
        Dim streetType As String = If(String.IsNullOrWhiteSpace(seed.StreetType), "RUA", seed.StreetType.ToUpperInvariant())
        Dim streetName As String = If(String.IsNullOrWhiteSpace(seed.StreetName), "ODILON AUTO", seed.StreetName.ToUpperInvariant())
        Dim addressNumber As String = If(String.IsNullOrWhiteSpace(seed.AddressNumber), "120", seed.AddressNumber.Trim())
        Dim neighborhood As String = If(String.IsNullOrWhiteSpace(seed.Neighborhood), "JARDIM CARAPINA", seed.Neighborhood.ToUpperInvariant())
        Dim complement As String = If(String.IsNullOrWhiteSpace(seed.AddressComplement), String.Empty, seed.AddressComplement.ToUpperInvariant())

        Dim protocol As String = GenerateUniqueProtocolId(PROTOCOL_PREFIX)
        Dim isPF As Boolean = (tipo = TipoCliente.PF)
        Dim doc As String = If(isPF, GenerarCPFValidoDemo(), GenerarCNPJValidoDemo())

        OUT("[CREATE][BUILD] TipoCliente=" & If(isPF, "PF", "PJ") & " Documento=" & doc)

        ' Address (pipe-fixed 8 partes)
        Dim addressParts As String() = {
            zip,
            streetType,
            streetName,
            addressNumber,
            neighborhood,
            complement,
            String.Empty,
            String.Empty
        }
        Dim addressPipe As String = String.Join("|", addressParts)

        Dim o As New JObject()
        o.Add("AC_FLD_PROTOCOL_ID", protocol)
        o.Add("BUSINESS_TYPE", If(isPF, "1", "2"))
        o.Add("NAMEINFO", name)
        o.Add("ADDRESS", addressPipe)
        o.Add("ZIP", zip)
        o.Add("CITY", city)
        o.Add("STATE", uf)
        o.Add("COUNTRY", "Brasil")
        o.Add("PHONES", phoneDigits)
        o.Add("PHONE_TYPE", "4")
        o.Add("AC_FLD_SEGMENT", "Warm")
        o.Add("AC_FLD_CPF_CNPJ", doc)
        o.Add("COUNTRY_CODE", "")
        o.Add("CITY_CODE", "")
        o.Add("AC_FLD_ADDRESS_NUMBER", addressNumber)
        o.Add("AC_FLD_NEIGHBORHOOD", neighborhood)
        o.Add("AC_FLD_BIRTHDAY_T", "1990-05-06")
        o.Add("AC_FLD_GENDER", "Masculino")
        o.Add("AC_FLD_EMAIL", email)

        If Not isPF Then
            o.Add("AC_FLD_INSCRICAO_ESTADUAL", "326749879")
            o.Add("AC_FLD_REPRESENTANTE_LEGAL", name)
            o.Add("AC_FLD_REPRESENTANTE_LEGAL_CPF", GenerarCPFValidoDemo())
            o.Add("AC_FLD_CNAE_CLIENTE", "4530701")
        End If

        Await Task.Yield()
        Return o
    End Function

    ' ===== BD =====
    Private Function ObtenerPoidPorDocumento(doc As String) As String
        Try
            Dim sql As String =
                "SELECT c.poid_id0 AS account_poid
                FROM PIN.PROFILE_T P, PIN.AC_PROFILE_ACCOUNT_T PA, PIN.ACCOUNT_NAMEINFO_T A, PIN.ACCOUNT_T C
                WHERE P.ACCOUNT_OBJ_ID0 = A.OBJ_ID0 AND A.OBJ_ID0 = C.POID_ID0
                AND PA.OBJ_ID0 = P.POID_ID0
                AND PA.CPF_CNPJ = :p_doc
                ORDER BY c.poid_id0 DESC
                FETCH FIRST 1 ROWS ONLY"
            Dim pars As New Dictionary(Of String, Object) From {{":p_doc", doc}}
            Dim dt As DataTable = _db.ExecuteDataTable(sql, pars, 30)
            If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                Dim id As Long = Convert.ToInt64(dt.Rows(0)("account_poid"))
                Return "0.0.0.1 /account " & id.ToString() & " 0"
            End If
        Catch ex As Exception
            ErrorMessage = "ObtenerPoidPorDocumento: " & ex.Message
            OUT("[DB][ERROR] " & ErrorMessage)
        End Try
        Return String.Empty
    End Function

    ' ===== Secuenciador de Protocolo =====
    Private Function GenerateUniqueProtocolId(prefix As String) As String
        Dim baseMax As Integer = GetMaxSuffixFromDb(prefix)
        If baseMax < 0 Then baseMax = 0
        Dim trySuffix As Integer = baseMax + 1
        For attempts As Integer = 0 To 300
            Dim candidate As String = prefix & trySuffix.ToString("0000")
            If Not ProtocoloExiste(candidate) Then
                OUT("[SEQ] next unique → " & candidate)
                Return candidate
            End If
            trySuffix += 1
        Next
        Dim fallback As String = prefix & DateTime.Now.ToString("mmss")
        OUT("[SEQ] fallback → " & fallback)
        Return fallback
    End Function

    Private Function GetMaxSuffixFromDb(prefix As String) As Integer
        Dim maxSuffix As Integer = -1
        Try
            Dim sql1 As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(ac_protocol_id),'([0-9]{4})$'))),0)
   FROM pin.ac_protocol_t
  WHERE ac_protocol_id LIKE :pfx || '%'"
            Dim v1 As Integer = _db.ExecuteScalar(Of Integer)(sql1, New Dictionary(Of String, Object) From {{":pfx", prefix}}, 15)
            maxSuffix = Math.Max(maxSuffix, v1)

            Dim sql2 As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(contract_id),'([0-9]{4})$'))),0)
   FROM pin.ac_profile_account_t
  WHERE contract_id LIKE :pfx || '%'"
            Dim v2 As Integer = _db.ExecuteScalar(Of Integer)(sql2, New Dictionary(Of String, Object) From {{":pfx", prefix}}, 15)
            maxSuffix = Math.Max(maxSuffix, v2)

            Dim sql3 As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(
       REGEXP_SUBSTR(input_json,
         '""AC_FLD_PROTOCOL_ID""[[:space:]]*:[[:space:]]*""' || :pfx || '([0-9]{4})""',
         1, 1, 'i', 1
       ),
       '([0-9]{4})$'
     ))),0)
   FROM pin.ac_interface_log_t
  WHERE input_json LIKE '%' || :pfx || '%'
    AND input_json LIKE '%AC_FLD_PROTOCOL_ID%'"
            Dim v3 As Integer = _db.ExecuteScalar(Of Integer)(sql3, New Dictionary(Of String, Object) From {{":pfx", prefix}}, 20)
            maxSuffix = Math.Max(maxSuffix, v3)

        Catch ex As Exception
            OUT("[SEQ][ERR] " & ex.Message)
            Return -1
        End Try

        OUT("[SEQ] max_suf BD (prefix=" & prefix & ") = " & maxSuffix.ToString())
        Return maxSuffix
    End Function

    Private Function ProtocoloExiste(candidate As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (SELECT 1 FROM pin.ac_protocol_t WHERE TRIM(ac_protocol_id) = :cand)
              OR EXISTS (SELECT 1 FROM pin.ac_profile_account_t WHERE TRIM(contract_id) = :cand)
           THEN 1 ELSE 0 END AS existe
  FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql, New Dictionary(Of String, Object) From {{":cand", candidate}}, 15)
            Return (v = 1)
        Catch
            Return True
        End Try
    End Function

    ' ===== Seeds =====
    Private Structure SeedRow
        Public Name As String
        Public StreetType As String
        Public StreetName As String
        Public Neighborhood As String
        Public City As String
        Public UF As String
        Public Zip As String
        Public AddressNumber As String
        Public AddressComplement As String
        Public Phone As String
        Public Email As String
    End Structure

    Private Shared ReadOnly _rnd As New Random()

    Private Function PickSeedRow(Optional ufPreferida As String = Nothing) As SeedRow
        Dim seeds = LoadSeeds()

        If seeds.Count = 0 Then
            Return New SeedRow With {
                .Name = "ORLANDO ROMERO",
                .StreetType = "Rua",
                .StreetName = "Odilon Auto",
                .Neighborhood = "Jardim Carapina",
                .City = "SERRA",
                .UF = "ES",
                .Zip = "29161709",
                .AddressNumber = "120",
                .AddressComplement = String.Empty,
                .Phone = "27998555123",
                .Email = "orlando.romero@example.com"
            }
        End If
        If Not String.IsNullOrWhiteSpace(ufPreferida) Then
            Dim targetUf As String = ufPreferida.Trim().ToUpperInvariant()
            For Each seed In seeds
                If String.Equals(seed.UF, targetUf, StringComparison.OrdinalIgnoreCase) Then
                    Return seed
                End If
            Next
        End If

        Return seeds(_rnd.Next(0, seeds.Count))
    End Function

    Private Function LoadSeeds() As System.Collections.Generic.List(Of SeedRow)
        Dim res As New System.Collections.Generic.List(Of SeedRow)
        Try
            Dim path As String = ResolveSeedsPath()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                OUT("[SEEDS] no encontrado: " & path)
                Return res
            End If
            OUT("[SEEDS] usando: " & path)
            Using sr As New StreamReader(path, Encoding.UTF8)
                Dim header As String = sr.ReadLine() ' salta cabecera
                While Not sr.EndOfStream
                    Dim line As String = sr.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While
                    Dim cols = ParseCsvLine(line)
                    If cols.Length >= 11 Then
                        Dim s As New SeedRow With {
                            .Name = cols(0),
                            .StreetType = cols(1),
                            .StreetName = cols(2),
                            .Neighborhood = cols(3),
                            .City = cols(4),
                            .UF = cols(5),
                            .Zip = cols(6),
                            .AddressNumber = cols(7),
                            .AddressComplement = cols(8),
                            .Phone = cols(9),
                            .Email = cols(10)
                        }
                        res.Add(s)
                    End If
                End While
            End Using
            OUT("[SEEDS] Cargadas: " & res.Count.ToString())
        Catch ex As Exception
            OUT("[SEEDS][ERROR] " & ex.Message)
        End Try
        Return res
    End Function

    Private Function ParseCsvLine(line As String) As String()
        Dim vals As New System.Collections.Generic.List(Of String)
        Dim sb As New StringBuilder()
        Dim inQ As Boolean = False
        For i As Integer = 0 To line.Length - 1
            Dim ch As Char = line(i)
            If ch = """"c Then
                inQ = Not inQ
            ElseIf ch = ","c AndAlso Not inQ Then
                vals.Add(sb.ToString())
                sb.Clear()
            Else
                sb.Append(ch)
            End If
        Next
        vals.Add(sb.ToString())
        Return vals.ToArray()
    End Function

    Private Function ResolveSeedsPath() As String
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim candidates As New System.Collections.Generic.List(Of String)
        candidates.Add(System.IO.Path.Combine(baseDir, "seeds_clientes_br.csv"))

        Dim current As String = baseDir
        For i As Integer = 0 To 3
            current = System.IO.Path.GetFullPath(System.IO.Path.Combine(current, ".."))
            candidates.Add(System.IO.Path.Combine(current, "seeds_clientes_br.csv"))
        Next

        For Each candidate In candidates
            If File.Exists(candidate) Then
                Return candidate
            End If
        Next

        Return candidates.FirstOrDefault()
    End Function

    Private Function SoloDigitos(s As String) As String
        If s Is Nothing Then Return ""
        Dim sb As New StringBuilder()
        For Each c As Char In s
            If Char.IsDigit(c) Then sb.Append(c)
        Next
        Return sb.ToString()
    End Function

End Class
