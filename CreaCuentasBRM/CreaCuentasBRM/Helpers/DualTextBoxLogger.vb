Option Strict On
Option Explicit On

Imports System.ComponentModel
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Helpers

    ''' <summary>
    ''' Logger que distribuye mensajes entre dos TextBox (flujo y datos) manteniendo sincronización de UI
    ''' e implementa <see cref="IAppLogger"/> para reutilizar toda la infraestructura existente.
    ''' </summary>
    Public Class DualTextBoxLogger
        Implements IAppLogger

        Private _sync As ISynchronizeInvoke
        Private _flowTextBox As TextBox
        Private _dataTextBox As TextBox
        Private _flowMaxLines As Integer = 2000
        Private _dataMaxLines As Integer = 2000
        Private _timestampFormat As String = "yyyy-MM-dd HH:mm:ss.fff"

        Public Sub New()
        End Sub

        Public Sub New(syncProvider As ISynchronizeInvoke,
                       flowTextBox As TextBox,
                       dataTextBox As TextBox,
                       Optional flowMaxLines As Integer = 2000,
                       Optional dataMaxLines As Integer = 2000,
                       Optional timestampFormat As String = "yyyy-MM-dd HH:mm:ss.fff")
            AttachFlow(syncProvider, flowTextBox, flowMaxLines)
            AttachData(syncProvider, dataTextBox, dataMaxLines)
            _timestampFormat = timestampFormat
        End Sub

        Public Sub AttachFlow(syncProvider As ISynchronizeInvoke,
                              textBox As TextBox,
                              Optional maxLines As Integer = 2000)
            _sync = syncProvider
            _flowTextBox = textBox
            _flowMaxLines = Math.Max(100, maxLines)
            PrepareTextBox(_flowTextBox)
        End Sub

        Public Sub AttachData(syncProvider As ISynchronizeInvoke,
                              textBox As TextBox,
                              Optional maxLines As Integer = 2000)
            _sync = syncProvider
            _dataTextBox = textBox
            _dataMaxLines = Math.Max(100, maxLines)
            PrepareTextBox(_dataTextBox)
        End Sub

        Public Property TimestampFormat As String
            Get
                Return _timestampFormat
            End Get
            Set(value As String)
                If Not String.IsNullOrWhiteSpace(value) Then
                    _timestampFormat = value
                End If
            End Set
        End Property

        Public Sub WriteFlow(message As String)
            If String.IsNullOrWhiteSpace(message) Then Return
            AppendLine(_flowTextBox, message, True, _flowMaxLines)
        End Sub

        Public Sub WriteDataBlock(header As String, body As String)
            If String.IsNullOrWhiteSpace(header) AndAlso String.IsNullOrWhiteSpace(body) Then Return
            AppendBlock(_dataTextBox, header, body, Nothing, True, False, _dataMaxLines)
        End Sub

        Public Sub LogError(ex As Exception, Optional context As Object = Nothing) Implements IAppLogger.LogError
            LogErrorInternal(If(ex?.Message, String.Empty), ex, context)
        End Sub

        Public Sub LogError(message As String, Optional ex As Exception = Nothing, Optional context As Object = Nothing) Implements IAppLogger.LogError
            LogErrorInternal(message, ex, context)
        End Sub

        Public Sub LogData(data As Object, Optional label As String = Nothing) Implements IAppLogger.LogData
            Dim head As String = "[DATA]" & If(String.IsNullOrWhiteSpace(label), String.Empty, " [" & label & "]")
            Dim payload As String = FormatObject(data)
            AppendBlock(_flowTextBox, head, payload, Nothing, True, False, _flowMaxLines)
        End Sub

        Public Sub LogJson(json As String, Optional label As String = Nothing) Implements IAppLogger.LogJson
            Dim pretty As String = FormatJson(json)
            Dim tag As String = "<JSON"
            If Not String.IsNullOrWhiteSpace(label) Then
                tag &= " label=\"" & label & "\""
            End If
            tag &= ">"
            Dim footer As String = "</JSON>"
            AppendBlock(_dataTextBox, tag, pretty, footer, True, True, _dataMaxLines)
        End Sub

        Public Sub LogQueryResult(sql As String, result As Object) Implements IAppLogger.LogQueryResult
            Dim sb As New StringBuilder()
            sb.AppendLine("SQL:")
            sb.AppendLine(sql)
            sb.AppendLine("RESULT:")
            sb.AppendLine(FormatObject(result))
            AppendBlock(_dataTextBox, "<QUERY>", sb.ToString().TrimEnd(), "</QUERY>", True, True, _dataMaxLines)
        End Sub

        Private Sub LogErrorInternal(message As String, ex As Exception, context As Object)
            Dim sb As New StringBuilder()
            If Not String.IsNullOrWhiteSpace(message) Then
                sb.AppendLine(message.Trim())
            End If
            If ex IsNot Nothing Then
                sb.AppendLine(ex.GetType().FullName & ": " & ex.Message)
                If Not String.IsNullOrWhiteSpace(ex.StackTrace) Then
                    sb.AppendLine("StackTrace:")
                    sb.AppendLine(ex.StackTrace)
                End If
            End If
            If context IsNot Nothing Then
                sb.AppendLine("Contexto:")
                sb.AppendLine(FormatObject(context))
            End If
            AppendBlock(_flowTextBox, "[ERROR]", sb.ToString().TrimEnd(), Nothing, True, False, _flowMaxLines)
        End Sub

        Private Sub AppendLine(tb As TextBox, message As String, includeTimestamp As Boolean, maxLines As Integer)
            If tb Is Nothing OrElse message Is Nothing Then Return
            Dim line As String = If(includeTimestamp, String.Format("{0} {1}", TimeStamp(), message), message)
            AppendCore(tb, line, maxLines)
        End Sub

        Private Sub AppendBlock(tb As TextBox,
                                 header As String,
                                 body As String,
                                 footer As String,
                                 includeHeaderTimestamp As Boolean,
                                 includeFooterTimestamp As Boolean,
                                 maxLines As Integer)
            If tb Is Nothing Then Return
            If Not String.IsNullOrWhiteSpace(header) Then
                AppendLine(tb, header, includeHeaderTimestamp, maxLines)
            End If
            If Not String.IsNullOrWhiteSpace(body) Then
                Dim normalized As String = body.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
                Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None)
                For Each raw In lines
                    AppendLine(tb, raw, False, maxLines)
                Next
            End If
            If Not String.IsNullOrWhiteSpace(footer) Then
                AppendLine(tb, footer, includeFooterTimestamp, maxLines)
            End If
        End Sub

        Private Sub AppendCore(tb As TextBox, text As String, maxLines As Integer)
            If tb Is Nothing Then Return
            If _sync IsNot Nothing AndAlso _sync.InvokeRequired Then
                Try
                    _sync.BeginInvoke(New Action(Of TextBox, String, Integer)(AddressOf AppendCore), New Object() {tb, text, maxLines})
                Catch
                End Try
                Return
            End If

            If tb.TextLength > 0 Then
                tb.AppendText(Environment.NewLine)
            End If
            tb.AppendText(text)
            TrimLines(tb, maxLines)
        End Sub

        Private Sub TrimLines(tb As TextBox, maxLines As Integer)
            If tb Is Nothing OrElse maxLines <= 0 Then Return
            Dim lines = tb.Lines
            If lines Is Nothing OrElse lines.Length <= maxLines Then Return
            Dim keep = lines.Skip(lines.Length - maxLines).ToArray()
            tb.Lines = keep
            tb.SelectionStart = tb.TextLength
            tb.ScrollToCaret()
        End Sub

        Private Sub PrepareTextBox(tb As TextBox)
            If tb Is Nothing Then Return
            tb.Multiline = True
            tb.ScrollBars = ScrollBars.Both
            tb.WordWrap = False
        End Sub

        Private Function TimeStamp() As String
            Return DateTime.Now.ToString(_timestampFormat)
        End Function

        Private Shared Function FormatJson(json As String) As String
            If String.IsNullOrWhiteSpace(json) Then Return String.Empty
            Try
                Dim token As JToken = JToken.Parse(json)
                Return token.ToString(Formatting.Indented)
            Catch
                Return json
            End Try
        End Function

        Private Shared Function FormatObject(value As Object) As String
            If value Is Nothing Then Return String.Empty

            Dim s As String = TryCast(value, String)
            If s IsNot Nothing Then Return s

            Try
                Return JsonConvert.SerializeObject(value, Formatting.Indented)
            Catch
                Return Convert.ToString(value)
            End Try
        End Function

    End Class

End Namespace
