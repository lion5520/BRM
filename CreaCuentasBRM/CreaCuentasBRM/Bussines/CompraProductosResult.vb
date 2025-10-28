Option Strict On
Option Explicit On

Public NotInheritable Class CompraProductosResult
    Public Property Success As Boolean
    Public Property AccountPoid As String
    Public Property ProtocolId As String
    Public Property ContractId As String
    Public Property Terminal As String
    Public Property HttpStatus As Integer?
    Public Property RawBody As String
End Class
