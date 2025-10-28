Option Strict On
Option Explicit On

Imports System
Imports System.Net.Http
Imports System.Text
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq

Public Class BolecodeResponse

    ' ===== Config =====
    Public Property BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH As String = "/BRMCustCustomServices/resources/BRMPaymentCustomServicesREST/bolecodeResponse"
    Private Const TOKEN_PREFIX As String = "03395942"
    Private Const TOKEN_SEED As String = "03395942700000000109019716900000001038855000"
    Private Const TYPEABLE_SEED As String = "03399356782060000000201234501011693970000000100"
    Private Const PAYTYPE_BOLETO As Integer = 2
    Private Const PAYTYPE_DAC As Integer = 3

    ' ===== Dependencias =====
    Private Shared ReadOnly _http As HttpClient = New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
    Private ReadOnly _db As BrmOracleQuery = New BrmOracleQuery()

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
    Public Property OnOut As Action(Of String)

    Private Sub OUT(line As String)
        Try
            If OnOut IsNot Nothing Then OnOut.Invoke(line)
        Catch
        End Try
    End Sub

    ' ===== API =====
    Public Async Function ActualizarAsync(accountPoid As String,
                                          Optional persist As Boolean = True) As Task(Of BolecodeResponseResult)

        Dim r As New BolecodeResponseResult With {.AccountPoid = accountPoid}

        Try
            Dim poid As String = NormalizarAccountPoid(accountPoid)
            If String.IsNullOrWhiteSpace(poid) Then
                ErrorMessage = "AccountPoid inválido."
                OUT("[BOLECODE][ERROR] " & ErrorMessage)
                Return r
            End If

            Dim accountId As Long?
            accountId = ExtraerAccountId(poid)
            If Not accountId.HasValue Then
                ErrorMessage = "No fue posible obtener el ID numérico de la cuenta."
                OUT("[BOLECODE][ERROR] " & ErrorMessage)
                Return r
            End If

            Dim parId As String = ObtenerParIdPorAccount(accountId.Value)
            If String.IsNullOrWhiteSpace(parId) Then
                ErrorMessage = "No se encontró PAR_ID asociado a la cuenta."
                OUT("[BOLECODE][ERROR] " & ErrorMessage)
                Return r
            End If

            Dim payType As Integer? = ObtenerPayTypePorPar(parId)
            Dim requiereActualizacion As Boolean = Not payType.HasValue OrElse payType.Value = PAYTYPE_BOLETO OrElse payType.Value = PAYTYPE_DAC
            OUT("[BOLECODE][DB] PAR_ID=" & parId & " PAY_TYPE=" & If(payType.HasValue, payType.Value.ToString(), "NULL") & " RequiresUpdates=" & requiereActualizacion.ToString())

            Dim boletoToken As String
            Dim pixToken As String

            If requiereActualizacion Then
                boletoToken = ObtenerSiguienteToken("PIN.AC_PAR_REMESSA_BOLETO_T", "TOKEN_BOLETO", TOKEN_PREFIX, TOKEN_SEED)
                pixToken = ObtenerSiguienteToken("PIN.AC_PAR_REMESSA_PIX_T", "PIX_TOKEN", TOKEN_PREFIX, TOKEN_SEED)

                OUT("[BOLECODE][DB] Nuevos tokens generados TOKEN_BOLETO=" & boletoToken & " PIX_TOKEN=" & pixToken)

                If persist Then
                    ActualizarToken("PIN.AC_PAR_REMESSA_BOLETO_T", "TOKEN_BOLETO", parId, boletoToken)
                    ActualizarToken("PIN.AC_PAR_REMESSA_PIX_T", "PIX_TOKEN", parId, pixToken)
                    ActualizarStatusPar(parId, 2)
                Else
                    OUT("[BOLECODE][DRY-RUN] persist=False, updates de tokens omitidos.")
                End If
            Else
                boletoToken = ObtenerTokenActual("PIN.AC_PAR_REMESSA_BOLETO_T", "TOKEN_BOLETO", parId)
                pixToken = ObtenerTokenActual("PIN.AC_PAR_REMESSA_PIX_T", "PIX_TOKEN", parId)
                OUT("[BOLECODE][DB] Tokens existentes reutilizados TOKEN_BOLETO=" & boletoToken & " PIX_TOKEN=" & pixToken)
            End If

            Dim tokenEfectivo As String = NormalizarToken(boletoToken)
            Dim barCode As String = NormalizarCodigoBarras(tokenEfectivo)
            Dim typeableLine As String = GenerarLineaDigitavel(barCode)
            If String.IsNullOrWhiteSpace(typeableLine) Then
                typeableLine = TYPEABLE_SEED
            End If

            Dim parIdEfectivo As String = If(String.IsNullOrWhiteSpace(parId), String.Empty, parId)

            OUT("[BOLECODE][PAYLOAD] token=" & tokenEfectivo & " bar_code=" & barCode & " typeable_line=" & typeableLine)

            Dim payload As New JObject From {
                {"token", tokenEfectivo},
                {"id", parIdEfectivo},
                {"origin", "brmnf"},
                {"status", "valid"},
                {"bar_code", barCode},
                {"typeable_line", typeableLine},
                {"our_number", "0000000042398"},
                {"gateway_boleto", "santander"},
                {"qr_code", "0002010102122692..."},
                {"gateway_pix", "pagarme"}
            }
            Dim json As String = payload.ToString()
            LastRequestJson = json
            OUT(">>> [BOLECODE][JSON] payload disponible en Log_Debug.")
            LogJsonToLogger("BOLECODE_REQUEST", json)

            If Not persist Then
                r.Success = True
                r.Token = tokenEfectivo
                r.ParId = parIdEfectivo
                OUT("[BOLECODE][DRY-RUN] persist=False, POST omitido.")
                LogInfoToLogger("BOLECODE_DRYRUN", "[BOLECODE][DRY-RUN] persist=False, POST omitido.")
                Return r
            End If

            Dim endpoint As String = BASE_URL.TrimEnd("/"c) & PATH
            Using req As New HttpRequestMessage(HttpMethod.Post, endpoint)
                req.Headers.Accept.Clear()
                req.Headers.Accept.ParseAdd("application/json")
                req.Content = New StringContent(json, Encoding.UTF8, "application/json")

                Using resp = Await _http.SendAsync(req).ConfigureAwait(False)
                    Dim body As String = Await resp.Content.ReadAsStringAsync().ConfigureAwait(False)
                    LastHttpStatus = CInt(resp.StatusCode)
                    LastResponseBody = body

                    OUT("<<< [BOLECODE][HTTP] " & LastHttpStatus.GetValueOrDefault().ToString())
                    OUT("<<< [BOLECODE][RESP] respuesta disponible en Log_Debug.")
                    LogInfoToLogger("BOLECODE_HTTP", "<<< [BOLECODE][HTTP] " & LastHttpStatus.GetValueOrDefault().ToString())
                    LogJsonToLogger("BOLECODE_RESPONSE", body)

                    r.Success = (LastHttpStatus.HasValue AndAlso LastHttpStatus.Value >= 200 AndAlso LastHttpStatus.Value < 300)
                    r.Token = tokenEfectivo
                    r.ParId = parIdEfectivo
                    r.HttpStatus = LastHttpStatus
                    r.RawBody = LastResponseBody
                End Using
            End Using

        Catch ex As Exception
            ErrorMessage = ex.Message
            OUT("[BOLECODE][ERROR] " & ErrorMessage)
            LogInfoToLogger("BOLECODE_ERROR", "[BOLECODE][ERROR] " & ErrorMessage)
            If _logger IsNot Nothing Then
                Try
                    _logger.LogError(ErrorMessage, ex, New With {.Operacion = "BolecodeResponse"})
                Catch
                End Try
            End If
            r.Success = False
            r.HttpStatus = LastHttpStatus
            r.RawBody = LastResponseBody
        End Try

        Return r
    End Function

    ' ===== Helpers =====
    Private Function ObtenerParIdPorAccount(accountId As Long) As String
        Try
            Dim sql As String = "SELECT MAX(PAR_ID) FROM PIN.AC_PAR_REMESSA_BOLETO_T WHERE ACCOUNT_OBJ_ID0 = :acc"
            Dim parId As String = _db.ExecuteScalar(Of String)(sql,
                New Dictionary(Of String, Object) From {{":acc", accountId}}, 15)
            If String.IsNullOrWhiteSpace(parId) Then
                Return String.Empty
            End If
            Return parId.Trim()
        Catch
            Return String.Empty
        End Try
    End Function

    Private Function ObtenerSiguienteToken(tableName As String,
                                           columnName As String,
                                           prefix As String,
                                           seed As String) As String
        Dim maxToken As String = Nothing
        Try
            Dim sql As String = String.Format("SELECT MAX({0}) FROM {1} WHERE {0} LIKE :pfx", columnName, tableName)
            maxToken = _db.ExecuteScalar(Of String)(sql,
                New Dictionary(Of String, Object) From {{":pfx", prefix & "%"}}, 15)
        Catch

        End Try
        Dim baseToken As String = If(String.IsNullOrWhiteSpace(maxToken), seed, maxToken.Trim())
        If String.IsNullOrWhiteSpace(baseToken) Then baseToken = seed
        If Not baseToken.StartsWith(prefix, StringComparison.Ordinal) Then
            baseToken = seed
        End If
        Return IncrementToken(baseToken)
    End Function

    Private Shared Function IncrementToken(baseToken As String) As String
        Dim trimmed As String = If(baseToken, String.Empty).Trim()
        If trimmed.Length = 0 Then Return TOKEN_SEED

        If trimmed.Length >= 4 Then
            Dim prefixLength As Integer = trimmed.Length - 4
            Dim prefix As String = trimmed.Substring(0, prefixLength)
            Dim suffix As String = trimmed.Substring(prefixLength)
            Dim lastDigits As Integer
            If Integer.TryParse(suffix, lastDigits) Then
                lastDigits += 1
                If lastDigits <= 9999 Then
                    Return prefix & lastDigits.ToString("D4")
                End If
            End If
        End If

        Return IncrementAllDigits(trimmed)
    End Function

    Private Shared Function NormalizarToken(token As String) As String
        Dim limpio As String = If(token, String.Empty).Trim()
        If String.IsNullOrWhiteSpace(limpio) Then
            Return TOKEN_SEED
        End If

        If Not EsNumerico(limpio) Then
            Return TOKEN_SEED
        End If

        If limpio.Length <> 44 Then
            Return TOKEN_SEED
        End If

        Return limpio
    End Function

    Private Shared Function NormalizarCodigoBarras(token As String) As String
        Dim limpio As String = If(token, String.Empty).Trim()
        If EsCodigoBarrasValido(limpio) Then
            Return limpio
        End If
        Return TOKEN_SEED
    End Function

    Private Shared Function EsCodigoBarrasValido(codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False
        If codigo.Length <> 44 Then Return False
        Return EsNumerico(codigo)
    End Function

    Private Shared Function EsNumerico(valor As String) As Boolean
        If String.IsNullOrWhiteSpace(valor) Then Return False
        For Each ch As Char In valor
            If Not Char.IsDigit(ch) Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Shared Function GenerarLineaDigitavel(barcode As String) As String
        If Not EsCodigoBarrasValido(barcode) Then Return String.Empty

        Try
            Dim campo1SemDv As String = barcode.Substring(0, 4) & barcode.Substring(19, 5)
            Dim campo2SemDv As String = barcode.Substring(24, 10)
            Dim campo3SemDv As String = barcode.Substring(34, 10)
            Dim campo4 As String = barcode.Substring(4, 1)
            Dim campo5 As String = barcode.Substring(5, 14)

            Dim campo1 As String = campo1SemDv & CalcularDigitoModulo10(campo1SemDv).ToString()
            Dim campo2 As String = campo2SemDv & CalcularDigitoModulo10(campo2SemDv).ToString()
            Dim campo3 As String = campo3SemDv & CalcularDigitoModulo10(campo3SemDv).ToString()

            Return campo1 & campo2 & campo3 & campo4 & campo5
        Catch
            Return String.Empty
        End Try
    End Function

    Private Shared Function CalcularDigitoModulo10(valor As String) As Integer
        Dim suma As Integer = 0
        Dim multiplicador As Integer = 2

        For i As Integer = valor.Length - 1 To 0 Step -1
            Dim digito As Integer = AscW(valor(i)) - AscW("0"c)
            Dim producto As Integer = digito * multiplicador
            If producto >= 10 Then
                suma += (producto \ 10) + (producto Mod 10)
            Else
                suma += producto
            End If

            multiplicador = If(multiplicador = 2, 1, 2)
        Next

        Dim resto As Integer = suma Mod 10
        Dim dv As Integer = (10 - resto) Mod 10
        Return dv
    End Function

    Private Shared Function IncrementAllDigits(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return TOKEN_SEED

        Dim chars As Char() = value.Trim().ToCharArray()
        Dim carry As Integer = 1

        For i As Integer = chars.Length - 1 To 0 Step -1
            If Not Char.IsDigit(chars(i)) Then
                Continue For
            End If

            Dim digit As Integer = AscW(chars(i)) - AscW("0"c) + carry
            If digit >= 10 Then
                chars(i) = "0"c
                carry = 1
            Else
                chars(i) = ChrW(AscW("0"c) + digit)
                carry = 0
                Exit For
            End If
        Next

        Dim result As String = New String(chars)
        If carry = 1 Then
            result = "1" & result
        End If
        Return result
    End Function

    Private Sub ActualizarToken(tableName As String,
                                 columnName As String,
                                 parId As String,
                                 token As String)
        Dim sql As String = String.Format("UPDATE {0} SET {1} = :token WHERE PAR_ID = :par", tableName, columnName)
        Dim parameters As New Dictionary(Of String, Object) From {
            {":token", token},
            {":par", parId}
        }
        _db.ExecuteNonQuery(sql, parameters, 20)
    End Sub

    Private Function ObtenerTokenActual(tableName As String,
                                         columnName As String,
                                         parId As String) As String
        If String.IsNullOrWhiteSpace(parId) Then Return String.Empty

        Try
            Dim sql As String = String.Format("SELECT {0} FROM {1} WHERE PAR_ID = :par", columnName, tableName)
            Dim token As String = _db.ExecuteScalar(Of String)(sql,
                New Dictionary(Of String, Object) From {{":par", parId}}, 15)
            If String.IsNullOrWhiteSpace(token) Then Return String.Empty
            Return token.Trim()
        Catch
            Return String.Empty
        End Try
    End Function

    Private Sub ActualizarStatusPar(parId As String, statusId As Integer)
        Dim sql As String = "UPDATE PIN.AC_PAR_T SET STATUS_ID = :status WHERE PAR_ID = :par"
        Dim parameters As New Dictionary(Of String, Object) From {
            {":status", statusId},
            {":par", parId}
        }
        _db.ExecuteNonQuery(sql, parameters, 20)
    End Sub

    Private Function ExtraerAccountId(poid As String) As Long?
        If String.IsNullOrWhiteSpace(poid) Then Return Nothing

        Dim parts As String() = poid.Trim().Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        Dim idx As Integer = Array.FindIndex(parts, Function(p) p.Equals("/account", StringComparison.OrdinalIgnoreCase))
        If idx >= 0 AndAlso idx + 1 < parts.Length Then
            Dim candidate As String = parts(idx + 1)
            Dim acc As Long
            If Long.TryParse(candidate, acc) Then Return acc
        End If

        For i As Integer = parts.Length - 1 To 0 Step -1
            Dim acc As Long
            If Long.TryParse(parts(i), acc) Then Return acc
        Next

        Return Nothing
    End Function

    Private Function NormalizarAccountPoid(poid As String) As String
        If String.IsNullOrWhiteSpace(poid) Then Return ""
        Dim s As String = poid.Trim()
        If s.IndexOf("/account", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return s
        End If
        Dim idNum As Long
        If Long.TryParse(s, idNum) Then
            Return "0.0.0.1 /account " & idNum.ToString() & " 0"
        End If
        Return s
    End Function

    Private Function ObtenerPayTypePorPar(parId As String) As Integer?
        If String.IsNullOrWhiteSpace(parId) Then Return Nothing

        Dim columnas() As String = {"PAY_TYPE", "PAY_TYPE_ID"}

        For Each columna In columnas
            Try
                Dim sql As String = String.Format("SELECT {0} FROM PIN.AC_PAR_T WHERE PAR_ID = :par", columna)
                Dim payTypeRaw As String = _db.ExecuteScalar(Of String)(sql,
                    New Dictionary(Of String, Object) From {{":par", parId}}, 15)

                If Not String.IsNullOrWhiteSpace(payTypeRaw) Then
                    Dim pay As Integer
                    If Integer.TryParse(payTypeRaw.Trim(), pay) Then
                        Return pay
                    End If
                End If
            Catch
                ' Ignorar y probar con el siguiente nombre de columna posible.
            End Try
        Next

        Return Nothing
    End Function

    Private Sub LogInfoToLogger(scope As String, message As String)
        If _logger Is Nothing OrElse String.IsNullOrWhiteSpace(message) Then Return
        Try
            _logger.LogData(message, scope)
        Catch
        End Try
    End Sub

    Private Sub LogJsonToLogger(scope As String, json As String)
        If _logger Is Nothing OrElse String.IsNullOrWhiteSpace(json) Then Return
        Try
            Dim pretty As String
            Try
                pretty = JObject.Parse(json).ToString()
            Catch
                pretty = json
            End Try
            _logger.LogJson(pretty, scope)
        Catch
        End Try
    End Sub

End Class
