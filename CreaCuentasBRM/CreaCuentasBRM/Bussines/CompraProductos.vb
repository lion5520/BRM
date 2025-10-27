Option Strict On
Option Explicit On

Imports System
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Data
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class CompraProductos

    Public Enum PayType
        CreditCard = 1  ' → PIN_FLD_PAY_TYPE = -1
        Boleto = 2      ' → -2
        DAC = 3         ' → -3
    End Enum

    ' ===== Config =====
    Public Property BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH_PURCHASE As String = "/BRMCustCustomServices/resources/BRMPurchaseCustomServicesREST/PurchasePlans"
    Private Const PROTOCOL_PREFIX As String = "ORACLE_SAP_TEST_"
    Private Const CONTRACT_PREFIX As String = "TP_"

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
    Public Async Function ComprarAsync(accountPoid As String,
                                       tipo As PayType,
                                       Optional persist As Boolean = True,
                                       Optional payTypeOverride As Integer? = Nothing) As Task(Of CompraProductosResult)

        Dim r As New CompraProductosResult With {.AccountPoid = accountPoid}

        Try
            Dim poid As String = NormalizarAccountPoid(accountPoid)
            If String.IsNullOrWhiteSpace(poid) Then
                ErrorMessage = "AccountPoid inválido."
                OUT("[PURCHASE][ERROR] " & ErrorMessage)
                Return r
            End If

            ' 1) Protocolo/Contrato y Terminal
            Dim protocol As String = GenerateUniqueProtocolId(PROTOCOL_PREFIX)
            Dim contractId As String = CONTRACT_PREFIX & protocol
            Dim terminal As String = GenerarTerminalUnico()

            ' 2) Pay type
            Dim pinPayType As Integer = If(payTypeOverride.HasValue, payTypeOverride.Value,
                                           If(tipo = PayType.CreditCard, -1, If(tipo = PayType.Boleto, -2, -3)))

            ' 3) JSON exacto
            Dim payload As JObject = BuildPurchasePayload(poid, protocol, contractId, terminal, pinPayType)
            Dim json As String = payload.ToString(Formatting.None)

            LastRequestJson = json
            OUT(">>> [PURCHASE][JSON]")
            OUT(LastRequestJson)

            If Not persist Then
                r.Success = True
                r.ProtocolId = protocol
                r.ContractId = contractId
                r.Terminal = terminal
                OUT("[PURCHASE][DRY-RUN] persist=False, POST omitido.")
                Return r
            End If

            ' 4) POST
            Dim endpoint As String = BASE_URL.TrimEnd("/"c) & PATH_PURCHASE
            Using req As New HttpRequestMessage(HttpMethod.Post, endpoint)
                req.Headers.Accept.Clear()
                req.Headers.Accept.ParseAdd("application/json")
                req.Content = New StringContent(json, Encoding.UTF8, "application/json")

                Using resp = Await _http.SendAsync(req).ConfigureAwait(False)
                    Dim body As String = Await resp.Content.ReadAsStringAsync().ConfigureAwait(False)
                    LastHttpStatus = CInt(resp.StatusCode)
                    LastResponseBody = body

                    OUT("<<< [PURCHASE][HTTP] " & LastHttpStatus.GetValueOrDefault().ToString())
                    OUT("<<< [PURCHASE][RESP]")
                    OUT(LastResponseBody)

                    ' 5) Validación mínima por protocolo (ajusta a tu log/tabla)
                    Dim ok As Boolean = ValidarCompraEnBdPorProtocolo(protocol)
                    r.Success = ok
                    r.ProtocolId = protocol
                    r.ContractId = contractId
                    r.Terminal = terminal
                    r.HttpStatus = LastHttpStatus
                    r.RawBody = LastResponseBody
                End Using
            End Using

        Catch ex As Exception
            ErrorMessage = ex.Message
            OUT("[PURCHASE][ERROR] " & ErrorMessage)
            r.Success = False
            r.HttpStatus = LastHttpStatus
            r.RawBody = LastResponseBody
        End Try

        Return r
    End Function

    Private Function BuildPurchasePayload(accountPoid As String,
                                          protocolId As String,
                                          contractId As String,
                                          terminal As String,
                                          pinPayType As Integer) As JObject
        Dim o As New JObject()
        o("PIN_FLD_POID") = accountPoid
        o("AC_FLD_PROTOCOL_ID") = protocolId
        o("AC_FLD_CONTRACT_ID") = contractId
        o("AC_FLD_STR_COD_TERMINAL") = terminal
        o("PIN_FLD_PAY_TYPE") = pinPayType
        ' TODO: agrega campos fijos restantes si tu API lo pide
        Return o
    End Function

    ' ====== Validación mínima ======
    Private Function ValidarCompraEnBdPorProtocolo(protocolId As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (
    SELECT 1 FROM pin.ac_interface_log_t
     WHERE input_json LIKE '%' || :p || '%'
) THEN 1 ELSE 0 END AS existe FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":p", protocolId}}, 20)
            Return (v = 1)
        Catch
            Return False
        End Try
    End Function

    ' ====== Secuenciador/Terminal ======
    Private Function GenerateUniqueProtocolId(prefix As String) As String
        Dim baseMax As Integer = GetMaxSuffixFromDb(prefix)
        If baseMax < 0 Then baseMax = 0
        Dim trySuffix As Integer = baseMax + 1
        Dim attempts As Integer = 0
        While attempts < 300
            Dim candidate As String = prefix & trySuffix.ToString("0000")
            If Not ProtocoloExiste(candidate) Then
                OUT("[SEQ][PURCHASE] next unique → " & candidate)
                Return candidate
            End If
            trySuffix += 1
            attempts += 1
        End While
        Return prefix & DateTime.Now.ToString("mmss")
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
        Catch ex As Exception
            OUT("[SEQ][PURCHASE][ERR] " & ex.Message)
            Return -1
        End Try
        Return maxSuffix
    End Function

    Private Function ProtocoloExiste(candidate As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (SELECT 1 FROM pin.ac_protocol_t WHERE TRIM(ac_protocol_id) = :cand)
              OR EXISTS (SELECT 1 FROM pin.ac_profile_account_t WHERE TRIM(contract_id) = :cand)
           THEN 1 ELSE 0 END AS existe
  FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":cand", candidate}}, 15)
            Return (v = 1)
        Catch
            Return True
        End Try
    End Function

    Private Shared ReadOnly _rnd As New Random()

    Private Function GenerarTerminalUnico() As String
        Dim tries As Integer = 0
        While tries < 200
            ' 10 dígitos
            Dim t As String = _rnd.Next(300000000, 399999999).ToString() & _rnd.Next(10, 99).ToString()
            If Not TerminalExiste(t) Then Return t
            tries += 1
        End While
        Return _rnd.Next(1000000000, Integer.MaxValue).ToString()
    End Function

    Private Function TerminalExiste(terminal As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (
  SELECT 1 FROM pin.ac_interface_log_t
   WHERE input_json LIKE '%' || :t || '%'
) THEN 1 ELSE 0 END AS e FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":t", terminal}}, 15)
            Return (v = 1)
        Catch
            Return False
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
