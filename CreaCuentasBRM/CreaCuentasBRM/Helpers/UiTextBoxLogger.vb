Option Strict On
Option Explicit On

Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Text

Namespace CreaCuentasBRM

    ''' <summary>
    ''' Logger para enviar texto a 1 o 2 TextBox del formulario con sincronización de UI.
    ''' Métodos: AttachPrimary, AttachSecondary, WriteDebug, WriteData.
    ''' </summary>
    Public Class UiTextBoxLogger

        Private _sync As ISynchronizeInvoke
        Private _tbDebug As TextBox
        Private _tbData As TextBox
        Private _maxLines As Integer = 2000
        Private _tsFmt As String = "yyyy-MM-dd HH:mm:ss.fff"

        ' ========= Constructores =========
        Public Sub New()
            ' Permite New UiTextBoxLogger() (tu FormMain lo usa).
        End Sub

        Public Sub New(syncProvider As ISynchronizeInvoke,
                       targetTextBox As TextBox,
                       Optional maxLines As Integer = 2000,
                       Optional timestampFormat As String = "yyyy-MM-dd HH:mm:ss.fff")
            _sync = syncProvider
            _tbDebug = targetTextBox
            _maxLines = maxLines
            _tsFmt = timestampFormat
        End Sub

        ' ========= Attach / Config =========
        Public Sub AttachPrimary(syncProvider As ISynchronizeInvoke, tb As TextBox)
            _sync = syncProvider
            _tbDebug = tb
        End Sub

        ''' <summary>
        ''' Segundo TextBox para volcar JSON, respuestas, consultas, etc.
        ''' </summary>
        Public Sub AttachSecondary(syncProvider As ISynchronizeInvoke, tb As TextBox)
            _sync = syncProvider
            _tbData = tb
        End Sub

        Public Property MaxLines As Integer
            Get
                Return _maxLines
            End Get
            Set(value As Integer)
                _maxLines = Math.Max(100, value)
            End Set
        End Property

        Public Property TimestampFormat As String
            Get
                Return _tsFmt
            End Get
            Set(value As String)
                If Not String.IsNullOrWhiteSpace(value) Then _tsFmt = value
            End Set
        End Property

        ' ========= API de Log =========
        Public Sub WriteDebug(message As String)
            AppendLine(_tbDebug, message)
        End Sub

        Public Sub WriteData(message As String)
            AppendLine(_tbData, message)
        End Sub

        ' ========= Internos =========
        Private Sub AppendLine(tb As TextBox, message As String)
            If tb Is Nothing Then Return

            Dim line As String = $"{DateTime.Now.ToString(_tsFmt)} {message}"

            If _sync IsNot Nothing AndAlso _sync.InvokeRequired Then
                Try
                    _sync.BeginInvoke(New Action(Of TextBox, String)(AddressOf AppendLineCore),
                                      New Object() {tb, line})
                Catch
                    ' Silencio en caso de cierre de formulario
                End Try
            Else
                AppendLineCore(tb, line)
            End If
        End Sub

        Private Sub AppendLineCore(tb As TextBox, line As String)
            If tb.TextLength > 0 Then tb.AppendText(Environment.NewLine)
            tb.AppendText(line)

            ' Control de número de líneas (simple)
            Dim lines = tb.Lines
            If lines Is Nothing Then Return
            If lines.Length > _maxLines Then
                Dim toKeep = Math.Min(lines.Length, _maxLines)
                Dim kept = lines.Skip(lines.Length - toKeep).ToArray()
                tb.Lines = kept
                tb.SelectionStart = tb.TextLength
                tb.ScrollToCaret()
            End If
        End Sub

    End Class

End Namespace
