Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Data
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Implementación de IAppLogger que escribe en un TextBox multiline,
''' SIN archivos. Thread-safe (marshaling al hilo UI). Mantiene un
''' máximo de líneas para evitar crecimiento infinito.
''' </summary>
Public NotInheritable Class UiTextBoxLogger
    Implements IAppLogger

    Private ReadOnly _sync As ISynchronizeInvoke
    Private ReadOnly _tb As TextBox
    Private ReadOnly _maxLines As Integer
    Private ReadOnly _tsFormat As String

    ''' <param name="syncProvider">Cualquier Control del formulario principal (por ejemplo: Me)</param>
    ''' <param name="targetTextBox">TextBox multiline de salida</param>
    ''' <param name="maxLines">Máximo de líneas a conservar en el TextBox</param>
    ''' <param name="timestampFormat">Formato de timestamp por línea</param>
    Public Sub New(syncProvider As ISynchronizeInvoke,
                   targetTextBox As TextBox,
                   Optional maxLines As Integer = 2000,
                   Optional timestampFormat As String = "yyyy-MM-dd HH:mm:ss.fff")

        If syncProvider Is Nothing Then Throw New ArgumentNullException(NameOf(syncProvider))
        If targetTextBox Is Nothing Then Throw New ArgumentNullException(NameOf(targetTextBox))
        _sync = syncProvider
        _tb = targetTextBox
        _maxLines = Math.Max(100, maxLines)
        _tsFormat = If(String.IsNullOrWhiteSpace(timestampFormat), "yyyy-MM-dd HH:mm:ss.fff", timestampFormat)

        ' Asegura configuración mínima del TextBox
        _tb.Multiline = True
        _tb.ScrollBars = ScrollBars.Both
        _tb.WordWrap = False
    End Sub

    ' ==================== API: Errores/Excepciones ====================
    Public Sub LogError(ex As Exception, Optional context As Object = Nothing) Implements IAppLogger.LogError
        AppendLineSafe(ComposeErrorLine(Date.Now, Nothing, ex, context))
    End Sub

    Public Sub LogError(message As String, Optional ex As Exception = Nothing, Optional context As Object = Nothing) Implements IAppLogger.LogError
        AppendLineSafe(ComposeErrorLine(Date.Now, message, ex, context))
    End Sub

    ' ==================== API: Datos/JSON/Resultados ====================
    Public Sub LogData(data As Object, Optional label As String = Nothing) Implements IAppLogger.LogData
        Dim head As String = $"{TimeStamp(Date.Now)} [DATA]{If(String.IsNullOrEmpty(label), "", " [" & label & "]")}"
        Dim body As String = ToPrettyString(data)
        Dim line As String = If(body.Length = 0, head, head & Environment.NewLine & body)
        AppendLineSafe(line)
    End Sub

    Public Sub LogJson(json As String, Optional label As String = Nothing) Implements IAppLogger.LogJson
        Dim safeJson As String = If(json, String.Empty)
        Dim composed As String = $"{TimeStamp(Date.Now)} [JSON]{If(String.IsNullOrEmpty(label), "", " [" & label & "]")} {safeJson}"
        AppendLineSafe(composed)
    End Sub

    Public Sub LogQueryResult(sql As String, result As Object) Implements IAppLogger.LogQueryResult
        Dim sb As New StringBuilder(1024)
        sb.Append(TimeStamp(Date.Now)).Append(" [QUERY]").AppendLine()
        sb.Append("SQL: ").AppendLine(If(sql, String.Empty))
        sb.Append("RESULT: ").AppendLine(ToPrettyString(result))
        AppendLineSafe(sb.ToString())
    End Sub

    ' ==================== Helpers de composición ====================
    Private Function TimeStamp(moment As DateTime) As String
        Return moment.ToString(_tsFormat)
    End Function

    Private Function ComposeErrorLine(moment As DateTime, msg As String, ex As Exception, ctx As Object) As String
        Dim sb As New StringBuilder(1024)
        sb.Append(TimeStamp(moment)).Append(" [ERROR] ")
        If Not String.IsNullOrWhiteSpace(msg) Then
            sb.Append(msg)
        End If
        If ex IsNot Nothing Then
            If sb.Length > 0 Then sb.AppendLine()
            sb.Append("Exception: ").Append(ex.GetType().FullName).Append(": ").Append(ex.Message).AppendLine()
            If ex.StackTrace IsNot Nothing Then
                sb.AppendLine("StackTrace:")
                sb.AppendLine(ex.StackTrace)
            End If
            Dim inner = ex.InnerException
            Dim level As Integer = 0
            While inner IsNot Nothing AndAlso level < 3
                sb.AppendLine("-- InnerException --")
                sb.Append(inner.GetType().FullName).Append(": ").Append(inner.Message).AppendLine()
                If inner.StackTrace IsNot Nothing Then sb.AppendLine(inner.StackTrace)
                inner = inner.InnerException
                level += 1
            End While
        End If
        If ctx IsNot Nothing Then
            sb.AppendLine("Context:")
            sb.AppendLine(ToPrettyString(ctx))
        End If
        Return sb.ToString()
    End Function

    ' ==================== Serialización básica ====================
    Private Shared Function ToPrettyString(obj As Object) As String
        If obj Is Nothing Then Return "(null)"

        Dim s As String = TryCast(obj, String)
        If s IsNot Nothing Then Return s

        Dim ex As Exception = TryCast(obj, Exception)
        If ex IsNot Nothing Then
            Return ex.GetType().FullName & ": " & ex.Message & Environment.NewLine & ex.StackTrace
        End If

        Dim dt As DataTable = TryCast(obj, DataTable)
        If dt IsNot Nothing Then
            Return DataTableToCsv(dt, 200)
        End If

        Dim dict As IDictionary = TryCast(obj, IDictionary)
        If dict IsNot Nothing Then
            Dim sb As New StringBuilder(512)
            sb.AppendLine("{")
            Dim written As Integer = 0
            For Each key In dict.Keys
                sb.Append("  ").Append(Convert.ToString(key)).Append(": ").AppendLine(Convert.ToString(dict(key)))
                written += 1
                If written >= 200 Then
                    sb.AppendLine("  ... (truncado)")
                    Exit For
                End If
            Next
            sb.Append("}")
            Return sb.ToString()
        End If

        Dim en As IEnumerable = TryCast(obj, IEnumerable)
        If en IsNot Nothing AndAlso Not TypeOf obj Is String Then
            Dim sb As New StringBuilder(512)
            sb.AppendLine("[")
            Dim count As Integer = 0
            For Each item In en
                sb.Append("  - ").AppendLine(Convert.ToString(item))
                count += 1
                If count >= 200 Then
                    sb.AppendLine("  ... (truncado)")
                    Exit For
                End If
            Next
            sb.Append("]")
            Return sb.ToString()
        End If

        Return Convert.ToString(obj)
    End Function

    Private Shared Function DataTableToCsv(dt As DataTable, maxRows As Integer) As String
        Dim sb As New StringBuilder(Math.Max(1024, dt.Rows.Count * (dt.Columns.Count * 4 + 4)))
        ' Encabezados
        For i As Integer = 0 To dt.Columns.Count - 1
            sb.Append(EscapeCsvField(dt.Columns(i).ColumnName))
            If i < dt.Columns.Count - 1 Then sb.Append(","c)
        Next
        sb.AppendLine()
        ' Filas limitadas
        Dim rowCount As Integer = Math.Min(dt.Rows.Count, maxRows)
        For r As Integer = 0 To rowCount - 1
            Dim dr As DataRow = dt.Rows(r)
            For c As Integer = 0 To dt.Columns.Count - 1
                Dim v As Object = dr(c)
                Dim cell As String = If(v Is Nothing OrElse v Is DBNull.Value, "", Convert.ToString(v))
                sb.Append(EscapeCsvField(cell))
                If c < dt.Columns.Count - 1 Then sb.Append(","c)
            Next
            sb.AppendLine()
        Next
        If dt.Rows.Count > rowCount Then
            sb.AppendLine("... (truncado)")
        End If
        Return sb.ToString()
    End Function

    Private Shared Function EscapeCsvField(s As String) As String
        If s Is Nothing Then Return ""
        Dim needs As Boolean = (s.IndexOf(","c) >= 0) OrElse (s.IndexOf(""""c) >= 0) OrElse s.Contains(Environment.NewLine)
        Dim v As String = s.Replace("""", """""")
        If needs Then
            Return """" & v & """"
        End If
        Return v
    End Function

    ' ==================== UI Append + truncado ====================
    Private Sub AppendLineSafe(text As String)
        If _sync.InvokeRequired Then
            _sync.BeginInvoke(New Action(Of String)(AddressOf AppendLineUnsafe), New Object() {text})
        Else
            AppendLineUnsafe(text)
        End If
    End Sub

    Private Sub AppendLineUnsafe(text As String)
        ' Agrega
        _tb.AppendText(text & Environment.NewLine)
        ' Trunca si excede
        Dim lines = _tb.Lines
        If lines.Length > _maxLines Then
            Dim take As Integer = _maxLines
            Dim newLines(take - 1) As String
            Array.Copy(lines, lines.Length - take, newLines, 0, take)
            _tb.Lines = newLines
            _tb.SelectionStart = _tb.TextLength
            _tb.ScrollToCaret()
        End If
    End Sub

End Class
