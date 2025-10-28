Option Strict On
Option Explicit On

' Este parcial solo agrega el método LogRequest usado por CrearAsync.
' Usa JsonLogHelper para escribir el JSON minificado en tus logs de UI.

Partial Public Class CreaCliente

    ' Llama a este método justo ANTES del POST en CrearAsync.
    Private Sub LogRequest(ByVal json As String)
        Try
            ' Escribe el JSON limpio en el canal de datos y/o debug del logger UI
            JsonLogHelper.LogRequestJson(_logger, json, "REQUEST_JSON_CREAR")
        Catch
            ' Nunca rompas el flujo por fallas de logging
        End Try
    End Sub

End Class
