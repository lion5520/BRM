Option Strict On
Option Explicit On

Imports System
Imports System.IO

''' <summary>
''' Config global del logger. Edita rutas y tamaños aquí una sola vez.
''' </summary>
Public NotInheritable Class AppLoggerConfig
    ' Carpeta base (por defecto, carpeta "Logs" junto al ejecutable)
    Public Shared Property BaseDirectory As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")

    ' Nombres de archivo
    Public Shared Property ErrorLogFileName As String = "errors.log"
    Public Shared Property DataLogFileName As String = "data.log"

    ' Tamaño máximo antes de rotar (bytes). Ej: 5 MB
    Public Shared Property MaxFileSizeBytes As Long = 5L * 1024L * 1024L

    ' Cantidad de respaldos a mantener (rotación simple: .1, .2, ...)
    Public Shared Property MaxBackups As Integer = 2

    ' Formato de timestamp
    Public Shared Property TimestampFormat As String = "yyyy-MM-dd HH:mm:ss.fff"

    ' Asegura carpeta creada
    Public Shared Sub EnsureDirectories()
        If Not Directory.Exists(BaseDirectory) Then
            Directory.CreateDirectory(BaseDirectory)
        End If
    End Sub

    ' Rutas completas
    Public Shared ReadOnly Property ErrorLogPath As String
        Get
            Return Path.Combine(BaseDirectory, ErrorLogFileName)
        End Get
    End Property

    Public Shared ReadOnly Property DataLogPath As String
        Get
            Return Path.Combine(BaseDirectory, DataLogFileName)
        End Get
    End Property
End Class
