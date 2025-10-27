Option Strict On
Option Explicit On

Public Class CrearClienteResult
    Public Property Success As Boolean
    Public Property AccountPoid As String
    Public Property Documento As String
    Public Property ProtocolId As String     ' <— NECESARIA
    Public Property HttpStatus As Integer
    Public Property RawBody As String
End Class


