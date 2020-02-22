Imports System
Imports System.IO

Public Class WriterBase

  Public Property CustomLineEnd As String = Environment.NewLine
  Private _Target As Action(Of String)

  Public Sub New(target As Action(Of String))
    _Target = target
  End Sub

#Region " Indent "

  Public Property IndentSpace As String = "  "

  Private _CurrentIndentCount As Integer = 0
  Private _CurrentIndentString As String = String.Empty

  Public ReadOnly Property CurrentIndentCount As Integer
    Get
      Return _CurrentIndentCount
    End Get
  End Property

  Public ReadOnly Property CurrentIndentString As String
    Get
      Return _CurrentIndentString
    End Get
  End Property

  Public Sub PushIndent()
    _CurrentIndentCount = _CurrentIndentCount + 1
    _CurrentIndentString = String.Empty
    For i As Integer = 1 To _CurrentIndentCount
      _CurrentIndentString = _CurrentIndentString & IndentSpace
    Next
  End Sub

  Public Sub PopIndent()
    _CurrentIndentCount = _CurrentIndentCount - 1
    If (_CurrentIndentCount < 0) Then
      _CurrentIndentCount = 0
    End If
    _CurrentIndentString = String.Empty
    For i As Integer = 1 To _CurrentIndentCount
      _CurrentIndentString = _CurrentIndentString & IndentSpace
    Next
  End Sub

  Public Function NewIndentScope() As IndentScope
    Return New IndentScope(Me)
  End Function

  Public Class IndentScope
    Inherits LiftimeScopeObject

    Private _Writer As WriterBase

    Public Sub New(writer As WriterBase)
      _Writer = writer
      _Writer.PushIndent()
    End Sub

    Protected Overrides Sub LeaveScope()
      _Writer.PopIndent()
    End Sub

  End Class

#End Region

#Region " Plane Writing "

  Private _NewLine As Boolean = True
  Private _SuppressIndentOnce As Boolean = False

  Public Sub WriteSingle(str As String)
    If (_NewLine) Then
      If (_SuppressIndentOnce) Then
        _SuppressIndentOnce = False
      Else
        _Target.Invoke(Me.CurrentIndentString)
      End If
      _NewLine = False
    End If
    _Target.Invoke(str)
  End Sub

  Public Sub SuppressIndentOnce()
    _SuppressIndentOnce = True
  End Sub

  Public Sub WriteLineBreak()
    _Target.Invoke(Me.CustomLineEnd)
    _NewLine = True
  End Sub

  Public Sub Write(obj As Object)
    'lineenden splitten
    Dim first As Boolean = True
    Dim lr As New StringReader(obj.ToString())
    Dim line As String = lr.ReadLine()
    While (line IsNot Nothing)
      If (first) Then
        first = False
      Else
        WriteLineBreak()
      End If
      WriteSingle(line)
      line = lr.ReadLine()
    End While
  End Sub

  Public Sub WriteLines(count As Integer)
    For i As Integer = 1 To count
      WriteLineBreak()
    Next
  End Sub

  Public Sub WriteLine()
    WriteLineBreak()
  End Sub

  Public Sub WriteLine(obj As Object)
    Me.Write(obj)
    WriteLineBreak()
  End Sub

  Public Sub WriteFormated(obj As Object, ParamArray placeHolders() As Object)
    Me.Write(String.Format(obj.ToString(), placeHolders))
  End Sub

  Public Sub WriteLineFormated(obj As Object, ParamArray placeHolders() As Object)
    Me.Write(String.Format(obj.ToString(), placeHolders))
    WriteLineBreak()
  End Sub

#End Region

End Class

#Region " Heplers "

Public MustInherit Class LiftimeScopeObject
  Implements IDisposable

  Public Sub New()
  End Sub

  Protected MustOverride Sub LeaveScope()

  Private _Disposed As Boolean ' To detect redundant calls

  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not Me._Disposed) Then
      If (disposing) Then
        Try
          Me.LeaveScope()
        Catch
        End Try
      End If
    End If
    Me._Disposed = True
  End Sub

  Public Sub Dispose() Implements IDisposable.Dispose
    Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

End Class

#End Region
