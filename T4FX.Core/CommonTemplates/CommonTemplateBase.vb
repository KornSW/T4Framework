Imports System
Imports System.CodeDom.Compiler
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Xml
Imports System.Xml.Linq
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextTemplating
Imports Microsoft.VisualStudio.TextTemplating.VSHost

Public MustInherit Class CommonTemplateBase
  Implements IDisposable

  Dim _Generator As GeneratorContext

  Public Sub New(generator As GeneratorContext)
    _Generator = generator
  End Sub

  Protected ReadOnly Property Generator As GeneratorContext
    Get
      Return _Generator
    End Get
  End Property

  Property _OutputFileContents As New Dictionary(Of String, StringBuilder)
  Protected ReadOnly Property OutputFileContent(fileName As String) As StringBuilder
    Get
      If (Not _OutputFileContents.ContainsKey(fileName)) Then
        _OutputFileContents.Add(fileName, New StringBuilder)
      End If
      Return _OutputFileContents(fileName)
    End Get
  End Property
  Property _OutputFileTypes As New Dictionary(Of String, VisualStudioGenerationHostWrapper.ESolutionItemType)
  Protected Property OutputFileType(fileName As String) As VisualStudioGenerationHostWrapper.ESolutionItemType
    Get
      If (_OutputFileTypes.ContainsKey(fileName)) Then
        Return _OutputFileTypes(fileName)
      Else
        Return VisualStudioGenerationHostWrapper.ESolutionItemType.Compile
      End If
    End Get
    Set(value As VisualStudioGenerationHostWrapper.ESolutionItemType)
      If (_OutputFileTypes.ContainsKey(fileName)) Then
        _OutputFileTypes(fileName) = value
      Else
        _OutputFileTypes.Add(fileName, value)
      End If
    End Set
  End Property

  Property _OutputFileEncodings As New Dictionary(Of String, Encoding)
  Protected Property OutputFileEncoding(fileName As String) As Encoding
    Get
      If (_OutputFileEncodings.ContainsKey(fileName)) Then
        Return _OutputFileEncodings(fileName)
      Else
        Return Nothing
      End If
    End Get
    Set(value As Encoding)
      If (_OutputFileEncodings.ContainsKey(fileName)) Then
        _OutputFileEncodings(fileName) = value
      Else
        _OutputFileEncodings.Add(fileName, value)
      End If
    End Set
  End Property

  Protected Sub SaveOutputFiles()
    For Each fileName As String In _OutputFileContents.Keys
      Dim encoding As Encoding
      Dim type As VisualStudioGenerationHostWrapper.ESolutionItemType
      Me.Generator.GenerationEnvironment.Append(_OutputFileContents(fileName).ToString())

      encoding = Me.OutputFileEncoding(fileName)
      type = Me.OutputFileType(fileName)

      Me.Generator.Host.SaveOutput(fileName, type, encoding)
    Next
  End Sub

  Protected ReadOnly Property LibraryInfoString As String
    Get
      Return String.Format("T4GenLib {0}", Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString())
    End Get
  End Property

  Public Function Generate() As String
    Dim log As New StringBuilder
    Try
      log.AppendLine(DateTime.Now.ToString())

      Me.GenerateInternal(log)

    Catch ex As Exception
      Dim msg As New StringBuilder

      msg.AppendLine("Exception:")
      msg.AppendLine("Error in T4GenLib!")

      Do Until (ex Is Nothing)
        msg.AppendLine()
        msg.AppendLine("inner Exception:")
        msg.AppendLine("""" & ex.Message & """")
        msg.AppendLine("Stacktrace:")
        msg.AppendLine(ex.StackTrace)
        ex = ex.InnerException
      Loop
      log.AppendLine()
      log.AppendLine(msg.ToString())

      Me.Generator.Host.LogToErrorList(msg.ToString())

    End Try

    Return log.ToString()
  End Function

  Public ReadOnly Property TTFileTitle As String
    Get
      Return Generator.Host.TemplateFileNameWithoutExtension
    End Get
  End Property

  Protected MustOverride Sub GenerateInternal(log As StringBuilder)

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
