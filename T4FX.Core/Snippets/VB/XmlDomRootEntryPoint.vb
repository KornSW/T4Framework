Imports System.Runtime.CompilerServices
Imports System.ComponentModel
Imports System.Collections.Generic
Imports System.Collections
Imports System.Diagnostics
Imports System.Xml.Linq
Imports System.Xml
Imports System.Linq
Imports System.Text
Imports System.IO
Imports System

Namespace __NamespaceName__

  Partial Public NotInheritable Class __FileClassName__

#Region " Constructors & Properties "

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Private _RawDocument As XDocument

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Private _Content As __RootObjectClassName__

    Public Shared Function ReadFromFile(fileName As String) As __FileClassName__
      Return New __FileClassName__(fileName)
    End Function

    Public Shared Function CreateNew() As __FileClassName__
      Return New __FileClassName__()
    End Function

    Private Sub New(fileName As String)
      Me.Customize()
      Me.ReadFile(fileName)
    End Sub

    Private Sub New()
      Me.Customize()
      _RawDocument = New XDocument()
      _Content = __RootObjectClassName__.CreateAndAddToDocumentRoot(Me, _RawDocument)
      Me.InitDefaultStructure()
    End Sub

    Public ReadOnly Property Content As __RootObjectClassName__
      Get
        Return _Content
      End Get
    End Property

#End Region

#Region " Serialize & Deserialize "

    Private Const XmlFileHeader As String = "<?xml version=""1.0"" encoding=""utf-8""?>"

    Private Property Encoding As System.Text.Encoding = System.Text.Encoding.Default

    Partial Private Sub Customize()
    End Sub

    Public Sub ReadFile(fileName As String)
      Me.UpdateRawContent(File.ReadAllText(fileName, Me.Encoding))
    End Sub

    Public Sub SaveFile(fileName As String)
      File.WriteAllText(fileName, XmlFileHeader & Environment.NewLine & Me.GetRawContent(), Me.Encoding)
    End Sub

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Function GetRawContent() As String
      Dim rawContentString As String
      rawContentString = _RawDocument.ToString()
      Me.SpecializeRawContentString(rawContentString)
      Return rawContentString
    End Function

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Sub UpdateRawContent(rawContentString As String)
      Me.NormalizeRawContentString(rawContentString)
      _RawDocument = XDocument.Parse(rawContentString)
      _Content = __RootObjectClassName__.FromDocumentRoot(Me, _RawDocument)
    End Sub

#End Region

#Region " Customizing (Partial Methods) "

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Partial Private Sub InitDefaultStructure()
    End Sub

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Partial Private Sub SpecializeRawContentString(ByRef rawContentString As String)
    End Sub

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Partial Private Sub NormalizeRawContentString(ByRef rawContentString As String)
    End Sub

#End Region

  End Class

End Namespace
