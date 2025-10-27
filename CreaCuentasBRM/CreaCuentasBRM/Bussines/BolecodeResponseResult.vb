Option Strict On
Option Explicit On

Public NotInheritable Class BolecodeResponseResult
    Public Property Success As Boolean
    Public Property HttpStatus As Integer
    Public Property RawBody As String

    ' Ecos del request
    Public Property AccountPoid As String
    Public Property Token As String
    Public Property ParId As String
End Class
