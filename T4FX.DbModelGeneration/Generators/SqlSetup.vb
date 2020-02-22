'Imports Definitions.DataModel
'Imports System
'Imports System.Linq
'Imports System.Collections
'Imports System.Collections.Generic
'Imports System.Text

'  Public Class SqlSetup
'    Inherits CommonTemplateBase

'    Public Sub New(generator As GeneratorContext)
'      MyBase.New(generator)
'    End Sub

'    Public Property InputFileName As String
'    Public Property NamespaceName As String
'    Public Property FileClassName As String
'    Public Property RootObject As String
'    Public Property TransformationHandle As Object

'    Protected Overrides Sub GenerateInternal(log As StringBuilder)

'      Dim model As DataModelDefinition
'      Dim entityContainer As Container
'      Dim inputFile As String

'      If (Me.InputFileName = String.Empty) Then
'        inputFile = Me.Generator.Host.RessolveRelativeFileName(Me.Generator.Host.TemplateFileNameWithoutExtension & ".xml")
'      Else
'        inputFile = Me.Generator.Host.RessolveRelativeFileName(Me.InputFileName.Trim())
'      End If

'      log.AppendLine("reading input file '" & inputFile & "'")

'      If (inputFile.ToLower.EndsWith(".edmx")) Then
'        model = DataModelDefinition.CreateNew()
'        entityContainer = model.Content.Containers.Item(0)
'        Dim edmxReader As New EdmxToDbModelTransformer(inputFile, entityContainer, Me.TransformationHandle)
'        edmxReader.Process()
'        edmxReader = Nothing
'      Else
'        model = DataModelDefinition.ReadFromFile(inputFile)
'        entityContainer = model.Content.Containers.Item(0)
'      End If

'      Me.Generator.Host.DeleteOldOutputs()

'      Dim associations As IEnumerable(Of Association) = model.Content.Associations

'      Dim filename As String = Me.Generator.Host.TemplateFileNameWithoutExtension & ".sql"
'      Me.OutputFileType(filename) = VisualStudioGenerationHostWrapper.ESolutionItemType.EmbeddedResource
'      Dim writer As New WriterBase(Sub(text) Me.OutputFileContent(filename).Append(text))

'      For Each entity As Entity In entityContainer.Entities

'        log.AppendLine("writing " & entity.Name)

'        writer.WriteLines(2)

'        If (Not entity.CodeSummary = String.Empty) Then
'          writer.WriteLineFormated("-- {0}", entity.CodeSummary.Trim().Replace(Environment.NewLine, Environment.NewLine & New String(" "c, 11) & "'''"))
'        End If

'        writer.WriteFormated("CREATE TABLE [dbo].[{0}] (", entity.Name)
'        Using writer.NewIndentScope
'          Dim isFirst As Boolean = True

'          For Each prop As ScalarProperty In entity.ScalarProperties
'            If (isFirst) Then
'              isFirst = False
'              writer.WriteLine()
'            Else
'              writer.WriteLine(",")
'            End If

'            If (Not prop.CodeSummary = String.Empty) Then
'              writer.WriteLineFormated("-- {0}", prop.CodeSummary.Trim().Replace(Environment.NewLine, Environment.NewLine & New String(" "c, 11) & "'''"))
'            End If

'            writer.WriteFormated("[{0}] [{1}] NOT NULL", prop.Name, Me.GetSqlTypeName(prop.TypeName))

'          Next
'        End Using
'        writer.WriteLine()
'        writer.WriteLine(") ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]")
'        writer.WriteLine("GO")

'      Next

'      Me.SaveOutputFiles()

'      log.AppendLine("done")

'    End Sub

'    Private Function GetSqlTypeName(typeName As String) As String
'      typeName = "." & typeName.ToLower()
'      Select Case True
'        Case typeName.EndsWith(".integer") : Return "int"
'        Case typeName.EndsWith(".datetime") : Return "datetime2"
'        Case typeName.EndsWith(".date") : Return "datetime2"
'        Case Else : Return "varchar(max)"
'      End Select

'    End Function

'  End Class
