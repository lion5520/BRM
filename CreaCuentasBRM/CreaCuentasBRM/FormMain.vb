Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks

Public Class FormMain

    Private ReadOnly _creador As New CreaCliente()
    Private ReadOnly _comprador As New CompraProductos()
    Private ReadOnly _bole As New BolecodeResponse()
    Private _debugLogger As UiTextBoxLogger

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Engancha canal OUT
        _creador.OnOut = AddressOf OutSink
        _comprador.OnOut = AddressOf OutSink
        _bole.OnOut = AddressOf OutSink

        If tst_log_debug IsNot Nothing AndAlso _debugLogger Is Nothing Then
            _debugLogger = New UiTextBoxLogger(Me, tst_log_debug)
            _creador.Logger = _debugLogger
            _comprador.Logger = _debugLogger
            _bole.Logger = _debugLogger
        End If

        If ProgressBar_general IsNot Nothing Then
            ProgressBar_general.Minimum = 0
            ProgressBar_general.Maximum = 100
            ProgressBar_general.Value = 0
        End If

        AppendDebug("[DATA] [INIT] Form listo.")
    End Sub

    Private Sub OutSink(line As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New Action(Of String)(AddressOf OutSink), line)
            Return
        End If
        If tst_log_out IsNot Nothing Then
            tst_log_out.AppendText(line & Environment.NewLine)
        End If
    End Sub

    Private Sub AppendDebug(line As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New Action(Of String)(AddressOf AppendDebug), line)
            Return
        End If
        If _debugLogger IsNot Nothing Then
            _debugLogger.LogData(line, "FLOW")
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
            If TextBox_NoCuentas IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(TextBox_NoCuentas.Text) Then
                Dim tmp As Integer
                If Integer.TryParse(TextBox_NoCuentas.Text.Trim(), tmp) AndAlso tmp > 0 Then nCuentas = tmp
            End If

            AppendDebug("[DATA] [FLOW] Inicio. Cuentas=" & nCuentas.ToString() & " Persistencia=" & doPersist.ToString())

            If ProgressBar_general IsNot Nothing Then
                ProgressBar_general.Minimum = 0
                ProgressBar_general.Maximum = Math.Max(1, nCuentas * 3)
                ProgressBar_general.Value = 0
                ProgressBar_general.Refresh()
            End If

            For i As Integer = 1 To nCuentas
                AppendDebug("[DATA] [FLOW] --- INICIO #" & i.ToString() & "/" & nCuentas.ToString() & " ---")

                If ProgressBar_general IsNot Nothing Then
                    Dim baseStep As Integer = (i - 1) * 3
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, baseStep)
                    ProgressBar_general.Refresh()
                End If

                ' Tipo cliente desde combo
                Dim tipoCliente As CreaCliente.TipoCliente = CreaCliente.TipoCliente.PF
                If ComboBox_ClienteTPO IsNot Nothing AndAlso ComboBox_ClienteTPO.SelectedItem IsNot Nothing Then
                    Dim s As String = ComboBox_ClienteTPO.SelectedItem.ToString().Trim().ToUpperInvariant()
                    If s = "PJ" Then tipoCliente = CreaCliente.TipoCliente.PJ
                End If
                AppendDebug("[DATA] [FLOW] Tipo Cliente: " & tipoCliente.ToString())

                ' Crear
                AppendDebug("[DEBUG] [CREATE] Llamando CreaCliente…")
                Dim rc As CrearClienteResult = Await _creador.CrearAsync(tipoCliente, Nothing, doPersist)
                If rc Is Nothing OrElse Not rc.Success Then
                    AppendDebug("[DEBUG] [ABORT] " & If(String.IsNullOrWhiteSpace(_creador.ErrorMessage),
                                                         "No se pudo crear el cliente. Proceso detenido.",
                                                         _creador.ErrorMessage))
                    AppendDebug("[DEBUG] [DONE] Proceso interrumpido antes de completar los pasos.")
                    If ProgressBar_general IsNot Nothing Then
                        ProgressBar_general.Value = 0
                        ProgressBar_general.Refresh()
                    End If
                    Exit Sub
                End If
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, ProgressBar_general.Value + 1)
                    ProgressBar_general.Refresh()
                End If

                ' Tipo compra
                Dim pay As CompraProductos.PayType = CompraProductos.PayType.Boleto
                If ComboBox_ProductoTPO IsNot Nothing AndAlso ComboBox_ProductoTPO.SelectedItem IsNot Nothing Then
                    Dim p As String = ComboBox_ProductoTPO.SelectedItem.ToString().Trim().ToUpperInvariant()
                    Select Case p
                        Case "CREDITCARD", "CREDIT CARD", "CC" : pay = CompraProductos.PayType.CreditCard
                        Case "DAC" : pay = CompraProductos.PayType.DAC
                        Case Else : pay = CompraProductos.PayType.Boleto
                    End Select
                End If

                ' Comprar
                AppendDebug("[DEBUG] [PURCHASE] Llamando CompraProductos…")
                Dim rb As CompraProductosResult = Await _comprador.ComprarAsync(rc.AccountPoid, pay, doPersist, Nothing)
                If rb Is Nothing OrElse Not rb.Success Then
                    AppendDebug("[DEBUG] [ABORT] " & If(String.IsNullOrWhiteSpace(_comprador.ErrorMessage),
                                                         "No se pudo realizar la compra. Proceso detenido.",
                                                         _comprador.ErrorMessage))
                    AppendDebug("[DEBUG] [DONE] Proceso interrumpido antes de completar los pasos.")
                    If ProgressBar_general IsNot Nothing Then
                        ProgressBar_general.Value = 0
                        ProgressBar_general.Refresh()
                    End If
                    Exit Sub
                End If
                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, ProgressBar_general.Value + 1)
                    ProgressBar_general.Refresh()
                End If

                ' Bolecode
                AppendDebug("[DEBUG] [PAYMENT] Llamando BolecodeResponse…")
                Dim rp As BolecodeResponseResult = Await _bole.ActualizarAsync(rc.AccountPoid, doPersist)
                If rp Is Nothing OrElse Not rp.Success Then
                    AppendDebug("[DEBUG] [ABORT] " & If(String.IsNullOrWhiteSpace(_bole.ErrorMessage),
                                                         "No se pudo actualizar bolecode. Proceso detenido.",
                                                         _bole.ErrorMessage))
                    AppendDebug("[DEBUG] [DONE] Proceso interrumpido antes de completar los pasos.")
                    If ProgressBar_general IsNot Nothing Then
                        ProgressBar_general.Value = 0
                        ProgressBar_general.Refresh()
                    End If
                    Exit Sub
                End If

                If ProgressBar_general IsNot Nothing Then
                    ProgressBar_general.Value = Math.Min(ProgressBar_general.Maximum, ProgressBar_general.Value + 1)
                    ProgressBar_general.Refresh()
                End If
                AppendDebug("[DATA] [FLOW] Iteración #" & i.ToString() & " completada.")
            Next

            AppendDebug("[DATA] [DONE] Proceso completado.")
        Catch ex As Exception
            AppendDebug("[ERROR] " & ex.Message)
            If _debugLogger IsNot Nothing Then
                _debugLogger.LogError(ex.Message, ex)
            End If
        Finally
            Me.UseWaitCursor = previousUseWait
            Me.Cursor = previousCursor
            If ProcesaTodo IsNot Nothing Then ProcesaTodo.Enabled = True
        End Try
    End Sub

End Class
