Option Strict On
Option Explicit On

Public Class CompraProductosResult
    Public Property Success As Boolean
    Public Property HttpStatus As Integer
    Public Property RawBody As String

    Public Property AccountPoid As String
    Public Property ProtocolId As String
    Public Property ContractId As String
    Public Property Terminal As String
End Class
