Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Xml
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextTemplating
Imports Microsoft.VisualStudio.TextTemplating.VSHost
Imports System.Data
Imports System.Data.SqlClient
Imports System.Xml.Linq

<Serializable()>
Public Class GeneratorContext
  Implements IDisposable

#Region " Constructor "

  Public Sub New(visualStudioHost As ITextTemplatingEngineHost, generationEnvironment As StringBuilder)
    Me.New(New VisualStudioGenerationHostWrapper(visualStudioHost, generationEnvironment), generationEnvironment)
  End Sub

  Public Sub New(outputFolder As String, generationEnvironment As StringBuilder)
    Me.New(New MockGenerationHost(), generationEnvironment)
  End Sub

  Public Sub New(host As IGenerationHost, generationEnvironment As StringBuilder)
    _Host = host
    _GenerationEnvironment = generationEnvironment
  End Sub

#End Region

#Region " Properties "

  Private _Host As IGenerationHost = Nothing
  Public ReadOnly Property Host As IGenerationHost
    Get
      Return _Host
    End Get
  End Property

  Private _GenerationEnvironment As StringBuilder = Nothing
  Public ReadOnly Property GenerationEnvironment As StringBuilder
    Get
      Return _GenerationEnvironment
    End Get
  End Property

#End Region

#Region " IDisposable "
  Private disposedValue As Boolean ' To detect redundant calls

  ' IDisposable
  Protected Overridable Sub Dispose(disposing As Boolean)
    If Not Me.disposedValue Then
      If disposing Then
        ' TODO: dispose managed state (managed objects).
      End If

      ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
      ' TODO: set large fields to null.
    End If
    Me.disposedValue = True
  End Sub

  ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
  'Protected Overrides Sub Finalize()
  '    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
  '    Dispose(False)
  '    MyBase.Finalize()
  'End Sub

  ' This code added by Visual Basic to correctly implement the disposable pattern.
  Public Sub Dispose() Implements IDisposable.Dispose
    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub
#End Region

End Class
