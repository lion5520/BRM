<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMain
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.TextBox_NoCuentas = New System.Windows.Forms.TextBox()
        Me.ComboBox_ProductoTPO = New System.Windows.Forms.ComboBox()
        Me.ProgressBar_general = New System.Windows.Forms.ProgressBar()
        Me.ComboBox_ClienteTPO = New System.Windows.Forms.ComboBox()
        Me.CheckBox_Persistencia = New System.Windows.Forms.CheckBox()
        Me.ProcesaTodo = New System.Windows.Forms.Button()
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.tst_log_out = New System.Windows.Forms.TextBox()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.tst_log_debug = New System.Windows.Forms.TextBox()
        Me.TabControl1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.TabPage4.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Controls.Add(Me.TabPage4)
        Me.TabControl1.Location = New System.Drawing.Point(12, 21)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(853, 499)
        Me.TabControl1.TabIndex = 0
        '
        'TabPage1
        '
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(845, 473)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Parametros"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.TextBox_NoCuentas)
        Me.TabPage2.Controls.Add(Me.ComboBox_ProductoTPO)
        Me.TabPage2.Controls.Add(Me.ProgressBar_general)
        Me.TabPage2.Controls.Add(Me.ComboBox_ClienteTPO)
        Me.TabPage2.Controls.Add(Me.CheckBox_Persistencia)
        Me.TabPage2.Controls.Add(Me.ProcesaTodo)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(845, 473)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Cuentas"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'TextBox_NoCuentas
        '
        Me.TextBox_NoCuentas.Location = New System.Drawing.Point(126, 231)
        Me.TextBox_NoCuentas.Name = "TextBox_NoCuentas"
        Me.TextBox_NoCuentas.Size = New System.Drawing.Size(100, 20)
        Me.TextBox_NoCuentas.TabIndex = 5
        '
        'ComboBox_ProductoTPO
        '
        Me.ComboBox_ProductoTPO.FormattingEnabled = True
        Me.ComboBox_ProductoTPO.Location = New System.Drawing.Point(106, 107)
        Me.ComboBox_ProductoTPO.Name = "ComboBox_ProductoTPO"
        Me.ComboBox_ProductoTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ProductoTPO.TabIndex = 4
        '
        'ProgressBar_general
        '
        Me.ProgressBar_general.Location = New System.Drawing.Point(138, 356)
        Me.ProgressBar_general.Name = "ProgressBar_general"
        Me.ProgressBar_general.Size = New System.Drawing.Size(100, 23)
        Me.ProgressBar_general.TabIndex = 3
        '
        'ComboBox_ClienteTPO
        '
        Me.ComboBox_ClienteTPO.FormattingEnabled = True
        Me.ComboBox_ClienteTPO.Items.AddRange(New Object() {"CPF", "CNPJ"})
        Me.ComboBox_ClienteTPO.Location = New System.Drawing.Point(106, 64)
        Me.ComboBox_ClienteTPO.Name = "ComboBox_ClienteTPO"
        Me.ComboBox_ClienteTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ClienteTPO.TabIndex = 2
        '
        'CheckBox_Persistencia
        '
        Me.CheckBox_Persistencia.AutoSize = True
        Me.CheckBox_Persistencia.Location = New System.Drawing.Point(303, 47)
        Me.CheckBox_Persistencia.Name = "CheckBox_Persistencia"
        Me.CheckBox_Persistencia.Size = New System.Drawing.Size(81, 17)
        Me.CheckBox_Persistencia.TabIndex = 1
        Me.CheckBox_Persistencia.Text = "CheckBox1"
        Me.CheckBox_Persistencia.UseVisualStyleBackColor = True
        '
        'ProcesaTodo
        '
        Me.ProcesaTodo.Location = New System.Drawing.Point(559, 142)
        Me.ProcesaTodo.Name = "ProcesaTodo"
        Me.ProcesaTodo.Size = New System.Drawing.Size(75, 23)
        Me.ProcesaTodo.TabIndex = 0
        Me.ProcesaTodo.Text = "Button1"
        Me.ProcesaTodo.UseVisualStyleBackColor = True
        '
        'TabPage3
        '
        Me.TabPage3.Controls.Add(Me.tst_log_out)
        Me.TabPage3.Location = New System.Drawing.Point(4, 22)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(845, 473)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "Log_Out"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'tst_log_out
        '
        Me.tst_log_out.Location = New System.Drawing.Point(6, 6)
        Me.tst_log_out.Multiline = True
        Me.tst_log_out.Name = "tst_log_out"
        Me.tst_log_out.Size = New System.Drawing.Size(833, 461)
        Me.tst_log_out.TabIndex = 1
        '
        'TabPage4
        '
        Me.TabPage4.Controls.Add(Me.tst_log_debug)
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(845, 473)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "Log_Debug"
        Me.TabPage4.UseVisualStyleBackColor = True
        '
        'tst_log_debug
        '
        Me.tst_log_debug.Location = New System.Drawing.Point(6, 6)
        Me.tst_log_debug.Multiline = True
        Me.tst_log_debug.Name = "tst_log_debug"
        Me.tst_log_debug.Size = New System.Drawing.Size(833, 461)
        Me.tst_log_debug.TabIndex = 1
        '
        'CompraProductos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(877, 532)
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "CompraProductos"
        Me.Text = "FormMain"
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage2.PerformLayout()
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage3.PerformLayout()
        Me.TabPage4.ResumeLayout(False)
        Me.TabPage4.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents ComboBox_ClienteTPO As ComboBox
    Friend WithEvents CheckBox_Persistencia As CheckBox
    Friend WithEvents ProcesaTodo As Button
    Friend WithEvents ProgressBar_general As ProgressBar
    Friend WithEvents ComboBox_ProductoTPO As ComboBox
    Friend WithEvents TabPage3 As TabPage
    Friend WithEvents tst_log_out As TextBox
    Friend WithEvents TabPage4 As TabPage
    Friend WithEvents tst_log_debug As TextBox
    Friend WithEvents TextBox_NoCuentas As TextBox
End Class
