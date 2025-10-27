' VB.NET WinForms (Framework 4.8) – Creador de Clientes BRM por Semillas (PF/PJ)
' -------------------------------------------------------------------------------
' UI mínima:
'   - Tipo: PF / PJ
'   - UF: Aleatorio o estado
'   - Cantidad N
'   - Botones: Crear 1, Crear N, Validar último
'
' Semillas: seeds_clientes_br.csv (misma carpeta del EXE/Proyecto)
'   Columns: name,street_type,street_name,neighborhood,city,uf,zip,phone,email
'
' POST: createCustomer (JSON en el ORDEN exacto de tus ejemplos)
' Validación: SELECT por CPF/CNPJ (editable en pestaña Config)
'
Imports System
Imports System.Linq
Imports System.Text
Imports System.Net
Imports System.Net.Http
Imports System.Windows.Forms
Imports Newtonsoft.Json.Linq
Imports Oracle.ManagedDataAccess.Client

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New FrmWizardCliente())
    End Sub
End Module

Public Class FrmWizardCliente
    Inherits Form

    ' ===== Config por defecto =====
    Private Const DEFAULT_BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH_CREATE As String = "/BRMCustCustomServices/resources/BRMAccountCustomServicesREST/createCustomer"
    Private Const DEFAULT_ORACLE_CS As String = "User Id=pin;Password=F0i1B2R3a5##;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=pindev-d1.interno)(PORT=1549))(CONNECT_DATA=(SERVICE_NAME=PINDEV)))"
    Private Const PREFIX_PROTOCOL As String = "ORACLE_SAP_TEST_"

    ' ===== UI =====
    Private tab As TabControl
    Private tpConfig As TabPage, tpCrear As TabPage, tpLog As TabPage

    Private txtBaseUrl As TextBox, txtToken As TextBox, txtConn As TextBox, txtQCreate As TextBox
    Private cboTipo As ComboBox, cboUF As ComboBox, nudN As NumericUpDown
    Private btnCrear1 As Button, btnCrearN As Button, btnValidarUltimo As Button
    Private txtLog As TextBox

    ' HTTP
    Private http As HttpClient

    ' Semillas
    Private seeds As List(Of String())

    ' Último doc (para validar)
    Private ultimoDoc As String = ""


    ' Random a nivel de clase (evita repetición de semilla por creación rápida)
    Private ReadOnly rnd As New Random()

    Public Sub New()
        Me.Text = "BRM – Crear Clientes (PF/PJ) por Semillas"
        Me.Width = 980
        Me.Height = 720
        Me.StartPosition = FormStartPosition.CenterScreen

        Dim handler = New HttpClientHandler() With {.AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate}
        http = New HttpClient(handler) With {.Timeout = TimeSpan.FromSeconds(60)}

        ConstruirUI()
        InicializarValores()
    End Sub

    ' Siempre arma 8 partes: CEP|TIPO|LOGRADOURO|NUMERO|BAIRRO|COMPLEMENTO|LOTE|RESERVADO
    Private Function BuildAddressPipeExacto(cep As String, tipoVia As String, logradouro As String,
                                        numero As String, bairro As String,
                                        Optional complemento As String = "",
                                        Optional lote As String = "",
                                        Optional reservado As String = "") As String
        Dim p As String() = {
        SoloDigitos(If(cep, "")),
        If(String.IsNullOrWhiteSpace(tipoVia), "Rua", tipoVia),
        If(logradouro, ""),
        If(numero, ""),
        If(bairro, ""),
        If(complemento, ""),
        If(lote, ""),
        If(reservado, "")
    }
        Return String.Join("|", p)
    End Function


    ' ================== Secuenciador desde BD (incrementa últimos 4 dígitos) ==================
    Private Function ObtenerSiguienteIdSecuencial(prefix As String) As String
        Dim nextNum As Integer = 1001 ' piso mínimo por si la tabla está vacía
        Try
            Using cn As New Oracle.ManagedDataAccess.Client.OracleConnection(txtConn.Text.Trim())
                cn.Open()
                Dim sql As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(val,'([0-9]{4})$'))),0)
   FROM (
         SELECT PA.CONTRACT_ID AS val
           FROM PIN.AC_PROFILE_ACCOUNT_T PA
          WHERE PA.CONTRACT_ID LIKE :p || '%'
         UNION ALL
         SELECT L.PROTOCOL_ID AS val
           FROM PIN.AC_INTERFACE_LOG_T L
          WHERE L.PROTOCOL_ID LIKE :p || '%'
        )"
                Using cmd As New Oracle.ManagedDataAccess.Client.OracleCommand(sql, cn)
                    cmd.Parameters.Add(":p", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = prefix
                    Dim r = cmd.ExecuteScalar()
                    If r IsNot Nothing AndAlso Not IsDBNull(r) Then
                        Dim max4 As Integer = Convert.ToInt32(r)
                        nextNum = Math.Max(nextNum, max4 + 1)
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Fallback si falla la BD para no frenar el flujo
            LogLn("[DB SEQ] " & ex.Message)
            nextNum = CInt(DateTime.Now.ToString("mmss"))
        End Try
        Dim id As String = prefix & nextNum.ToString("0000")
        LogLn("[SEQ] " & id)
        Return id
    End Function






    ' ================== UI ==================
    Private Sub ConstruirUI()
        tab = New TabControl() With {.Dock = DockStyle.Fill}
        tpConfig = New TabPage("0) Config")
        tpCrear = New TabPage("1) Crear")
        tpLog = New TabPage("Log")

        ' ----- Config -----
        Dim p0 As New Panel() With {.Dock = DockStyle.Fill}
        Dim y As Integer = 12

        p0.Controls.Add(MkLabel("Base URL:", 12, y))
        txtBaseUrl = MkText(DEFAULT_BASE_URL, 120, y, 400) : y += 32 : p0.Controls.Add(txtBaseUrl)

        p0.Controls.Add(MkLabel("Bearer Token (opcional):", 12, y))
        txtToken = MkText("", 180, y, 340) : y += 32 : p0.Controls.Add(txtToken)

        p0.Controls.Add(MkLabel("Oracle Connection:", 12, y))
        txtConn = MkText(DEFAULT_ORACLE_CS, 140, y, 780) : y += 32 : p0.Controls.Add(txtConn)

        p0.Controls.Add(MkLabel("Query Validación por CPF/CNPJ:", 12, y)) : y += 18
        txtQCreate = MkMultiline(
"SELECT PA.CPF_CNPJ, A.OBJ_ID0 AS CONTA,
TO_CHAR((TIMESTAMP '1970-01-01 00:00:00 +00:00' + NUMTODSINTERVAL(P.CREATED_T,'SECOND')) AT TIME ZONE 'GMT','DD.MM.YYYY HH24:MI:SS') AS DT_ATIVACAO,
PA.CONTRACT_ID AS CONTRATO, A.FIRST_NAME AS NOME, A.STATE AS UF, A.ADDRESS AS ENDERECO, A.CITY AS CIDADE, A.ZIP AS CEP,
DECODE (C.BUSINESS_TYPE, 1, '1 - PF', 2, '2 - PJ') TIPO_CLIENTE
FROM PIN.PROFILE_T P, PIN.AC_PROFILE_ACCOUNT_T PA, PIN.ACCOUNT_NAMEINFO_T A, PIN.ACCOUNT_T C
WHERE P.ACCOUNT_OBJ_ID0 = A.OBJ_ID0 AND A.OBJ_ID0 = C.POID_ID0 AND PA.OBJ_ID0 = P.POID_ID0
AND PA.CPF_CNPJ = :p_cpf_cnpj
ORDER BY PA.CONTRACT_ID DESC FETCH FIRST 1 ROWS ONLY", 12, y, 900, 100) : y += 110
        p0.Controls.Add(txtQCreate)

        tpConfig.Controls.Add(p0)

        ' ----- Crear -----
        Dim p1 As New Panel() With {.Dock = DockStyle.Fill}
        y = 20
        p1.Controls.Add(MkLabel("Tipo:", 12, y))
        cboTipo = New ComboBox() With {.Left = 60, .Top = y - 2, .Width = 80, .DropDownStyle = ComboBoxStyle.DropDownList}
        cboTipo.Items.AddRange(New Object() {"PF", "PJ"}) : cboTipo.SelectedIndex = 0 : p1.Controls.Add(cboTipo)

        p1.Controls.Add(MkLabel("UF:", 160, y))
        cboUF = New ComboBox() With {.Left = 190, .Top = y - 2, .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}
        cboUF.Items.AddRange(New Object() {"Aleatorio", "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"})
        cboUF.SelectedIndex = 0 : p1.Controls.Add(cboUF)

        p1.Controls.Add(MkLabel("Cantidad:", 320, y))
        nudN = New NumericUpDown() With {.Left = 390, .Top = y - 2, .Width = 80, .Minimum = 1, .Maximum = 999, .Value = 1}
        p1.Controls.Add(nudN)

        btnCrear1 = MkBtn("Crear 1", 500, y - 4, AddressOf BtnCrear1_Click)
        btnCrearN = MkBtn("Crear N", 600, y - 4, AddressOf BtnCrearN_Click)
        btnValidarUltimo = MkBtn("Validar último", 700, y - 4, AddressOf BtnValidarUltimo_Click)
        p1.Controls.AddRange(New Control() {btnCrear1, btnCrearN, btnValidarUltimo})

        y += 40
        p1.Controls.Add(MkLabel("Semillas: seeds_clientes_br.csv (misma carpeta del EXE/proyecto)", 12, y))

        tpCrear.Controls.Add(p1)

        ' ----- Log -----
        txtLog = New TextBox() With {.Multiline = True, .ScrollBars = ScrollBars.Both, .Dock = DockStyle.Fill, .Font = New Drawing.Font("Consolas", 10), .WordWrap = False}
        tpLog.Controls.Add(txtLog)

        tab.TabPages.AddRange(New TabPage() {tpConfig, tpCrear, tpLog})
        Me.Controls.Add(tab)
    End Sub

    Private Function MkLabel(t As String, x As Integer, y As Integer) As Label
        Return New Label() With {.Text = t, .Left = x, .Top = y, .AutoSize = True}
    End Function
    Private Function MkText(t As String, x As Integer, y As Integer, w As Integer) As TextBox
        Return New TextBox() With {.Text = t, .Left = x, .Top = y, .Width = w}
    End Function
    Private Function MkMultiline(t As String, x As Integer, y As Integer, w As Integer, h As Integer) As TextBox
        Return New TextBox() With {.Text = t, .Left = x, .Top = y, .Width = w, .Height = h, .Multiline = True, .ScrollBars = ScrollBars.Both, .Font = New Drawing.Font("Consolas", 9), .WordWrap = False}
    End Function
    Private Function MkBtn(t As String, x As Integer, y As Integer, h As EventHandler) As Button
        Dim b = New Button() With {.Text = t, .Left = x, .Top = y, .Width = 95}
        AddHandler b.Click, h
        Return b
    End Function

    Private Sub InicializarValores()
        txtBaseUrl.Text = DEFAULT_BASE_URL
        txtConn.Text = DEFAULT_ORACLE_CS
        seeds = CargarSemillas(GetSeedsPath())
        VerificarConexionYEndpoint()
    End Sub

    ' ================== Botones ==================
    Private Async Sub BtnCrear1_Click(sender As Object, e As EventArgs)
        Await CrearUnaCuenta()
    End Sub

    Private Async Sub BtnCrearN_Click(sender As Object, e As EventArgs)
        Dim n As Integer = CInt(nudN.Value)
        For i As Integer = 1 To n
            LogLn($"---- Lote {i}/{n} ----")
            Await CrearUnaCuenta()
            Await Task.Delay(200)
        Next
    End Sub

    Private Sub BtnValidarUltimo_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(ultimoDoc) Then
            LogLn("[VALIDAR] Aún no hay documento generado en esta sesión.")
            Return
        End If
        ValidarCreatePorCpfCnpj(txtQCreate.Text, ultimoDoc)
    End Sub

    ' ================== Crear cuenta ==================
    Private Async Function CrearUnaCuenta() As Threading.Tasks.Task
        Try
            Dim url = txtBaseUrl.Text.Trim().TrimEnd("/"c) & PATH_CREATE
            Dim payload = BuildCreateCustomerPayload() ' actualiza ultimoDoc

            Dim req = New HttpRequestMessage(HttpMethod.Post, url)
            req.Content = New StringContent(payload.ToString(), Encoding.UTF8, "application/json")
            Dim token = txtToken.Text.Trim()
            If token <> String.Empty Then
                req.Headers.Authorization = New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)
            End If
            req.Headers.Add("X-Request-Id", Guid.NewGuid().ToString("N"))

            LogLn("=== CREATE REQUEST ===")
            LogLn(payload.ToString())

            Dim resp = Await http.SendAsync(req)
            Dim body = Await resp.Content.ReadAsStringAsync()
            LogLn($"Status: {(CInt(resp.StatusCode))} {resp.ReasonPhrase}")
            LogLn(body)
            tab.SelectedTab = tpLog
        Catch ex As Exception
            LogLn("[ERROR CREATE] " & ex.Message)
            tab.SelectedTab = tpLog
        End Try
    End Function


    ' ================== JSON en orden exacto con ADDRESS hardcodeado ==================
    Private Function BuildCreateCustomerPayload() As JObject
        Dim esPF As Boolean = (cboTipo.SelectedItem.ToString() = "PF")

        ' 1) Semilla (la seguimos usando para nombre, email, etc.)
        Dim row = PickSeedRow()
        Dim name As String = Up(GetCol(row, 0), "ORLANDO ROMERO")
        Dim fone As String = SoloDigitos(If(GetCol(row, 7) = "", "47661185709", GetCol(row, 7)))
        Dim email As String = If(GetCol(row, 8) = "", "demo@nio.local", GetCol(row, 8))

        ' 2) UF final (si el usuario eligió una específica, usamos esa; si no, la que venga en semilla)
        Dim ufSeed As String = Up(GetCol(row, 5), "DF")
        Dim uf As String = If(cboUF.SelectedIndex > 0, cboUF.SelectedItem.ToString(), ufSeed)

        ' 3) ADDRESS FIJO (8 partes, 7 pipes) — por requerimiento
        Dim addressPipe As String = "19769459|Rua|Governador Roberto Silveira|317|São Pedro|||"

        ' 4) Para evitar inconsistencias con validaciones del backend, también alineo ZIP
        Dim zip As String = "19769459"

        ' 5) Documento fiscal válido (CPF para PF; CNPJ para PJ)
        Dim doc As String = If(esPF, GenerarCPFValidoDemo(), GenerarCNPJValidoDemo())
        ultimoDoc = doc

        ' 6) Protocolo desde BD (sufijo 4 dígitos +1 con tu prefijo)
        Dim proto As String = ObtenerSiguienteIdSecuencial(PREFIX_PROTOCOL)

        ' 7) Construcción en el ORDEN EXACTO
        Dim o As New JObject()
        o.Add("AC_FLD_PROTOCOL_ID", proto)
        o.Add("BUSINESS_TYPE", If(esPF, "1", "2"))
        o.Add("NAMEINFO", name)
        o.Add("ADDRESS", addressPipe)
        o.Add("ZIP", zip)

        ' CITY/STATE/COUNTRY: puedes dejar que vengan de la semilla/selección;
        ' si prefieres fijarlos, descomenta y ajusta:
        'Dim city As String = "Serra"   ' <- opcional si el backend lo exige
        'Dim state As String = "ES"     ' <- opcional si el backend lo exige
        'o.Add("CITY", city)
        'o.Add("STATE", state)
        'o.Add("COUNTRY", "Brasil")

        ' por defecto seguimos respetando UF de la UI y city de semilla:
        Dim citySeed As String = Up(GetCol(row, 4), "BRASILIA")
        o.Add("CITY", citySeed)
        o.Add("STATE", uf)
        o.Add("COUNTRY", "Brasil")

        o.Add("PHONES", fone)
        o.Add("PHONE_TYPE", "4")
        o.Add("AC_FLD_SEGMENT", "Warm")
        o.Add("AC_FLD_CPF_CNPJ", doc)
        o.Add("COUNTRY_CODE", "")
        o.Add("CITY_CODE", "")
        o.Add("AC_FLD_ADDRESS_NUMBER", "317")          ' consistente con ADDRESS fijo
        o.Add("AC_FLD_NEIGHBORHOOD", "São Pedro")      ' consistente con ADDRESS fijo
        o.Add("AC_FLD_BIRTHDAY_T", "1990-05-06")       ' PF usa esto; PJ lo ignora
        o.Add("AC_FLD_GENDER", "Masculino")
        o.Add("AC_FLD_EMAIL", email)

        If Not esPF Then
            o.Add("AC_FLD_INSCRICAO_ESTADUAL", "326749879")
            o.Add("AC_FLD_REPRESENTANTE_LEGAL", name)
            o.Add("AC_FLD_REPRESENTANTE_LEGAL_CPF", GenerarCPFValidoDemo())
            o.Add("AC_FLD_CNAE_CLIENTE", "4530701")
        End If

        Return o
    End Function



    ' ================== JSON en orden exacto ==================
    ' PF:  AC_FLD_PROTOCOL_ID, BUSINESS_TYPE, NAMEINFO, ADDRESS, ZIP, CITY, STATE, COUNTRY,
    '      PHONES, PHONE_TYPE, AC_FLD_SEGMENT, AC_FLD_CPF_CNPJ, COUNTRY_CODE, CITY_CODE,
    '      AC_FLD_ADDRESS_NUMBER, AC_FLD_NEIGHBORHOOD, AC_FLD_BIRTHDAY_T, AC_FLD_GENDER, AC_FLD_EMAIL
    ' PJ añade al final:
    '      AC_FLD_INSCRICAO_ESTADUAL, AC_FLD_REPRESENTANTE_LEGAL, AC_FLD_REPRESENTANTE_LEGAL_CPF, AC_FLD_CNAE_CLIENTE


    ' ================== Validación ==================
    Private Sub ValidarCreatePorCpfCnpj(sqlTemplate As String, cpfCnpj As String)
        Try
            Using cn As New OracleConnection(txtConn.Text.Trim())
                cn.Open()
                Using cmd As New OracleCommand(sqlTemplate, cn)
                    cmd.Parameters.Add(":p_cpf_cnpj", OracleDbType.Varchar2).Value = cpfCnpj
                    Using dr = cmd.ExecuteReader()
                        If dr.Read() Then
                            Dim cpf = SafeStr(dr, 0), conta = SafeStr(dr, 1), ativ = SafeStr(dr, 2), contrato = SafeStr(dr, 3)
                            Dim nome = SafeStr(dr, 4), uf = SafeStr(dr, 5), endereco = SafeStr(dr, 6), cidade = SafeStr(dr, 7), cep = SafeStr(dr, 8), tipo = SafeStr(dr, 9)
                            LogLn($"[CREATE] OK  CPF/CNPJ={cpf} CONTA={conta} ATIV={ativ} CONTRATO={contrato} NOME={nome} UF={uf} END={endereco} CIDADE={cidade} CEP={cep} TIPO={tipo}")
                        Else
                            LogLn($"[CREATE] Sin filas para CPF/CNPJ={cpfCnpj}")
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            LogLn("[DB CREATE] " & ex.Message)
        End Try
        tab.SelectedTab = tpLog
    End Sub

    ' ================== Semillas ==================
    Private Function GetSeedsPath() As String
        Return IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seeds_clientes_br.csv")
    End Function

    Private Function CargarSemillas(path As String) As List(Of String())
        Dim list As New List(Of String())
        Try
            If Not IO.File.Exists(path) Then Return list
            Using sr As New IO.StreamReader(path, Encoding.UTF8)
                Dim header As String = sr.ReadLine() ' skip header
                While Not sr.EndOfStream
                    Dim line = sr.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While
                    list.Add(ParseCsvLine(line))
                End While
            End Using
            LogLn($"[SEEDS] {list.Count} filas cargadas de {path}")
        Catch ex As Exception
            LogLn("[SEEDS] ERROR: " & ex.Message)
        End Try
        Return list
    End Function

    Private Function ParseCsvLine(line As String) As String()
        Dim res As New List(Of String)()
        Dim sb As New StringBuilder()
        Dim inQ As Boolean = False
        For Each ch In line
            If ch = """"c Then
                inQ = Not inQ
            ElseIf ch = ","c AndAlso Not inQ Then
                res.Add(sb.ToString()) : sb.Clear()
            Else
                sb.Append(ch)
            End If
        Next
        res.Add(sb.ToString())
        Return res.ToArray()
    End Function

    Private Function PickSeedRow() As String()
        If seeds Is Nothing OrElse seeds.Count = 0 Then
            ' Fallback mínimo
            Return New String() {"ORLANDO ROMERO", "Rua", "Odilon Auto", "Jardim Carapina", "Serra", "ES", "29161709", "27998555123", "orlando.romero@example.com"}
        End If
        Return seeds(rnd.Next(0, seeds.Count))
    End Function

    ' ====== Helpers faltantes (los que pedías) ======
    Private Function GetCol(row As String(), idx As Integer) As String
        If row Is Nothing Then Return ""
        If idx < 0 OrElse idx >= row.Length Then Return ""
        Dim v As String = If(row(idx), "")
        Return v
    End Function

    Private Function Up(s As String, Optional fallback As String = "") As String
        If String.IsNullOrWhiteSpace(s) Then Return fallback
        Return s.ToUpperInvariant()
    End Function

    Private Function PickNumero() As String
        Return rnd.Next(50, 999).ToString()
    End Function

    ' ================== Utiles ==================
    Private Function SoloDigitos(s As String) As String
        If s Is Nothing Then Return ""
        Return New String(s.Where(Function(c) Char.IsDigit(c)).ToArray())
    End Function

    Private Function SafeStr(dr As OracleDataReader, idx As Integer) As String
        Try
            If dr.IsDBNull(idx) Then Return ""
            Return Convert.ToString(dr.GetValue(idx))
        Catch
            Return ""
        End Try
    End Function

    Private Sub LogLn(msg As String)
        If txtLog Is Nothing Then Return
        If txtLog.TextLength > 0 Then txtLog.AppendText(Environment.NewLine)
        txtLog.AppendText($"{DateTime.Now:HH:mm:ss} | {msg}")
    End Sub

    ' Generadores CPF/CNPJ válidos (check digits)
    Private Function GenerarCPFValidoDemo() As String
        Dim nums(8) As Integer
        For i = 0 To 8 : nums(i) = rnd.Next(0, 10) : Next
        Dim d1 As Integer = 0
        For i = 0 To 8 : d1 += nums(i) * (10 - i) : Next
        d1 = 11 - (d1 Mod 11) : If d1 >= 10 Then d1 = 0
        Dim d2 As Integer = 0
        For i = 0 To 8 : d2 += nums(i) * (11 - i) : Next
        d2 += d1 * 2
        d2 = 11 - (d2 Mod 11) : If d2 >= 10 Then d2 = 0
        Return String.Join("", nums.Select(Function(n) n.ToString()).ToArray()) & d1.ToString() & d2.ToString()
    End Function

    ' ===== Helper: arma querystring a partir del JSON (key=value&...) =====
    Private Function BuildQueryFromJson(obj As JObject) As String
        Dim pares As New List(Of String)()
        For Each prop In obj.Properties()
            Dim k As String = prop.Name
            Dim v As String = ""
            If prop.Value IsNot Nothing Then v = prop.Value.ToString()
            ' Evita nulos y deja todo url-encoded
            pares.Add(Uri.EscapeDataString(k) & "=" & Uri.EscapeDataString(v))
        Next
        If pares.Count = 0 Then Return ""
        Return "?" & String.Join("&", pares.ToArray())
    End Function

    ' ===== Helper: agrega headers y evita 100-continue =====
    Private Sub SetHttpDefaults(req As HttpRequestMessage)
        req.Headers.Accept.Clear()
        req.Headers.Accept.Add(New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        ' algunos backends no leen el body si hay Expect: 100-continue
        System.Net.ServicePointManager.Expect100Continue = False
    End Sub

    ' ====== REEMPLAZA tu BtnCrear_Click por esta versión ======
    Private Async Sub BtnCrear_Click(sender As Object, e As EventArgs)
        Try
            Dim baseUrl = txtBaseUrl.Text.Trim().TrimEnd("/"c)
            Dim url = baseUrl & PATH_CREATE

            ' 1) Construimos el payload EXACTO (orden ya garantizado en BuildCreateCustomerPayload)
            Dim payload = BuildCreateCustomerPayload()

            ' 2) Para compatibilidad con el “Postman que sí guarda”: mismos campos en el querystring
            Dim qs = BuildQueryFromJson(payload)
            Dim finalUrl = url & qs

            ' 3) Preparamos request: body JSON + querystring
            Dim req = New HttpRequestMessage(HttpMethod.Post, finalUrl)
            req.Content = New StringContent(payload.ToString(), Encoding.UTF8, "application/json")
            SetHttpDefaults(req)

            Dim token = txtToken.Text.Trim()
            If token <> String.Empty Then
                req.Headers.Authorization = New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)
            End If
            req.Headers.Add("X-Request-Id", Guid.NewGuid().ToString("N"))

            LogLn("=== CREATE URL ===")
            LogLn(finalUrl)
            LogLn("=== CREATE BODY ===")
            LogLn(payload.ToString())

            Dim resp = Await http.SendAsync(req)
            Dim body = Await resp.Content.ReadAsStringAsync()
            LogLn($"Status: {(CInt(resp.StatusCode))} {resp.ReasonPhrase}")
            LogLn(body)

            ' Valida realmente en BD (el servicio devuelve 200 aun si no persiste)
            If Not String.IsNullOrWhiteSpace(ultimoDoc) Then
                ValidarCreatePorCpfCnpj(txtQCreate.Text, ultimoDoc)
            End If

            tab.SelectedTab = tpLog
        Catch ex As Exception
            LogLn("[ERROR CREATE] " & ex.Message)
            tab.SelectedTab = tpLog
        End Try
    End Sub

    Private Function GenerarCNPJValidoDemo() As String
        Dim nums(11) As Integer
        For i = 0 To 11 : nums(i) = rnd.Next(0, 10) : Next
        Dim pesos1 = New Integer() {5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2}
        Dim sum1 = 0 : For i = 0 To 11 : sum1 += nums(i) * pesos1(i) : Next
        Dim d1 = sum1 Mod 11 : d1 = If(d1 < 2, 0, 11 - d1)
        Dim pesos2 = New Integer() {6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2}
        Dim sum2 = d1 * 2
        For i = 0 To 11 : sum2 += nums(i) * pesos2(i + 1) : Next
        Dim d2 = sum2 Mod 11 : d2 = If(d2 < 2, 0, 11 - d2)
        Return String.Join("", nums.Select(Function(n) n.ToString()).ToArray()) & d1.ToString() & d2.ToString()
    End Function

    ' Conectividad rápida
    Private Sub VerificarConexionYEndpoint()
        Try
            Using cn As New OracleConnection(DEFAULT_ORACLE_CS)
                cn.Open()
                LogLn("[DB] Conexión OK → " & cn.DataSource)
            End Using
        Catch ex As Exception
            LogLn("[DB] ERROR → " & ex.Message)
        End Try
        Try
            Dim url = DEFAULT_BASE_URL.Trim().TrimEnd("/"c) & PATH_CREATE
            LogLn("[HTTP] Endpoint CREATE → " & url)
        Catch ex As Exception
            LogLn("[HTTP] ERROR → " & ex.Message)
        End Try
    End Sub


    ' =====================================================================
    '  SECUENCIADOR DE PROTOCOLO — ÚNICO, ROBUSTO Y LIMPIO
    '  - Genera ORACLE_SAP_TEST_XXXX (o el prefijo que pases)
    '  - Lee máximo en BD desde:
    '      * PIN.AC_PROTOCOL_T.AC_PROTOCOL_ID (si existe)
    '      * PIN.AC_PROFILE_ACCOUNT_T.CONTRACT_ID
    '      * PIN.AC_INTERFACE_LOG_T.INPUT_JSON  (buscando "AC_FLD_PROTOCOL_ID":"<prefijo>####")
    '  - Valida existencia y avanza hasta encontrar uno libre
    ' =====================================================================

    ' === API principal: usa esto para obtener el próximo protocolo único ===
    Private Function GenerateUniqueProtocolId(prefix As String) As String
        Dim baseMax As Integer = GetMaxSuffixFromDb(prefix)  ' -1 si no pudo leer nada
        If baseMax < 0 Then baseMax = 0

        Dim trySuffix As Integer = baseMax + 1
        Dim attempts As Integer = 0

        While attempts < 200
            Dim candidate As String = prefix & trySuffix.ToString("0000")
            If Not ProtocolExists(candidate) Then
                LogLn($"[SEQ] next unique → {candidate} (base={baseMax})")
                Return candidate
            End If
            trySuffix += 1
            attempts += 1
        End While

        ' Fallback defensivo si algo anda mal (no debería llegar aquí)
        Dim fallback As String = prefix & DateTime.Now.ToString("mmss")
        LogLn($"[SEQ] fallback → {fallback}")
        Return fallback
    End Function

    ' === Lee el MAXIMO sufijo de 4 dígitos desde BD para el prefijo dado ===
    Private Function GetMaxSuffixFromDb(prefix As String) As Integer
        Dim maxSuffix As Integer = -1

        Try
            Using cn As New Oracle.ManagedDataAccess.Client.OracleConnection(txtConn.Text.Trim())
                cn.Open()

                ' 1) AC_PROTOCOL_T.AC_PROTOCOL_ID (si existe la tabla/columna)
                Try
                    Using cmd As New Oracle.ManagedDataAccess.Client.OracleCommand(
                    "SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(ac_protocol_id),'([0-9]{4})$'))),0)
                       FROM pin.ac_protocol_t
                      WHERE ac_protocol_id LIKE :pfx || '%'", cn)
                        cmd.Parameters.Add(":pfx", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = prefix
                        Dim r = cmd.ExecuteScalar()
                        If r IsNot Nothing AndAlso Not IsDBNull(r) Then
                            maxSuffix = Math.Max(maxSuffix, Convert.ToInt32(r))
                        End If
                    End Using
                Catch
                    ' ignora si no existe la tabla
                End Try

                ' 2) AC_PROFILE_ACCOUNT_T.CONTRACT_ID
                Try
                    Using cmd As New Oracle.ManagedDataAccess.Client.OracleCommand(
                    "SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(contract_id),'([0-9]{4})$'))),0)
                       FROM pin.ac_profile_account_t
                      WHERE contract_id LIKE :pfx || '%'", cn)
                        cmd.Parameters.Add(":pfx", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = prefix
                        Dim r = cmd.ExecuteScalar()
                        If r IsNot Nothing AndAlso Not IsDBNull(r) Then
                            maxSuffix = Math.Max(maxSuffix, Convert.ToInt32(r))
                        End If
                    End Using
                Catch
                End Try

                ' 3) AC_INTERFACE_LOG_T.INPUT_JSON — extrae "AC_FLD_PROTOCOL_ID":"<prefijo>####"
                '    Usamos clases POSIX [[:space:]] para evitar problemas con \s (Oracle usa POSIX)
                Try
                    Using cmd As New Oracle.ManagedDataAccess.Client.OracleCommand(
                    "SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(
                               REGEXP_SUBSTR(input_json,
                                 '""AC_FLD_PROTOCOL_ID""[[:space:]]*:[[:space:]]*""' || :pfx || '([0-9]{4})""',
                                 1, 1, 'i', 1
                               ),
                               '([0-9]{4})$'
                             ))),0)
                       FROM pin.ac_interface_log_t
                      WHERE input_json LIKE '%' || :pfx || '%'
                        AND input_json LIKE '%AC_FLD_PROTOCOL_ID%'", cn)
                        cmd.Parameters.Add(":pfx", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = prefix
                        Dim r = cmd.ExecuteScalar()
                        If r IsNot Nothing AndAlso Not IsDBNull(r) Then
                            maxSuffix = Math.Max(maxSuffix, Convert.ToInt32(r))
                        End If
                    End Using
                Catch
                End Try
            End Using

            LogLn($"[SEQ] max_suf BD (prefix={prefix}) = {maxSuffix}")
        Catch ex As Exception
            LogLn("[SEQ][ERR] " & ex.Message)
            Return -1
        End Try

        Return maxSuffix
    End Function

    ' === Verifica si un protocolo ya existe en alguna de las fuentes ===
    Private Function ProtocolExists(candidate As String) As Boolean
        Try
            Using cn As New Oracle.ManagedDataAccess.Client.OracleConnection(txtConn.Text.Trim())
                cn.Open()
                Using cmd As New Oracle.ManagedDataAccess.Client.OracleCommand(
                "SELECT CASE WHEN EXISTS (
                           SELECT 1 FROM pin.ac_protocol_t WHERE TRIM(ac_protocol_id) = :cand
                         ) OR EXISTS (
                           SELECT 1 FROM pin.ac_profile_account_t WHERE TRIM(contract_id) = :cand
                         ) OR EXISTS (
                           SELECT 1
                             FROM pin.ac_interface_log_t
                            WHERE input_json LIKE '%' || :cand_json || '%'
                         )
                        THEN 1 ELSE 0 END AS existe
                   FROM dual", cn)

                    ' patrón JSON exacto para reducir falsos positivos en INPUT_JSON
                    Dim jsonNeedle As String = """AC_FLD_PROTOCOL_ID"":""" & candidate & """"
                    cmd.Parameters.Add(":cand", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = candidate
                    cmd.Parameters.Add(":cand_json", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = jsonNeedle

                    Dim r = cmd.ExecuteScalar()
                    Dim exists As Boolean = (r IsNot Nothing AndAlso Convert.ToInt32(r) = 1)
                    Return exists
                End Using
            End Using
        Catch ex As Exception
            LogLn("[SEQ][EXISTS][ERR] " & ex.Message)
            ' En error, para no colisionar, asumimos que SÍ existe
            Return True
        End Try
    End Function


End Class
