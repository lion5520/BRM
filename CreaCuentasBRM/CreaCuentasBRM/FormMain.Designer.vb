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
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.TextBox_NoCuentas = New System.Windows.Forms.NumericUpDown()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Button_limpiar = New System.Windows.Forms.Button()
        Me.Label_total_errores = New System.Windows.Forms.Label()
        Me.Label_TotalCreados = New System.Windows.Forms.Label()
        Me.ComboBox_ProductoTPO = New System.Windows.Forms.ComboBox()
        Me.ProgressBar_general = New System.Windows.Forms.ProgressBar()
        Me.ComboBox_ClienteTPO = New System.Windows.Forms.ComboBox()
        Me.CheckBox_Persistencia = New System.Windows.Forms.CheckBox()
        Me.ProcesaTodo = New System.Windows.Forms.Button()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.tst_log_out = New System.Windows.Forms.TextBox()
        Me.tst_log_debug = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.ComboBox_UF = New System.Windows.Forms.ComboBox()
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
        Me.TabControl1.Size = New System.Drawing.Size(1131, 526)
        Me.TabControl1.TabIndex = 0
        '
        'TabPage1
        '
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(1123, 500)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Parametros"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.ComboBox_UF)
        Me.TabPage2.Controls.Add(Me.Label6)
        Me.TabPage2.Controls.Add(Me.Label5)
        Me.TabPage2.Controls.Add(Me.Label4)
        Me.TabPage2.Controls.Add(Me.Label3)
        Me.TabPage2.Controls.Add(Me.TextBox_NoCuentas)
        Me.TabPage2.Controls.Add(Me.Label2)
        Me.TabPage2.Controls.Add(Me.Label1)
        Me.TabPage2.Controls.Add(Me.Button_limpiar)
        Me.TabPage2.Controls.Add(Me.Label_total_errores)
        Me.TabPage2.Controls.Add(Me.Label_TotalCreados)
        Me.TabPage2.Controls.Add(Me.ComboBox_ProductoTPO)
        Me.TabPage2.Controls.Add(Me.ProgressBar_general)
        Me.TabPage2.Controls.Add(Me.ComboBox_ClienteTPO)
        Me.TabPage2.Controls.Add(Me.CheckBox_Persistencia)
        Me.TabPage2.Controls.Add(Me.ProcesaTodo)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(1123, 500)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Cuentas"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(34, 324)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(91, 13)
        Me.Label5.TabIndex = 14
        Me.Label5.Text = "Cuentas Erroneas"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(34, 287)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(88, 13)
        Me.Label4.TabIndex = 13
        Me.Label4.Text = "Cuentas Creadas"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(181, 222)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(104, 13)
        Me.Label3.TabIndex = 12
        Me.Label3.Text = "Numero de Cuentas "
        '
        'TextBox_NoCuentas
        '
        Me.TextBox_NoCuentas.Location = New System.Drawing.Point(184, 240)
        Me.TextBox_NoCuentas.Name = "TextBox_NoCuentas"
        Me.TextBox_NoCuentas.Size = New System.Drawing.Size(120, 20)
        Me.TextBox_NoCuentas.TabIndex = 11
        Me.TextBox_NoCuentas.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(34, 103)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(71, 13)
        Me.Label2.TabIndex = 10
        Me.Label2.Text = "Tipo de Pago"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(34, 25)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(77, 13)
        Me.Label1.TabIndex = 9
        Me.Label1.Text = "Tipo de cliente"
        '
        'Button_limpiar
        '
        Me.Button_limpiar.Location = New System.Drawing.Point(332, 413)
        Me.Button_limpiar.Name = "Button_limpiar"
        Me.Button_limpiar.Size = New System.Drawing.Size(112, 32)
        Me.Button_limpiar.TabIndex = 8
        Me.Button_limpiar.Text = "Limpiar"
        Me.Button_limpiar.UseVisualStyleBackColor = True
        '
        'Label_total_errores
        '
        Me.Label_total_errores.AutoSize = True
        Me.Label_total_errores.Location = New System.Drawing.Point(145, 324)
        Me.Label_total_errores.Name = "Label_total_errores"
        Me.Label_total_errores.Size = New System.Drawing.Size(13, 13)
        Me.Label_total_errores.TabIndex = 7
        Me.Label_total_errores.Text = "0"
        '
        'Label_TotalCreados
        '
        Me.Label_TotalCreados.AutoSize = True
        Me.Label_TotalCreados.Location = New System.Drawing.Point(145, 287)
        Me.Label_TotalCreados.Name = "Label_TotalCreados"
        Me.Label_TotalCreados.Size = New System.Drawing.Size(13, 13)
        Me.Label_TotalCreados.TabIndex = 6
        Me.Label_TotalCreados.Text = "0"
        '
        'ComboBox_ProductoTPO
        '
        Me.ComboBox_ProductoTPO.FormattingEnabled = True
        Me.ComboBox_ProductoTPO.Items.AddRange(New Object() {"1. CreditCard", "2. Boleto", "3. DAC"})
        Me.ComboBox_ProductoTPO.Location = New System.Drawing.Point(37, 119)
        Me.ComboBox_ProductoTPO.Name = "ComboBox_ProductoTPO"
        Me.ComboBox_ProductoTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ProductoTPO.TabIndex = 4
        '
        'ProgressBar_general
        '
        Me.ProgressBar_general.Location = New System.Drawing.Point(37, 384)
        Me.ProgressBar_general.Name = "ProgressBar_general"
        Me.ProgressBar_general.Size = New System.Drawing.Size(407, 23)
        Me.ProgressBar_general.TabIndex = 3
        '
        'ComboBox_ClienteTPO
        '
        Me.ComboBox_ClienteTPO.FormattingEnabled = True
        Me.ComboBox_ClienteTPO.Items.AddRange(New Object() {"CPF", "CNPJ"})
        Me.ComboBox_ClienteTPO.Location = New System.Drawing.Point(37, 43)
        Me.ComboBox_ClienteTPO.Name = "ComboBox_ClienteTPO"
        Me.ComboBox_ClienteTPO.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox_ClienteTPO.TabIndex = 2
        '
        'CheckBox_Persistencia
        '
        Me.CheckBox_Persistencia.AutoSize = True
        Me.CheckBox_Persistencia.Checked = True
        Me.CheckBox_Persistencia.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox_Persistencia.Location = New System.Drawing.Point(382, 47)
        Me.CheckBox_Persistencia.Name = "CheckBox_Persistencia"
        Me.CheckBox_Persistencia.Size = New System.Drawing.Size(63, 17)
        Me.CheckBox_Persistencia.TabIndex = 1
        Me.CheckBox_Persistencia.Text = "Persiste"
        Me.CheckBox_Persistencia.UseVisualStyleBackColor = True
        '
        'ProcesaTodo
        '
        Me.ProcesaTodo.Location = New System.Drawing.Point(332, 232)
        Me.ProcesaTodo.Name = "ProcesaTodo"
        Me.ProcesaTodo.Size = New System.Drawing.Size(112, 32)
        Me.ProcesaTodo.TabIndex = 0
        Me.ProcesaTodo.Text = "Crear Todo"
        Me.ProcesaTodo.UseVisualStyleBackColor = True
        '
        'TabPage4
        '
        Me.TabPage4.Controls.Add(Me.tst_log_out)
        Me.TabPage4.Controls.Add(Me.tst_log_debug)
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(1123, 500)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "Log_Debug"
        Me.TabPage4.UseVisualStyleBackColor = True
        '
        'tst_log_out
        '
        Me.tst_log_out.Location = New System.Drawing.Point(551, 6)
        Me.tst_log_out.Multiline = True
        Me.tst_log_out.Name = "tst_log_out"
        Me.tst_log_out.Size = New System.Drawing.Size(566, 488)
        Me.tst_log_out.TabIndex = 2
        '
        'tst_log_debug
        '
        Me.tst_log_debug.Location = New System.Drawing.Point(6, 6)
        Me.tst_log_debug.Multiline = True
        Me.tst_log_debug.Name = "tst_log_debug"
        Me.tst_log_debug.Size = New System.Drawing.Size(530, 488)
        Me.tst_log_debug.TabIndex = 1
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(200, 25)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(57, 13)
        Me.Label6.TabIndex = 15
        Me.Label6.Text = "Estado UF"
        '
        'ComboBox_UF
        '
        Me.ComboBox_UF.FormattingEnabled = True
        Me.ComboBox_UF.Location = New System.Drawing.Point(203, 43)
        Me.ComboBox_UF.Name = "ComboBox_UF"
        Me.ComboBox_UF.Size = New System.Drawing.Size(129, 21)
        Me.ComboBox_UF.TabIndex = 16
        '
        'FormMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1155, 559)
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
    Friend WithEvents Label_TotalCreados As Label
    Friend WithEvents Label_total_errores As Label
    Friend WithEvents Button_limpiar As Button
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents TextBox_NoCuentas As NumericUpDown
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents tst_log_out As TextBox
    Friend WithEvents ComboBox_UF As ComboBox
    Friend WithEvents Label6 As Label
End Class
