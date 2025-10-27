Option Strict On
Option Explicit On

Imports System
Imports System.Text
Imports System.Globalization

Public NotInheritable Class JsonLogHelper

    ''' <summary>
    ''' Minifica un JSON sin romper contenido entre comillas (quita espacios y saltos fuera de strings).
    ''' </summary>
    Public Shared Function Minify(json As String) As String
        If String.IsNullOrEmpty(json) Then Return String.Empty

        Dim sb As New StringBuilder(json.Length)
        Dim inStr As Boolean = False
        Dim escape As Boolean = False

        For Each c As Char In json
            If inStr Then
                sb.Append(c)
                If escape Then
                    escape = False
                Else
                    If c = "\"c Then
                        escape = True
                    ElseIf c = """"c Then
                        inStr = False
                    End If
                End If
            Else
                Select Case c
                    Case """"c
                        inStr = True
                        sb.Append(c)

                ' ✅ Estos tres valores son *caracteres* válidos, no strings.
                    Case " "c, ChrW(9), ChrW(10), ChrW(13)
                        ' omitir espacios, tabulaciones, saltos de línea y retorno de carro

                    Case Else
                        sb.Append(c)
                End Select
            End If
        Next

        Return sb.ToString()
    End Function


    ''' <summary>
    ''' Envía el JSON minificado a ambos canales (debug/data) si están disponibles.
    ''' </summary>
    Public Shared Sub LogRequestJson(logger As Object, json As String, Optional etiqueta As String = "REQUEST_JSON")
        Dim clean As String = Minify(json)
        Try
            Dim t = logger?.GetType()
            If t IsNot Nothing Then
                Dim miJ = t.GetMethod("LogJson")
                If miJ IsNot Nothing Then miJ.Invoke(logger, New Object() {clean, etiqueta})
                Dim miD = t.GetMethod("LogData")
                If miD IsNot Nothing Then miD.Invoke(logger, New Object() {clean, etiqueta})
            End If
        Catch
            ' ignorar
        End Try
    End Sub

End Class
