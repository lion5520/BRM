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

            Dim boletoToken As String = ObtenerSiguienteToken("PIN.AC_PAR_REMESSA_BOLETO_T", "TOKEN_BOLETO", TOKEN_PREFIX, TOKEN_SEED)
            Dim pixToken As String = ObtenerSiguienteToken("PIN.AC_PAR_REMESSA_PIX_T", "PIX_TOKEN", TOKEN_PREFIX, TOKEN_SEED)

            OUT("[BOLECODE][DB] PAR_ID=" & parId & " TOKEN_BOLETO=" & boletoToken & " PIX_TOKEN=" & pixToken)

            If persist Then
                ActualizarToken("PIN.AC_PAR_REMESSA_BOLETO_T", "TOKEN_BOLETO", parId, boletoToken)
                ActualizarToken("PIN.AC_PAR_REMESSA_PIX_T", "PIX_TOKEN", parId, pixToken)
                ActualizarStatusPar(parId, 2)
            Else
                OUT("[BOLECODE][DRY-RUN] persist=False, updates de tokens omitidos.")
            End If

            Dim payload As New JObject From {
                {"token", boletoToken},
                {"id", parId},
                {"origin", "brmnf"},
                {"status", "valid"},
                {"bar_code", "03395942700000000109019716900000001038891001"},
                {"typeable_line", "03399356782060000000201234501011693970000000100"},
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
                r.Token = boletoToken
                r.ParId = parId
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
                    r.Token = boletoToken
                    r.ParId = parId
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
        If String.IsNullOrWhiteSpace(baseToken) Then Return TOKEN_SEED & "1"

        Dim chars As Char() = baseToken.Trim().ToCharArray()
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
