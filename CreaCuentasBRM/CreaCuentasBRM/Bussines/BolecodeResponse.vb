Option Strict On
Option Explicit On

Imports System
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq

Public Class BolecodeResponse

    ' ===== Config =====
    Public Property BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH As String = "/BRMCustCustomServices/resources/BRMPaymentCustomServicesREST/bolecodeResponse"

    ' ===== Dependencias =====
    Private Shared ReadOnly _http As HttpClient = New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
    Private ReadOnly _db As BrmOracleQuery = New BrmOracleQuery()

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

            ' Token (varía solo últimos 4 dígitos)
            Dim tokenBase As String = "0339594270000000010901971690000000103889"
            Dim token As String = GenerarTokenUnico(tokenBase)
            Dim parId As String = ObtenerParIdPorAccount(poid)

            Dim payload As New JObject From {
                {"token", token},
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
            OUT(">>> [BOLECODE][JSON]")
            OUT(LastRequestJson)

            If Not persist Then
                r.Success = True
                r.Token = token
                r.ParId = parId
                OUT("[BOLECODE][DRY-RUN] persist=False, POST omitido.")
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
                    OUT("<<< [BOLECODE][RESP]")
                    OUT(LastResponseBody)

                    r.Success = (LastHttpStatus.HasValue AndAlso LastHttpStatus.Value >= 200 AndAlso LastHttpStatus.Value < 300)
                    r.Token = token
                    r.ParId = parId
                    r.HttpStatus = LastHttpStatus
                    r.RawBody = LastResponseBody
                End Using
            End Using

        Catch ex As Exception
            ErrorMessage = ex.Message
            OUT("[BOLECODE][ERROR] " & ErrorMessage)
            r.Success = False
            r.HttpStatus = LastHttpStatus
            r.RawBody = LastResponseBody
        End Try

        Return r
    End Function

    ' ===== Helpers =====
    Private Shared ReadOnly _rnd As New Random()

    Private Function GenerarTokenUnico(prefix31 As String) As String
        For i As Integer = 0 To 200
            Dim last4 As String = _rnd.Next(0, 9999).ToString("0000")
            Dim candidate As String = prefix31 & last4
            If Not TokenExiste(candidate) Then Return candidate
        Next
        Return prefix31 & _rnd.Next(0, 9999).ToString("0000")
    End Function

    Private Function TokenExiste(token As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (
  SELECT 1 FROM pin.ac_interface_log_t
   WHERE input_json LIKE '%' || :t || '%'
) THEN 1 ELSE 0 END AS e FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":t", token}}, 15)
            Return (v = 1)
        Catch
            Return False
        End Try
    End Function

    Private Function ObtenerParIdPorAccount(accountPoid As String) As String
        Try
            ' TODO: si tienes tabla real, cámbialo aquí
            Return "60088"
        Catch
            Return "60088"
        End Try
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

End Class
