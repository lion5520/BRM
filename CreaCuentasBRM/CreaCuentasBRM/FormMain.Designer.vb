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
        Me.Label_total_errores = New System.Windows.Forms.Label()
        Me.Label_TotalCreados = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TextBox_NoCuentas = New System.Windows.Forms.NumericUpDown()
        Me.ComboBox_ProductoTPO = New System.Windows.Forms.ComboBox()
        Me.ProgressBar_general = New System.Windows.Forms.ProgressBar()
        Me.ComboBox_ClienteTPO = New System.Windows.Forms.ComboBox()
        Me.CheckBox_Persistencia = New System.Windows.Forms.CheckBox()
        Me.ProcesaTodo = New System.Windows.Forms.Button()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.tst_log_out = New System.Windows.Forms.TextBox()
        Me.tst_log_debug = New System.Windows.Forms.TextBox()
        Me.Button_limpiar = New System.Windows.Forms.Button()
        Me.TabControl1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        CType(Me.TextBox_NoCuentas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage4.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage4)
        Me.TabControl1.Location = New System.Drawing.Point(12, 21)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1070, 537)
        Me.TabControl1.TabIndex = 0
        '
        'TabPage1
        '
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(1062, 511)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Parametros"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.Button_limpiar)
        Me.TabPage2.Controls.Add(Me.Label_total_errores)
        Me.TabPage2.Controls.Add(Me.Label_TotalCreados)
        Me.TabPage2.Controls.Add(Me.Label5)
        Me.TabPage2.Controls.Add(Me.Label4)
        Me.TabPage2.Controls.Add(Me.Label3)
        Me.TabPage2.Controls.Add(Me.Label2)
        Me.TabPage2.Controls.Add(Me.Label1)
        Me.TabPage2.Controls.Add(Me.TextBox_NoCuentas)
        Me.TabPage2.Controls.Add(Me.ComboBox_ProductoTPO)
        Me.TabPage2.Controls.Add(Me.ProgressBar_general)
        Me.TabPage2.Controls.Add(Me.ComboBox_ClienteTPO)
        Me.TabPage2.Controls.Add(Me.CheckBox_Persistencia)
        Me.TabPage2.Controls.Add(Me.ProcesaTodo)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(1062, 511)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Cuentas"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'Label_total_errores
        '
        Me.Label_total_errores.AutoSize = True
        Me.Label_total_errores.Location = New System.Drawing.Point(141, 365)
        Me.Label_total_errores.Name = "Label_total_errores"
        Me.Label_total_errores.Size = New System.Drawing.Size(73, 13)
        Me.Label_total_errores.TabIndex = 13
        Me.Label_total_errores.Text = "Total Creados"
        '
        'Label_TotalCreados
        '
        Me.Label_TotalCreados.AutoSize = True
        Me.Label_TotalCreados.Location = New System.Drawing.Point(141, 330)
        Me.Label_TotalCreados.Name = "Label_TotalCreados"
        Me.Label_TotalCreados.Size = New System.Drawing.Size(73, 13)
        Me.Label_TotalCreados.TabIndex = 12
        Me.Label_TotalCreados.Text = "Total Creados"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(53, 365)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(67, 13)
        Me.Label5.TabIndex = 11
        Me.Label5.Text = "Total Errores"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(53, 330)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(73, 13)
        Me.Label4.TabIndex = 10
        Me.Label4.Text = "Total Creados"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(34, 101)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(71, 13)
        Me.Label3.TabIndex = 9
        Me.Label3.Text = "Tipo de Pago"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(34, 33)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(78, 13)
        Me.Label2.TabIndex = 8
        Me.Label2.Text = "Tipo de Cliente"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(190, 33)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(136, 13)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "Numero de cuentas a crear"
        '
        'TextBox_NoCuentas
        '
        Me.TextBox_NoCuentas.Location = New System.Drawing.Point(194, 50)
        Me.TextBox_NoCuentas.Name = "TextBox_NoCuentas"
        Me.TextBox_NoCuentas.Size = New System.Drawing.Size(132, 20)
        Me.TextBox_NoCuentas.TabIndex = 6
        Me.TextBox_NoCuentas.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'ComboBox_ProductoTPO
        '
        Me.ComboBox_ProductoTPO.FormattingEnabled = True
        Me.ComboBox_ProductoTPO.Items.AddRange(New Object() {"1 Carton de credito", "2 Voleto", "3 DAC"})
        Me.ComboBox_ProductoTPO.Location = New System.Drawing.Point(37, 117)
        Me.ComboBox_ProductoTPO.Name = "ComboBox_ProductoTPO"
        Me.ComboBox_ProductoTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ProductoTPO.TabIndex = 4
        '
        'ProgressBar_general
        '
        Me.ProgressBar_general.Location = New System.Drawing.Point(56, 411)
        Me.ProgressBar_general.Name = "ProgressBar_general"
        Me.ProgressBar_general.Size = New System.Drawing.Size(447, 23)
        Me.ProgressBar_general.TabIndex = 3
        '
        'ComboBox_ClienteTPO
        '
        Me.ComboBox_ClienteTPO.FormattingEnabled = True
        Me.ComboBox_ClienteTPO.Items.AddRange(New Object() {"CPF", "CNPJ"})
        Me.ComboBox_ClienteTPO.Location = New System.Drawing.Point(37, 49)
        Me.ComboBox_ClienteTPO.Name = "ComboBox_ClienteTPO"
        Me.ComboBox_ClienteTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ClienteTPO.TabIndex = 2
        '
        'CheckBox_Persistencia
        '
        Me.CheckBox_Persistencia.AutoSize = True
        Me.CheckBox_Persistencia.Checked = True
        Me.CheckBox_Persistencia.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox_Persistencia.Location = New System.Drawing.Point(395, 49)
        Me.CheckBox_Persistencia.Name = "CheckBox_Persistencia"
        Me.CheckBox_Persistencia.Size = New System.Drawing.Size(108, 17)
        Me.CheckBox_Persistencia.TabIndex = 1
        Me.CheckBox_Persistencia.Text = "Modo Persistente"
        Me.CheckBox_Persistencia.UseVisualStyleBackColor = True
        '
        'ProcesaTodo
        '
        Me.ProcesaTodo.Location = New System.Drawing.Point(419, 137)
        Me.ProcesaTodo.Name = "ProcesaTodo"
        Me.ProcesaTodo.Size = New System.Drawing.Size(84, 52)
        Me.ProcesaTodo.TabIndex = 0
        Me.ProcesaTodo.Text = "Procesa Todo"
        Me.ProcesaTodo.UseVisualStyleBackColor = True
        '
        'TabPage4
        '
        Me.TabPage4.Controls.Add(Me.tst_log_out)
        Me.TabPage4.Controls.Add(Me.tst_log_debug)
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(1062, 511)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "Log_Debug"
        Me.TabPage4.UseVisualStyleBackColor = True
        '
        'tst_log_out
        '
        Me.tst_log_out.Location = New System.Drawing.Point(523, 6)
        Me.tst_log_out.Multiline = True
        Me.tst_log_out.Name = "tst_log_out"
        Me.tst_log_out.Size = New System.Drawing.Size(533, 499)
        Me.tst_log_out.TabIndex = 2
        '
        'tst_log_debug
        '
        Me.tst_log_debug.Location = New System.Drawing.Point(6, 6)
        Me.tst_log_debug.Multiline = True
        Me.tst_log_debug.Name = "tst_log_debug"
        Me.tst_log_debug.Size = New System.Drawing.Size(511, 499)
        Me.tst_log_debug.TabIndex = 1
        '
        'Button_limpiar
        '
        Me.Button_limpiar.Location = New System.Drawing.Point(419, 440)
        Me.Button_limpiar.Name = "Button_limpiar"
        Me.Button_limpiar.Size = New System.Drawing.Size(84, 52)
        Me.Button_limpiar.TabIndex = 14
        Me.Button_limpiar.Text = "Limpiar"
        Me.Button_limpiar.UseVisualStyleBackColor = True
        '
        'FormMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1094, 570)
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "FormMain"
        Me.Text = "FormMain"
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage2.PerformLayout()
        CType(Me.TextBox_NoCuentas, System.ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents TabPage4 As TabPage
    Friend WithEvents tst_log_debug As TextBox
    Friend WithEvents tst_log_out As TextBox
    Friend WithEvents Label_total_errores As Label
    Friend WithEvents Label_TotalCreados As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents TextBox_NoCuentas As NumericUpDown
    Friend WithEvents Button_limpiar As Button
End Class
