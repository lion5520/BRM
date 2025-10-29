Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Globalization
Imports Oracle.ManagedDataAccess.Client

Public NotInheritable Class BrmOracleQuery

    Private ReadOnly _connString As String

    Public Sub New()
        ' Usa la cadena que ya tenías en tu clase anterior o la que inyectas en runtime
        _connString = "User Id=pin;Password=F0i1B2R3a5##;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=pindev-d1.interno)(PORT=1549))(CONNECT_DATA=(SERVICE_NAME=PINDEV)))"
    End Sub

    Public Sub New(connectionString As String)
        _connString = If(connectionString, String.Empty)
    End Sub

    ' ------------------- API PÚBLICA (SOLO LECTURA) -------------------

    Public Function ExecuteDataTable(sql As String,
                                     Optional parameters As Dictionary(Of String, Object) = Nothing,
                                     Optional timeoutSeconds As Integer = 60) As DataTable
        Dim dt As New DataTable()
        Dim fixedSql As String = PreprocessSql(sql)

        Using cn As New OracleConnection(_connString)
            cn.Open()
            Using cmd As New OracleCommand(fixedSql, cn)
                cmd.BindByName = True
                cmd.CommandTimeout = timeoutSeconds
                AddParams(cmd, parameters)

                Using adp As New OracleDataAdapter(cmd)
                    adp.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    Public Function ExecuteScalar(Of T)(sql As String,
                                        Optional parameters As Dictionary(Of String, Object) = Nothing,
                                        Optional timeoutSeconds As Integer = 60) As T
        Dim fixedSql As String = PreprocessSql(sql)

        Using cn As New OracleConnection(_connString)
            cn.Open()
            Using cmd As New OracleCommand(fixedSql, cn)
                cmd.BindByName = True
                cmd.CommandTimeout = timeoutSeconds
                AddParams(cmd, parameters)
                Dim o = cmd.ExecuteScalar()
                If o Is Nothing OrElse o Is DBNull.Value Then
                    Return Nothing
                End If
                Return CType(Convert.ChangeType(o, GetType(T), CultureInfo.InvariantCulture), T)
            End Using
        End Using
    End Function

    Public Function ExecuteReader(sql As String,
                                  Optional parameters As Dictionary(Of String, Object) = Nothing,
                                  Optional timeoutSeconds As Integer = 60) As OracleDataReader
        Dim fixedSql As String = PreprocessSql(sql)

        Dim cn As New OracleConnection(_connString)
        cn.Open()
        Dim cmd As New OracleCommand(fixedSql, cn)
        cmd.BindByName = True
        cmd.CommandTimeout = timeoutSeconds
        AddParams(cmd, parameters)
        ' El lector cerrará la conexión cuando se deseche.
        Return cmd.ExecuteReader(CommandBehavior.CloseConnection)
    End Function

    Public Function ExecuteNonQuery(sql As String,
                                Optional parameters As Dictionary(Of String, Object) = Nothing,
                                Optional timeoutSeconds As Integer = 60,
                                Optional autoCommit As Boolean = True) As Integer
        Dim fixedSql As String = PreprocessSql(sql)

        Using cn As New OracleConnection(_connString)
            cn.Open()

            Dim tx As OracleTransaction = Nothing
            If autoCommit Then
                tx = cn.BeginTransaction(IsolationLevel.ReadCommitted)
            End If

            Try
                Using cmd As New OracleCommand(fixedSql, cn)
                    cmd.BindByName = True
                    cmd.CommandTimeout = timeoutSeconds
                    If tx IsNot Nothing Then cmd.Transaction = tx
                    AddParams(cmd, parameters)

                    Dim affected As Integer = cmd.ExecuteNonQuery()

                    If tx IsNot Nothing Then tx.Commit()
                    Return affected
                End Using

            Catch
                If tx IsNot Nothing Then
                    Try : tx.Rollback() : Catch : End Try
                End If
                Throw

            Finally
                If tx IsNot Nothing Then tx.Dispose()
            End Try
        End Using
    End Function


    ' ------------------- HELPERS -------------------

    Private Shared Sub AddParams(cmd As OracleCommand, parameters As Dictionary(Of String, Object))
        If parameters Is Nothing OrElse parameters.Count = 0 Then Return
        For Each kv In parameters
            Dim p As New OracleParameter With {
                .ParameterName = If(kv.Key.StartsWith(":", StringComparison.Ordinal), kv.Key, ":" & kv.Key),
                .Value = If(kv.Value, DBNull.Value)
            }
            cmd.Parameters.Add(p)
        Next
    End Sub

    ''' <summary>
    ''' Corrige SQLs heredados comunes que causan ORA-00904 por nombres de columnas equivocados.
    ''' También permite insertar pistas mínimas sin tocar el resto del código.
    ''' </summary>
    Private Shared Function PreprocessSql(sql As String) As String
        If String.IsNullOrWhiteSpace(sql) Then Return String.Empty

        Dim s As String = sql

        ' --- FIX 1: "PA.POID_ID0" no existe en AC_PROFILE_ACCOUNT_T. Debe ser "PA.OBJ_ID0".
        ' Visto en el log del usuario: ORA-00904 "PA"."POID_ID0": identificador no válido
        s = s.Replace("PA.POID_ID0", "PA.OBJ_ID0")

        ' --- FIX 2 (defensivo): espacios / CRLF inconsistentes
        s = s.Replace(vbCr, " ").Replace(vbLf, " ")

        Return s
    End Function

End Class
