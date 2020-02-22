Imports System
Imports System.IO
Imports System.Text
Imports System.Reflection

#Region " MY Extension "

Namespace My

  <Microsoft.VisualBasic.HideModuleName>
  Friend Module MyVbSnippets

    Private _InstanceVbSnippets As VbSnippets = Nothing
    Friend ReadOnly Property VbSnippets As VbSnippets
      Get
        If (_InstanceVbSnippets Is Nothing) Then
          _InstanceVbSnippets = New VbSnippets
        End If
        Return _InstanceVbSnippets
      End Get
    End Property

  End Module

End Namespace

#End Region

Partial Public NotInheritable Class VbSnippets
#Region "..."

  Private Function LoadEmbeddedFile(fileName As String) As StringBuilder
    Dim content As New StringBuilder
    Dim streamReader As StreamReader

    streamReader = New StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("DevTools.T4GenLib." & fileName))

    While Not streamReader.EndOfStream
      content.AppendLine(streamReader.ReadLine())
    End While

    Return content
  End Function

#End Region

  'generated functions for each embedded snippet file:

#Region " CopyrightHeader.vb "

    Public Function [CopyrightHeader](generatorName As String, generatorAuthor As String, genLibTitle As String) as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("CopyrightHeader.vb")

        content.Replace("__GeneratorName__", generatorName)
        content.Replace("__GeneratorAuthor__", generatorAuthor)
        content.Replace("__GenLibTitle__", genLibTitle)

        Return content.ToString()
    End Function

#End Region

#Region " DefaultImports.vb "

    Public Function [DefaultImports]() as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("DefaultImports.vb")

        Return content.ToString()
    End Function

#End Region

#Region " ReadonlyProperty.vb "

    Public Function [ReadonlyProperty](modifier As String, name As String, type As String, returnValue As String) as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("ReadonlyProperty.vb")

        content.Replace("__Modifier__", modifier)
        content.Replace("__Name__", name)
        content.Replace("__Type__", type)
        content.Replace("__ReturnValue__", returnValue)

        Return content.ToString()
    End Function

#End Region

#Region " XmlDom._BACKUP.vb "

    Public Function [XmlDom__BACKUP]() as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("XmlDom._BACKUP.vb")

        Return content.ToString()
    End Function

#End Region

#Region " XmlDomBase.vb "

    Public Function [XmlDomBase](namespaceName As String) as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("XmlDomBase.vb")

        content.Replace("__NamespaceName__", namespaceName)

        Return content.ToString()
    End Function

#End Region

#Region " XmlDomEntityExtension.vb "

    Public Function [XmlDomEntityExtension](entityName As String, entityNameFirstLCase As String, commaSeparatedKeyPropertyParams As String, keyPropertyExpression As String, commaSeparatedKeyPropertyNames As String) as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("XmlDomEntityExtension.vb")

        content.Replace("__EntityName__", entityName)
        content.Replace("__EntityNameFirstLCase__", entityNameFirstLCase)
        content.Replace("__CommaSeparatedKeyPropertyParams__", commaSeparatedKeyPropertyParams)
        content.Replace("__KeyPropertyExpression__", keyPropertyExpression)
        content.Replace("__CommaSeparatedKeyPropertyNames__", commaSeparatedKeyPropertyNames)

        Return content.ToString()
    End Function

#End Region

#Region " XmlDomRootEntryPoint.vb "

    Public Function [XmlDomRootEntryPoint](namespaceName As String, fileClassName As String, rootObjectClassName As String) as String
        Dim content As StringBuilder
        content = Me.LoadEmbeddedFile("XmlDomRootEntryPoint.vb")

        content.Replace("__NamespaceName__", namespaceName)
        content.Replace("__FileClassName__", fileClassName)
        content.Replace("__RootObjectClassName__", rootObjectClassName)

        Return content.ToString()
    End Function

#End Region

End Class
