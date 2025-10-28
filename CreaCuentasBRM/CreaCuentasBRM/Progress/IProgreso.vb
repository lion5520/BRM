' IProgreso.vb
Public Interface IProgreso
    ''' <summary>
    ''' Reporta avance (1-100) y mensaje asociado.
    ''' </summary>
    ''' <param name="porcentaje">Entero 1 a 100</param>
    ''' <param name="mensaje">Texto descriptivo</param>
    Sub Reportar(porcentaje As Integer, mensaje As String)
End Interface

