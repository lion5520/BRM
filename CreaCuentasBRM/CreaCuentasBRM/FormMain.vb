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

                Dim tipoCliente As CreaCliente.TipoCliente = CreaCliente.TipoCliente.PF
                If ComboBox_ClienteTPO IsNot Nothing AndAlso ComboBox_ClienteTPO.SelectedItem IsNot Nothing Then
                    Dim s As String = ComboBox_ClienteTPO.SelectedItem.ToString().Trim().ToUpperInvariant()
                    If s = "PJ" Then tipoCliente = CreaCliente.TipoCliente.PJ
                End If

                Dim uF_Seleccionada As String
                If ComboBox_UF IsNot Nothing AndAlso ComboBox_UF.SelectedItem IsNot Nothing Then
                    uF_Seleccionada = ComboBox_UF.SelectedItem.ToString().Trim().ToUpperInvariant()
                Else
                    uF_Seleccionada = "RJ"
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
                If ComboBox_ProductoTPO IsNot Nothing AndAlso ComboBox_ProductoTPO.SelectedItem IsNot Nothing Then
                    Dim p As String = ComboBox_ProductoTPO.SelectedItem.ToString().Trim().ToUpperInvariant()
                    Select Case p
                        Case "CREDITCARD", "CREDIT CARD", "CC"
                            pay = CompraProductos.PayType.CreditCard
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
            If ProcesaTodo IsNot Nothing Then ProcesaTodo.Enabled = True
        End Try
    End Sub

    Private Sub Button_limpiar_Click(sender As Object, e As EventArgs) Handles Button_limpiar.Click
        ResetInterface()
    End Sub

    Private Sub ResetInterface()
        ResetCounters()
        ResetLabels()

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

End Class
