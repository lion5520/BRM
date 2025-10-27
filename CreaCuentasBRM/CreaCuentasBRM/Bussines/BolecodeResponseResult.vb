Option Strict On
Option Explicit On

Public NotInheritable Class BolecodeResponseResult
    Public Property Success As Boolean
    Public Property AccountPoid As String
    Public Property Token As String
    Public Property ParId As String
    Public Property HttpStatus As Integer?
    Public Property RawBody As String
End Class
