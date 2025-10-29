Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports CreaCuentasBRM.Helpers

Public Class FormMain

    Private ReadOnly _creador As New CreaCliente()
    Private ReadOnly _comprador As New CompraProductos()
    Private ReadOnly _bole As New BolecodeResponse()
    Private _appLogger As DualTextBoxLogger
    Private _totalCreados As Integer
    Private _totalErrores As Integer

    Private ReadOnly _db As BrmOracleQuery = New BrmOracleQuery()

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Engancha canal OUT
        _creador.OnOut = AddressOf OutSink
        _comprador.OnOut = AddressOf OutSink
        _bole.OnOut = AddressOf OutSink

        If _appLogger Is Nothing Then
            _appLogger = New DualTextBoxLogger()
            _appLogger.AttachFlow(Me, tst_log_out)
            _appLogger.AttachData(Me, tst_log_debug)
            _creador.Logger = _appLogger
            _comprador.Logger = _appLogger
            _bole.Logger = _appLogger
        End If

        If ProgressBar_general IsNot Nothing Then
            ProgressBar_general.Minimum = 0
            ProgressBar_general.Maximum = 100
            ProgressBar_general.Value = 0
        End If

        ResetCounters()
        ResetLabels()

        'Carga menu de estados 
        Me.ComboBox_UF.Items.AddRange(New Object() {"", "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"})
        Me.ComboBox_UF.SelectedIndex = 0

        AppendDebug("[DATA] [INIT] Form listo.")
    End Sub

    Private Sub OutSink(line As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New Action(Of String)(AddressOf OutSink), line)
            Return
        End If
        If _appLogger IsNot Nothing Then
            _appLogger.WriteFlow(line)
        ElseIf tst_log_out IsNot Nothing Then
            tst_log_out.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") & " " & line & Environment.NewLine)
        End If
    End Sub

    Private Sub AppendDebug(line As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New Action(Of String)(AddressOf AppendDebug), line)
            Return
        End If
        If _appLogger IsNot Nothing Then
            _appLogger.LogData(line, "FLOW")
        ElseIf tst_log_debug IsNot Nothing Then
            tst_log_debug.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") & " " & line & Environment.NewLine)
        End If
    End Sub

    Private Async Sub ProcesaTodo_Click(sender As Object, e As EventArgs) Handles ProcesaTodo.Click
        Dim previousCursor As Cursor = Me.Cursor
        Dim previousUseWait As Boolean = Me.UseWaitCursor

        If TabControl1 IsNot Nothing AndAlso TabPage4 IsNot Nothing Then
            TabControl1.SelectTab(TabPage4)
            TabControl1.Refresh()
        End If

        Me.UseWaitCursor = True
        Me.Cursor = Cursors.WaitCursor
        If ProcesaTodo IsNot Nothing Then ProcesaTodo.Enabled = False

        Try
            Dim doPersist As Boolean = (CheckBox_Persistencia IsNot Nothing AndAlso CheckBox_Persistencia.Checked)

            Dim nCuentas As Integer = 1
            Dim cuentasTexto As String = TextBox_NoCuentas?.Text
            If Not String.IsNullOrWhiteSpace(cuentasTexto) Then
                Dim tmp As Integer
                If Integer.TryParse(cuentasTexto.Trim(), tmp) AndAlso tmp > 0 Then nCuentas = tmp
            End If

            ResetCounters()
            ResetLabels()
            AppendDebug("[DATA] [FLOW] Inicio. Cuentas=" & nCuentas.ToString() & " Persistencia=" & doPersist.ToString())

            Dim totalSteps As Integer = Math.Max(1, nCuentas * 3)
            Dim currentStep As Integer = 0
            If ProgressBar_general IsNot Nothing Then
                ProgressBar_general.Minimum = 0
                ProgressBar_general.Maximum = totalSteps
                ProgressBar_general.Value = 0
                ProgressBar_general.Refresh()
            End If

            Dim aborted As Boolean = False
            Dim abortMessage As String = String.Empty

            For i As Integer = 1 To nCuentas
                AppendDebug("[DATA] [FLOW] --- INICIO #" & i.ToString() & "/" & nCuentas.ToString() & " ---")

                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, currentStep)
                    ProgressBar_general.Refresh()
                End If

                Dim tipoCliente As CreaCliente.TipoCliente = CreaCliente.TipoCliente.CPF
                If ComboBox_ClienteTPO IsNot Nothing AndAlso ComboBox_ClienteTPO.SelectedItem IsNot Nothing Then
                    Dim s As String = ComboBox_ClienteTPO.SelectedItem.ToString().Trim().ToUpperInvariant()
                    If s = "CNPJ" OrElse s = "CNPJ" Then
                        tipoCliente = CreaCliente.TipoCliente.CNPJ
                    ElseIf s = "CPF" OrElse s = "CPF" Then
                        tipoCliente = CreaCliente.TipoCliente.CPF
                    End If
                End If

                Dim uF_Seleccionada As String
                If ComboBox_UF IsNot Nothing AndAlso ComboBox_UF.SelectedItem IsNot Nothing Then
                    uF_Seleccionada = ComboBox_UF.SelectedItem.ToString().Trim().ToUpperInvariant()
                Else
                    'uF_Seleccionada = "RJ"
                    ' Selecciona un valor aleatorio dentro del rango de Items
                    Dim rnd As New Random()
                    Dim idx As Integer = rnd.Next(0, ComboBox_UF.Items.Count)
                    uF_Seleccionada = ComboBox_UF.Items(idx).ToString().Trim().ToUpperInvariant()
                End If

                AppendDebug("[DATA] [FLOW] Tipo Cliente: " & tipoCliente.ToString())

                AppendDebug("[DEBUG] [CREATE] Llamando CreaCliente…")
                Dim rc As CrearClienteResult = Await _creador.CrearAsync(tipoCliente, uF_Seleccionada, doPersist)
                If rc Is Nothing OrElse Not rc.Success Then
                    Dim detalle As String = If(String.IsNullOrWhiteSpace(_creador.ErrorMessage), "No se pudo crear el cliente.", _creador.ErrorMessage)
                    abortMessage = "[CREAR CUENTA] " & detalle
                    AppendDebug("[DEBUG] [ABORT] " & detalle)
                    aborted = True
                    If _appLogger IsNot Nothing Then
                        _appLogger.LogError(detalle, Nothing, New With {.Etapa = "CrearCuenta", .Iteracion = i})
                    End If
                    IncrementErrores()
                    Exit For
                End If
                currentStep += 1
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, currentStep)
                    ProgressBar_general.Refresh()
                End If

                Dim pay As CompraProductos.PayType = CompraProductos.PayType.Boleto

                If ComboBox_ProductoTPO IsNot Nothing AndAlso ComboBox_ProductoTPO.Items.Count > 0 Then
                    ' Si no hay selección, elegir aleatoriamente uno
                    If ComboBox_ProductoTPO.SelectedItem Is Nothing Then
                        Dim rnd As New Random()
                        ComboBox_ProductoTPO.SelectedIndex = rnd.Next(0, ComboBox_ProductoTPO.Items.Count)
                    End If

                    ' Obtener texto y limpiar prefijo numérico tipo "1." o "2."
                    Dim raw As String = ComboBox_ProductoTPO.SelectedItem.ToString().Trim()
                    Dim limpio As String = System.Text.RegularExpressions.Regex.Replace(raw, "^\d+\.\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).ToUpperInvariant()

                    ' Ahora el texto limpio coincide correctamente con los valores reales
                    Select Case limpio
                        Case "CREDITCARD"
                            pay = CompraProductos.PayType.CreditCard
                        Case "BOLETO"
                            pay = CompraProductos.PayType.Boleto
                        Case "DAC"
                            pay = CompraProductos.PayType.DAC
                        Case Else
                            pay = CompraProductos.PayType.Boleto
                    End Select
                End If


                AppendDebug("[DEBUG] [PURCHASE] Llamando CompraProductos…")
                Dim rb As CompraProductosResult = Await _comprador.ComprarAsync(rc.AccountPoid, pay, doPersist, Nothing)
                If rb Is Nothing OrElse Not rb.Success Then
                    Dim detalleCompra As String = If(String.IsNullOrWhiteSpace(_comprador.ErrorMessage), "No se pudo realizar la compra.", _comprador.ErrorMessage)
                    abortMessage = "[COMPRA PRODUCTOS] " & detalleCompra
                    AppendDebug("[DEBUG] [ABORT] " & detalleCompra)
                    aborted = True
                    If _appLogger IsNot Nothing Then
                        _appLogger.LogError(detalleCompra, Nothing, New With {.Etapa = "CompraProductos", .Iteracion = i, .Account = rc.AccountPoid})
                    End If
                    IncrementErrores()
                    Exit For
                End If
                currentStep += 1
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, currentStep)
                    ProgressBar_general.Refresh()
                End If

                AppendDebug("[DEBUG] [PAYMENT] Llamando BolecodeResponse…")
                Dim rp As BolecodeResponseResult = Await _bole.ActualizarAsync(rc.AccountPoid, doPersist)
                If rp Is Nothing OrElse Not rp.Success Then
                    Dim detallePago As String = If(String.IsNullOrWhiteSpace(_bole.ErrorMessage), "No se pudo actualizar la información de pago.", _bole.ErrorMessage)
                    abortMessage = "[ACTUALIZAR DATOS] " & detallePago
                    AppendDebug("[DEBUG] [ABORT] " & detallePago)
                    aborted = True
                    If _appLogger IsNot Nothing Then
                        _appLogger.LogError(detallePago, Nothing, New With {.Etapa = "ActualizarDatos", .Iteracion = i, .Account = rc.AccountPoid})
                    End If
                    IncrementErrores()
                    Exit For
                End If

                currentStep += 1
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, currentStep)
                    ProgressBar_general.Refresh()
                End If

                Dim dtGlobal As DataTable
                ' Llamadas consecutivas con distintos POIDs
                dtGlobal = ObtenerDatosPorPoid_Acumulativo(ExtraerAccountObjId(rc.AccountPoid))
                ' Asignar al DataGridView
                With DataGridView1
                    .AutoGenerateColumns = True    ' Permite generar columnas automáticamente
                    .DataSource = dtGlobal         ' Enlaza el DataTable acumulado
                    .Refresh()
                End With

                IncrementCreados()
                AppendDebug("[DATA] [FLOW] Iteración #" & i.ToString() & " completada.")
            Next

            If aborted Then
                AppendDebug("[DEBUG] [DONE] Proceso interrumpido antes de completar los pasos.")
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, currentStep)
                    ProgressBar_general.Refresh()
                End If
                If Not String.IsNullOrWhiteSpace(abortMessage) Then
                    MessageBox.Show(Me, abortMessage, "Proceso interrumpido", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                AppendDebug("[DATA] [DONE] Proceso completado.")
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = ProgressBar_general.Maximum
                    ProgressBar_general.Refresh()
                End If
            End If
            UpdateLabels()
        Catch ex As Exception
            AppendDebug("[ERROR] " & ex.Message)
            If _appLogger IsNot Nothing Then
                _appLogger.LogError(ex, New With {.Operacion = "ProcesaTodo"})
            End If
            MessageBox.Show(Me, ex.Message, "Error inesperado", MessageBoxButtons.OK, MessageBoxIcon.Error)

        Finally
            Me.UseWaitCursor = previousUseWait
            Me.Cursor = previousCursor
            TabControl1.SelectTab(TabPage2)
            TabControl1.Refresh()
            If ProcesaTodo IsNot Nothing Then ProcesaTodo.Enabled = True
        End Try
    End Sub

    Private Shared Function ExtraerAccountObjId(accountPoid As String) As Long
        If String.IsNullOrWhiteSpace(accountPoid) Then Return Nothing

        Dim cleaned As String = accountPoid.Trim()

        Dim idx As Integer = cleaned.IndexOf("/account", StringComparison.OrdinalIgnoreCase)
        If idx >= 0 Then
            Dim tail As String = cleaned.Substring(idx + 8).Trim()
            Dim parts = tail.Split(New Char() {" "c, "/"c}, StringSplitOptions.RemoveEmptyEntries)
            For Each part In parts
                Dim value As Long
                If Long.TryParse(part, value) Then
                    Return value
                End If
            Next
        End If

        Dim numericOnly As String = New String(cleaned.Where(AddressOf Char.IsDigit).ToArray())
        Dim parsed As Long
        If Not String.IsNullOrWhiteSpace(numericOnly) AndAlso Long.TryParse(numericOnly, parsed) Then
            Return parsed
        End If

        Return Nothing
    End Function

    Private Sub Button_limpiar_Click(sender As Object, e As EventArgs) Handles Button_limpiar.Click
        ResetInterface()
    End Sub

    Private Sub ResetInterface()
        ResetCounters()
        ResetLabels()

        ' Limpia el acumulado y el grid
        _dtAcumulado = Nothing
        DataGridView1.DataSource = Nothing
        DataGridView1.Rows.Clear()
        DataGridView1.Visible = False


        If _appLogger IsNot Nothing Then
            _appLogger.Clear()
        Else
            tst_log_out?.Clear()
            tst_log_debug?.Clear()
        End If

        If ProgressBar_general IsNot Nothing Then
            ProgressBar_general.Value = 0
            ProgressBar_general.Refresh()
        End If

        TextBox_NoCuentas.Value = 1
        ComboBox_ClienteTPO?.ResetText()
        ComboBox_ProductoTPO?.ResetText()
        If ComboBox_ClienteTPO IsNot Nothing Then ComboBox_ClienteTPO.SelectedIndex = -1
        If ComboBox_ProductoTPO IsNot Nothing Then ComboBox_ProductoTPO.SelectedIndex = -1
        If CheckBox_Persistencia IsNot Nothing Then CheckBox_Persistencia.Checked = True
        If ComboBox_UF IsNot Nothing Then ComboBox_UF.SelectedIndex = -1

        Me.UseWaitCursor = False
        Me.Cursor = Cursors.Default
        If ProcesaTodo IsNot Nothing Then ProcesaTodo.Enabled = True
    End Sub

    Private Sub ResetCounters()
        _totalCreados = 0
        _totalErrores = 0
    End Sub

    Private Sub IncrementCreados()
        _totalCreados += 1
        UpdateLabels()
    End Sub

    Private Sub IncrementErrores()
        _totalErrores += 1
        UpdateLabels()
    End Sub

    Private Sub ResetLabels()
        SetLabelText(Label_TotalCreados, "0")
        SetLabelText(Label_total_errores, "0")
    End Sub

    Private Sub UpdateLabels()
        SetLabelText(Label_TotalCreados, _totalCreados.ToString())
        SetLabelText(Label_total_errores, _totalErrores.ToString())
    End Sub

    Private Sub SetLabelText(target As Label, value As String)
        If target Is Nothing Then Return
        If target.InvokeRequired Then
            target.BeginInvoke(New Action(Of Label, String)(AddressOf SetLabelText), target, value)
        Else
            target.Text = value
        End If
    End Sub


    ' Declaración de DataTable a nivel de clase (global)
    Private _dtAcumulado As DataTable

    Private Function ObtenerDatosPorPoid_Acumulativo(poid As Long) As DataTable
        Try
            ' Si es la primera vez, inicializa la estructura
            If _dtAcumulado Is Nothing Then
                _dtAcumulado = New DataTable("Acumulado")
            End If

            ' Query SQL base
            Dim sql As String =
                "SELECT 
                C.POID_ID0 AS ACCOUNT_POID,
                PA.CPF_CNPJ,
                TO_CHAR(
                    (TIMESTAMP '1970-01-01 00:00:00 +00:00' + NUMTODSINTERVAL(P.CREATED_T,'SECOND')) 
                    AT TIME ZONE 'GMT','DD.MM.YYYY HH24:MI:SS'
                ) AS DT_ATIVADO,
                A.STATE AS UF,
                DECODE(C.BUSINESS_TYPE, 1, 'CPF', 2, 'CNPJ') AS TIPO_CLIENTE,
                APT.PAY_TYPE,
                CAA.PAY_TYPE_DESC,
                CAA.AGENT AS NOME_AGENT,
                (
                    SELECT COUNT(*) 
                    FROM PIN.PURCHASED_PRODUCT_T PP
                    JOIN PIN.AC_PURCHASED_PRODUCT_T APP ON APP.PURCHASED_PRODUCT_OBJ_ID0 = PP.POID_ID0
                    JOIN PIN.SERVICE_T S ON S.POID_ID0 = PP.SERVICE_OBJ_ID0
                    WHERE PP.ACCOUNT_OBJ_ID0 = C.POID_ID0
                      AND S.STATUS = 10100
                ) AS PROD_ACTIVOS
            FROM 
                PIN.PROFILE_T P
                JOIN PIN.AC_PROFILE_ACCOUNT_T PA ON PA.OBJ_ID0 = P.POID_ID0
                JOIN PIN.ACCOUNT_NAMEINFO_T A     ON P.ACCOUNT_OBJ_ID0 = A.OBJ_ID0
                JOIN PIN.ACCOUNT_T C              ON A.OBJ_ID0 = C.POID_ID0
                LEFT JOIN PIN.AC_PAR_T APT        ON APT.ACCOUNT_OBJ_ID0 = C.POID_ID0
                LEFT JOIN PIN.CONFIG_AC_AGENTS_T CAA ON CAA.AGENT_ID = APT.AGENT_ID
            WHERE 
                C.POID_ID0 = :p_poid
            ORDER BY APT.PAY_TYPE"

            Dim pars As New Dictionary(Of String, Object) From {
                {":p_poid", poid}
            }

            ' Ejecuta la consulta
            Dim dtTemp As DataTable = _db.ExecuteDataTable(sql, pars, 30)

            ' Si no tiene columnas todavía, copia estructura y filas
            If _dtAcumulado.Columns.Count = 0 AndAlso dtTemp.Columns.Count > 0 Then
                _dtAcumulado = dtTemp.Clone()
            End If

            ' Agrega los nuevos resultados (si existen)
            For Each row As DataRow In dtTemp.Rows
                _dtAcumulado.ImportRow(row)
            Next

            AppendDebug($"[DB][INFO] POID {poid}: {_dtAcumulado.Rows.Count} registros acumulados hasta ahora.")
            Return _dtAcumulado

        Catch ex As Exception
            AppendDebug("[DB][ERROR] " & "ObtenerDatosPorPoid_Acumulativo: " & ex.Message)
            Return _dtAcumulado
        End Try
    End Function



End Class
