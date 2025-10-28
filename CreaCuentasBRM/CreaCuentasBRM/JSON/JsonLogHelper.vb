Option Strict On
Option Explicit On

Imports System
Imports System.Text
Imports System.Globalization
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public NotInheritable Class JsonLogHelper

    ''' <summary>
    ''' Minifica un JSON sin romper contenido entre comillas (quita espacios y saltos fuera de strings).
    ''' </summary>
    Public Shared Function Pretty(json As String) As String
        If String.IsNullOrWhiteSpace(json) Then Return String.Empty
        Try
            Dim token As JToken = JToken.Parse(json)
            Return token.ToString(Formatting.Indented)
        Catch
            Return json
        End Try
    End Function


    ''' <summary>
    ''' Envía el JSON minificado a ambos canales (debug/data) si están disponibles.
    ''' </summary>
    Public Shared Sub LogRequestJson(logger As Object, json As String, Optional etiqueta As String = "REQUEST_JSON")
        Dim clean As String = Pretty(json)
        Try
            Dim t = logger?.GetType()
            If t IsNot Nothing Then
                Dim miJ = t.GetMethod("LogJson")
                If miJ IsNot Nothing Then miJ.Invoke(logger, New Object() {clean, etiqueta})
            End If
        Catch
            ' ignorar
        End Try
    End Sub

End Class
