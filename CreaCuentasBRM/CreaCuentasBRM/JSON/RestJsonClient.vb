Option Strict On
Option Explicit On

' Requiere referencia a System.Net.Http
Imports System
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic

''' <summary>
''' Cliente REST para enviar JSON con POST (forzado).
''' - Valida endpoint y payload
''' - Timeout, reintentos con backoff
''' - Devuelve RestJsonResponse
''' - Sin "Await" dentro de Catch/Finally (compatible con VB.NET)
''' </summary>
Public NotInheritable Class RestJsonClient
    Implements IDisposable

    Private Shared ReadOnly _http As HttpClient = CreateSharedClient()
    Private _disposed As Boolean

    Public Sub New()
        ' Nada adicional; usa configuración global de RestJsonConfig
    End Sub

    Private Shared Function CreateSharedClient() As HttpClient
        Dim baseUri As Uri = BuildBaseUri(RestJsonConfig.BaseUrl)
        Dim c As New HttpClient() With {
            .BaseAddress = baseUri
        }
        If Not String.IsNullOrWhiteSpace(RestJsonConfig.ApiKey) Then
            c.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", RestJsonConfig.ApiKey)
        End If
        Return c
    End Function

    ''' <summary>
    ''' Envía JSON por POST al endpoint indicado (ruta relativa o URL absoluta).
    ''' </summary>
    Public Async Function SendJsonAsync(endpoint As String,
                                        jsonPayload As String,
                                        Optional timeoutSeconds As Integer = -1,
                                        Optional extraHeaders As Dictionary(Of String, String) = Nothing) As Task(Of RestJsonResponse)

        ValidateEndpoint(endpoint)
        ValidateJson(jsonPayload)

        Dim uri As Uri = BuildRequestUri(endpoint)
        Dim retries As Integer = Math.Max(0, RestJsonConfig.DefaultMaxRetries)
        Dim backoff As Integer = Math.Max(0, RestJsonConfig.DefaultBackoffMs)
        Dim effectiveTimeout As Integer = If(timeoutSeconds > 0, timeoutSeconds, RestJsonConfig.DefaultTimeoutSeconds)

        Dim attempt As Integer = 0

        Do
            attempt += 1

            Dim req As New HttpRequestMessage(HttpMethod.Post, uri)
            req.Content = New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            If extraHeaders IsNot Nothing Then
                For Each kvp In extraHeaders
                    If Not req.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value) Then
                        req.Content.Headers.Remove(kvp.Key)
                        req.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value)
                    End If
                Next
            End If

            Dim needRetry As Boolean = False
            Dim delayMs As Integer = 0
            Dim outRes As RestJsonResponse = Nothing

            Dim cts As New CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, effectiveTimeout)))
            Try
                Dim res As HttpResponseMessage = Await _http.SendAsync(req, cts.Token).ConfigureAwait(False)
                Dim body As String = Await res.Content.ReadAsStringAsync().ConfigureAwait(False)

                outRes = New RestJsonResponse With {
                    .Success = res.IsSuccessStatusCode,
                    .StatusCode = CInt(res.StatusCode),
                    .Body = body,
                    .ResponseHeaders = FlattenHeaders(res)
                }

                If outRes.Success Then
                    Return outRes
                End If

                ' ¿Es error transitorio?
                If attempt <= retries AndAlso IsTransient(res.StatusCode) Then
                    needRetry = True
                    delayMs = BackoffDelay(backoff, attempt)
                End If

            Catch ex As TaskCanceledException
                If attempt <= retries Then
                    needRetry = True
                    delayMs = BackoffDelay(backoff, attempt)
                Else
                    outRes = New RestJsonResponse With {
                        .Success = False,
                        .StatusCode = 0,
                        .Body = "Timeout/TaskCanceled: " & ex.Message,
                        .ResponseHeaders = New Dictionary(Of String, String)()
                    }
                End If
            Catch ex As Exception
                If attempt <= retries Then
                    needRetry = True
                    delayMs = BackoffDelay(backoff, attempt)
                Else
                    outRes = New RestJsonResponse With {
                        .Success = False,
                        .StatusCode = 0,
                        .Body = "Exception: " & ex.Message,
                        .ResponseHeaders = New Dictionary(Of String, String)()
                    }
                End If
            Finally
                cts.Dispose()
            End Try

            If needRetry Then
                Await Task.Delay(delayMs).ConfigureAwait(False)
                Continue Do
            End If

            ' Si llegamos aquí y no hay retry, devolvemos el último resultado construido
            If outRes Is Nothing Then
                outRes = New RestJsonResponse With {
                    .Success = False,
                    .StatusCode = 0,
                    .Body = "Unknown error",
                    .ResponseHeaders = New Dictionary(Of String, String)()
                }
            End If
            Return outRes
        Loop
    End Function

    ' ====================== Helpers ======================

    Private Shared Function BuildBaseUri(baseUrl As String) As Uri
        If String.IsNullOrWhiteSpace(baseUrl) Then Throw New InvalidOperationException("RestJsonConfig.BaseUrl no está configurado.")
        Dim trimmed As String = baseUrl.Trim()
        If trimmed.EndsWith("/", StringComparison.Ordinal) Then
            trimmed = trimmed.Substring(0, trimmed.Length - 1)
        End If
        Return New Uri(trimmed, UriKind.Absolute)
    End Function

    Private Shared Function BuildRequestUri(endpoint As String) As Uri
        If Uri.IsWellFormedUriString(endpoint, UriKind.Absolute) Then
            Return New Uri(endpoint, UriKind.Absolute)
        End If
        Dim rel As String = endpoint.Trim()
        If Not rel.StartsWith("/", StringComparison.Ordinal) Then
            rel = "/" & rel
        End If
        Return New Uri(_http.BaseAddress, rel)
    End Function

    Private Shared Sub ValidateEndpoint(endpoint As String)
        If String.IsNullOrWhiteSpace(endpoint) Then Throw New ArgumentException("endpoint vacío")
        Dim e As String = endpoint.Trim()
        If e.Contains(" "c) Then Throw New ArgumentException("endpoint inválido: contiene espacios.")
        If Uri.IsWellFormedUriString(e, UriKind.Absolute) Then
            Dim u As New Uri(e)
            If u.Scheme <> Uri.UriSchemeHttp AndAlso u.Scheme <> Uri.UriSchemeHttps Then
                Throw New ArgumentException("endpoint inválido: esquema no http/https.")
            End If
        End If
    End Sub

    Private Shared Sub ValidateJson(jsonPayload As String)
        If String.IsNullOrWhiteSpace(jsonPayload) Then
            Throw New ArgumentException("JSON vacío.")
        End If
        Dim t As String = jsonPayload.Trim()
        Dim startsOk As Boolean = (t.StartsWith("{", StringComparison.Ordinal) OrElse t.StartsWith("[", StringComparison.Ordinal))
        Dim endsOk As Boolean = (t.EndsWith("}", StringComparison.Ordinal) OrElse t.EndsWith("]", StringComparison.Ordinal))
        If Not (startsOk AndAlso endsOk) Then
            Throw New ArgumentException("JSON con formato aparente inválido (no inicia/termina con llaves o corchetes).")
        End If
    End Sub

    Private Shared Function IsTransient(code As HttpStatusCode) As Boolean
        Dim n As Integer = CInt(code)
        If n = 408 OrElse n = 429 Then Return True
        If n >= 500 AndAlso n <= 599 Then Return True
        Return False
    End Function

    Private Shared Function BackoffDelay(baseMs As Integer, attempt As Integer) As Integer
        Dim ms As Integer = CInt(baseMs * Math.Pow(2, Math.Max(0, attempt - 1)))
        Return Math.Min(ms, 5000)
    End Function

    Private Shared Function FlattenHeaders(res As HttpResponseMessage) As Dictionary(Of String, String)
        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each h In res.Headers
            d(h.Key) = String.Join(",", h.Value)
        Next
        If res.Content IsNot Nothing Then
            For Each h In res.Content.Headers
                d(h.Key) = String.Join(",", h.Value)
            Next
        End If
        Return d
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        ' HttpClient compartido NO se dispone para reutilizar sockets
    End Sub

End Class
