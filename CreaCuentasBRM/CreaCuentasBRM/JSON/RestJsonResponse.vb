Option Strict On
Option Explicit On

Imports System.Collections.Generic

''' <summary>
''' Resultado estandarizado de la llamada REST.
''' </summary>
Public NotInheritable Class RestJsonResponse
    Public Property Success As Boolean
    Public Property StatusCode As Integer
    Public Property Body As String
    Public Property ResponseHeaders As Dictionary(Of String, String)

    Public Overrides Function ToString() As String
        Return $"Success={Success}, Status={StatusCode}, BodyLen={If(Body, "").Length}"
    End Function
End Class
