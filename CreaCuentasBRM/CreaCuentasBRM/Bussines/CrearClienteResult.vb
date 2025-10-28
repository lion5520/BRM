Option Strict On
Option Explicit On

Public Class CrearClienteResult
    Public Property Success As Boolean
    Public Property AccountPoid As String            ' "0.0.0.1 /account <id> 0"
    Public Property Documento As String              ' CPF/CNPJ
    Public Property ProtocolId As String             ' ORACLE_SAP_TEST_####
    Public Property HttpStatus As Integer?
    Public Property RawBody As String
End Class


