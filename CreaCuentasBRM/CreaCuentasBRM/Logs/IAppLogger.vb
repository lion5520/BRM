Option Strict On
Option Explicit On

''' <summary>
''' Logger central con dos flujos lógicos:
''' 1) Errores/Excepciones
''' 2) Datos/JSON/Resultados (texto, objetos, etc.)
''' Salida: UI (TextBox) vía AppendText, thread-safe.
''' </summary>
Public Interface IAppLogger
    ' ---- Errores / Excepciones ----
    Sub LogError(ex As Exception, Optional context As Object = Nothing)
    Sub LogError(message As String, Optional ex As Exception = Nothing, Optional context As Object = Nothing)

    ' ---- Datos / JSON / Resultados ----
    Sub LogData(data As Object, Optional label As String = Nothing)
    Sub LogJson(json As String, Optional label As String = Nothing)
    Sub LogQueryResult(sql As String, result As Object)
End Interface
