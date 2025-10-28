Option Strict On
Option Explicit On

''' <summary>
''' Configuración central para conexión a Oracle BRM.
''' Modifica aquí los valores y todas las llamadas usarán esta configuración.
''' </summary>
Public NotInheritable Class BrmOracleConfig

    Public Shared ReadOnly Property Host As String = "pindev-d1.interno"
    Public Shared ReadOnly Property Port As Integer = 1549
    Public Shared ReadOnly Property Service As String = "PINDEV"
    Public Shared ReadOnly Property User As String = "pin"
    Public Shared ReadOnly Property Password As String = "F0i1B2R3a5##"

    ''' <summary>
    ''' Devuelve la cadena completa de conexión Oracle lista para usar.
    ''' </summary>
    Public Shared Function GetConnectionString() As String
        Return $"User Id={User};Password={Password};Data Source={Host}:{Port}/{Service};Pooling=true;Min Pool Size=1;Max Pool Size=50;"
    End Function

End Class
