Option Strict On
Option Explicit On

''' <summary>
''' Config central para llamadas JSON (REST). Edita BaseUrl y ApiKey si aplica.
''' </summary>
Public NotInheritable Class RestJsonConfig
    ' URL base SIN "/" final. Ej: "http://brmdev.hml.ocpcorp.oi.intranet"
    Public Shared Property BaseUrl As String = "http://TU-HOST"

    ' Si tu API usa Bearer/API-Key (opcional)
    Public Shared Property ApiKey As String = ""

    ' Timeout por llamada (segundos)
    Public Shared Property DefaultTimeoutSeconds As Integer = 30

    ' Reintentos para errores transitorios
    Public Shared Property DefaultMaxRetries As Integer = 2

    ' Milisegundos base para backoff exponencial
    Public Shared Property DefaultBackoffMs As Integer = 250
End Class
