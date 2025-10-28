Option Strict On
Option Explicit On

Imports System
Imports System.Linq

' Este parcial SOLO agrega los generadores de documento.
' No duplica otras funciones ni toca tu lógica existente.

Partial Public Class CreaCliente

    ' Random dedicado a documentos (evita choques con otros Random del proyecto)
    Private Shared ReadOnly _rngDoc As New Random(CInt((Environment.TickCount Xor Guid.NewGuid().GetHashCode()) And &H7FFFFFFF))

    Private Shared Function NextDigitDoc() As Integer
        SyncLock _rngDoc
            Return _rngDoc.Next(0, 10) ' 0..9
        End SyncLock
    End Function

    ' ===============================
    ' === GENERADOR DE CPF DEMO ====
    ' ===============================
    Public Shared Function GenerarCPFValidoDemo() As String
        Dim nums As Integer() = New Integer(8) {}
        For i As Integer = 0 To 8
            nums(i) = NextDigitDoc()
        Next

        ' d1
        Dim d1 As Integer = 0
        For i As Integer = 0 To 8
            d1 += nums(i) * (10 - i)
        Next
        d1 = 11 - (d1 Mod 11)
        If d1 >= 10 Then d1 = 0

        ' d2
        Dim d2 As Integer = 0
        For i As Integer = 0 To 8
            d2 += nums(i) * (11 - i)
        Next
        d2 += d1 * 2
        d2 = 11 - (d2 Mod 11)
        If d2 >= 10 Then d2 = 0

        Dim baseStr As String = String.Concat(nums.Select(Function(n) n.ToString()))
        Return baseStr & d1.ToString() & d2.ToString()
    End Function

    ' ================================
    ' === GENERADOR DE CNPJ DEMO ====
    ' ================================
    Public Shared Function GenerarCNPJValidoDemo() As String
        Dim nums As Integer() = New Integer(11) {}
        For i As Integer = 0 To 11
            nums(i) = NextDigitDoc()
        Next

        Dim pesos1 As Integer() = {5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2}
        Dim sum1 As Integer = 0
        For i As Integer = 0 To 11
            sum1 += nums(i) * pesos1(i)
        Next
        Dim d1 As Integer = sum1 Mod 11
        d1 = If(d1 < 2, 0, 11 - d1)

        Dim pesos2 As Integer() = {6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2}
        Dim sum2 As Integer = d1 * 2
        For i As Integer = 0 To 11
            sum2 += nums(i) * pesos2(i + 1)
        Next
        Dim d2 As Integer = sum2 Mod 11
        d2 = If(d2 < 2, 0, 11 - d2)

        Dim baseStr As String = String.Concat(nums.Select(Function(n) n.ToString()))
        Return baseStr & d1.ToString() & d2.ToString()
    End Function

End Class
