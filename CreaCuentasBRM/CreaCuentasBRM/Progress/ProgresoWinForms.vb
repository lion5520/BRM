Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Windows.Forms

''' <summary>
''' Implementación de IProgreso para Windows Forms (multi-instancia y thread-safe).
''' Registra una o varias ProgressBar (y opcional Label) y actualiza todas con Reportar().
''' </summary>
Public Class ProgresoWinForms
    Implements IProgreso

    Private NotInheritable Class Target
        Public ReadOnly Barra As ProgressBar
        Public ReadOnly Etiqueta As Label

        Public Sub New(barra As ProgressBar, etiqueta As Label)
            Me.Barra = barra
            Me.Etiqueta = etiqueta
        End Sub
    End Class

    Private ReadOnly _targets As New List(Of Target)
    Private ReadOnly _syncControl As Control

    ''' <summary>
    ''' Crea un puente de progreso que invoca seguro al hilo de UI.
    ''' Pasa cualquier control del formulario principal (por ejemplo: Me).
    ''' </summary>
    ''' <param name="syncControl">Control perteneciente al hilo de UI.</param>
    Public Sub New(syncControl As Control)
        If syncControl Is Nothing Then Throw New ArgumentNullException(NameOf(syncControl))
        _syncControl = syncControl
    End Sub

    ''' <summary>
    ''' Registra una barra y opcionalmente una etiqueta para recibir actualizaciones.
    ''' Repite las veces necesarias para múltiples barras/etiquetas.
    ''' </summary>
    Public Sub Registrar(barra As ProgressBar, Optional etiqueta As Label = Nothing)
        If barra Is Nothing Then Throw New ArgumentNullException(NameOf(barra))
        SyncLock _targets
            _targets.Add(New Target(barra, etiqueta))
        End SyncLock
    End Sub

    Public Sub Reportar(porcentaje As Integer, mensaje As String) Implements IProgreso.Reportar
        If porcentaje < 1 Then porcentaje = 1
        If porcentaje > 100 Then porcentaje = 100

        Dim actualizar As MethodInvoker =
            Sub()
                Dim local As List(Of Target)
                SyncLock _targets
                    local = New List(Of Target)(_targets)
                End SyncLock

                For Each t In local
                    If t.Barra IsNot Nothing Then
                        If t.Barra.Minimum <> 0 Then t.Barra.Minimum = 0
                        If t.Barra.Maximum <> 100 Then t.Barra.Maximum = 100
                        ' Evita ArgumentOutOfRangeException si el control aún no está manejado
                        If Not t.Barra.IsHandleCreated Then
                            ' En raros casos de controles aún no creados, omite hasta que estén listos
                        Else
                            t.Barra.Value = porcentaje
                        End If
                    End If

                    If t.Etiqueta IsNot Nothing Then
                        If t.Etiqueta.IsHandleCreated Then
                            t.Etiqueta.Text = $"{porcentaje}% - {mensaje}"
                        End If
                    End If
                Next
            End Sub

        If _syncControl IsNot Nothing AndAlso _syncControl.IsHandleCreated AndAlso _syncControl.InvokeRequired Then
            _syncControl.BeginInvoke(actualizar)
        ElseIf _syncControl IsNot Nothing AndAlso _syncControl.IsHandleCreated Then
            actualizar()
        Else
            ' Si el handle aún no está creado (ej. durante Load antes de CreateHandle),
            ' ejecuta directamente: los controles pueden ignorar la escritura hasta estar listos.
            actualizar()
        End If
    End Sub
End Class
