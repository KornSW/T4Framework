
Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Xml
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextTemplating
Imports Microsoft.VisualStudio.TextTemplating.VSHost
Imports System.Data
Imports System.Data.SqlClient
Imports System.Xml.Linq
Imports System.CodeDom.Compiler

<Serializable()>
Public Class VisualStudioGenerationHostWrapper
  Implements IGenerationHost


#Region "..."

  Private _GenerationEnvironment As StringBuilder = Nothing
  Private _VsHost As ITextTemplatingEngineHost = Nothing

  Public Sub New(vsHost As ITextTemplatingEngineHost, generationEnvironment As StringBuilder)
    _VsHost = vsHost
    _GenerationEnvironment = generationEnvironment
  End Sub

  Public ReadOnly Property VsHost As ITextTemplatingEngineHost
    Get
      Return _VsHost
    End Get
  End Property

  Public ReadOnly Property ServiceProvider As IServiceProvider
    Get
      Return DirectCast(_VsHost, IServiceProvider)
    End Get
  End Property

  Public Function GetService(Of T)() As T
    Return DirectCast(Me.ServiceProvider.GetService(GetType(T)), T)
  End Function

  Private _DTE As EnvDTE.DTE = Nothing
  Public ReadOnly Property DTE As EnvDTE.DTE
    Get
      If (_DTE Is Nothing) Then
        _DTE = Me.GetService(Of EnvDTE.DTE)()
      End If
      Return _DTE
    End Get
  End Property

  Public ReadOnly Property GenerationEnvironment As StringBuilder
    Get
      Return _GenerationEnvironment
    End Get
  End Property

  Public Function GetTemplateProject() As EnvDTE.Project

    'Dim hostServiceProvider As IServiceProvider = DirectCast(_VsHost, IServiceProvider)
    'If (hostServiceProvider Is Nothing) Then
    '  Throw New Exception("Host property returned unexpected value (null)")
    'End If

    'Dim dte As EnvDTE.DTE = DirectCast(hostServiceProvider.GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
    'If (dte Is Nothing) Then
    '  Throw New Exception("Unable to retrieve EnvDTE.DTE")
    'End If

    Dim activeSolutionProjects As Array = DirectCast(DTE.ActiveSolutionProjects, Array)
    If (activeSolutionProjects Is Nothing) Then
      Throw New Exception("DTE.ActiveSolutionProjects returned null")
    End If

    Dim dteProject As EnvDTE.Project = DirectCast(activeSolutionProjects.GetValue(0), EnvDTE.Project)
    If (dteProject Is Nothing) Then
      Throw New Exception("DTE.ActiveSolutionProjects[0] returned null")
    End If

    Return dteProject
  End Function

  Public Function GetTemplateProjectItem() As EnvDTE.ProjectItem

    Dim dteProject As EnvDTE.Project = GetTemplateProject()
    Dim vsProject As IVsProject = DteProjectToVsProject(dteProject)

    Dim iFound As Integer = 0
    Dim itemId As UInteger = 0

    Dim pdwPriority As VSDOCUMENTPRIORITY() = New VSDOCUMENTPRIORITY(1) {}

    Dim result As Integer = vsProject.IsDocumentInProject(VsHost.TemplateFile, iFound, pdwPriority, itemId)
    If (Not result = VSConstants.S_OK) Then
      Throw New Exception("Unexpected error calling IVsProject.IsDocumentInProject")
    End If
    If (iFound = 0) Then
      Throw New Exception("Cannot retrieve ProjectItem for template file")
    End If
    If (itemId = 0) Then
      Throw New Exception("Cannot retrieve ProjectItem for template file")
    End If

    Dim itemContext As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = Nothing
    result = vsProject.GetItemContext(itemId, itemContext)
    If (Not result = VSConstants.S_OK) Then
      Throw New Exception("Unexpected error calling IVsProject.GetItemContext")
    End If
    If (itemContext Is Nothing) Then
      Throw New Exception("IVsProject.GetItemContext returned null")
    End If

    Dim itemContextService As ServiceProvider = New ServiceProvider(itemContext)
    Dim templateItem As EnvDTE.ProjectItem = DirectCast(itemContextService.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
    Debug.Assert(templateItem IsNot Nothing, "itemContextService.GetService returned null")

    Return templateItem
  End Function

  Friend Shared Function DteProjectToVsProject(project As EnvDTE.Project) As IVsProject

    If (project Is Nothing) Then
      Throw New ArgumentNullException("project")
    End If

    Dim projectGuid As String = Nothing

    ' DTE does not expose the project GUID that exists at in the msbuild project file.        
    ' Cannot use MSBuild object model because it uses a static instance of the Engine,         
    ' and using the Project will cause it to be unloaded from the engine when the         
    ' GC collects the variable that we declare.       
    Using projectReader = System.Xml.XmlReader.Create(project.FileName)
      projectReader.MoveToContent()
      Dim nodeName As Object = projectReader.NameTable.Add("ProjectGuid")
      Do While (projectReader.Read())
        If (Object.Equals(projectReader.LocalName, nodeName)) Then
          projectGuid = DirectCast(projectReader.ReadElementContentAsString(), String)
          Exit Do
        End If
      Loop
    End Using

    If (String.IsNullOrEmpty(projectGuid)) Then
      Throw New Exception("Unable to find ProjectGuid element in the project file")
    End If

    Dim dteServiceProvider As Microsoft.VisualStudio.OLE.Interop.IServiceProvider =
      DirectCast(project.DTE, Microsoft.VisualStudio.OLE.Interop.IServiceProvider)

    Dim serviceProvider As IServiceProvider = New ServiceProvider(dteServiceProvider)
    Dim vsHierarchy As IVsHierarchy = VsShellUtilities.GetHierarchy(serviceProvider, New Guid(projectGuid))

    Dim vsProject As IVsProject = DirectCast(vsHierarchy, IVsProject)
    If (vsProject Is Nothing) Then
      Throw New ArgumentException("Project is not a VS project.")
    End If

    Return vsProject
  End Function

#End Region

  Public Function RessolveRelativeFileName(relativeFileName As String) As String Implements IGenerationHost.RessolveRelativeFileName

    If (relativeFileName.StartsWith("\")) Then
      relativeFileName = "." & relativeFileName
    Else
      relativeFileName = ".\" & relativeFileName
    End If

    Dim fi As New FileInfo(IO.Path.Combine(IO.Path.GetDirectoryName(_VsHost.TemplateFile), relativeFileName))

    Return fi.FullName
  End Function

  Public ReadOnly Property TemplateFileNameWithoutExtension As String Implements IGenerationHost.TemplateFileNameWithoutExtension
    Get
      Return IO.Path.GetFileNameWithoutExtension(_VsHost.TemplateFile)
    End Get
  End Property

#Region " Solution Files "

  Dim _SavedOutputs As List(Of String) = New List(Of String)

  Public Sub DeleteOldOutputs() Implements IGenerationHost.DeleteOldOutputs

    Dim templateProjectItem As EnvDTE.ProjectItem = GetTemplateProjectItem()

    For Each childProjectItem As EnvDTE.ProjectItem In templateProjectItem.ProjectItems
      If (Not _SavedOutputs.Contains(childProjectItem.Name)) Then
        Threading.Thread.Sleep(100)
        childProjectItem.Delete()
      End If
    Next

  End Sub

  Dim _Engine As Engine = New Engine()

  Public Sub ProcessTemplate(templateFileName As String, outputFileName As String)

    Dim templateDirectory As String = Path.GetDirectoryName(VsHost.TemplateFile)
    Dim outputFilePath As String = Path.Combine(templateDirectory, outputFileName)

    Dim template As String = File.ReadAllText(VsHost.ResolvePath(templateFileName))
    Dim output As String = _Engine.ProcessTemplate(template, VsHost)
    File.WriteAllText(outputFilePath, output)

    Dim templateProjectItem As EnvDTE.ProjectItem = GetTemplateProjectItem()
    templateProjectItem.ProjectItems.AddFromFile(outputFilePath)

    _SavedOutputs.Add(outputFileName)
  End Sub

  Public Enum ESolutionItemType As Integer
    None = 0
    Compile = 1
    EmbeddedResource = 3
  End Enum

  Public Sub SaveOutput(outputFileName As String, Optional solutionItemType As ESolutionItemType = ESolutionItemType.Compile, Optional encoding As System.Text.Encoding = Nothing) Implements IGenerationHost.SaveOutput

    Dim templateDirectory As String = Path.GetDirectoryName(VsHost.TemplateFile)
    Dim outputFilePath As String = Path.Combine(templateDirectory, outputFileName)

    Dim templateProjectItem As EnvDTE.ProjectItem = GetTemplateProjectItem()

    'checkout file from source control
    If (templateProjectItem.DTE.SourceControl IsNot Nothing) Then

      templateProjectItem.DTE.SourceControl.CheckOutItem(outputFilePath)

      For Each existingFileItem As EnvDTE.ProjectItem In templateProjectItem.ProjectItems
        If (existingFileItem.Name = IO.Path.GetFileName(outputFilePath)) Then

          'we need to do this fist beacause there could be a old version which is under sourcecontrol and write-protected 
          existingFileItem.Remove()

          Threading.Thread.Sleep(100)
        End If
      Next

    End If

    'remove write protection and delete file (if already exists)
    Dim outputFileInfo As New System.IO.FileInfo(outputFilePath)
    If (outputFileInfo.Exists) Then

      outputFileInfo.Attributes = IO.FileAttributes.Normal
      Threading.Thread.Sleep(100)

      File.Delete(outputFilePath)
      Threading.Thread.Sleep(100)

    End If

    If (encoding Is Nothing) Then
      File.WriteAllText(outputFilePath, Me.GenerationEnvironment.ToString(), System.Text.Encoding.Default)
    Else
      File.WriteAllText(outputFilePath, Me.GenerationEnvironment.ToString(), encoding)
    End If

    Me.GenerationEnvironment.Clear()

    Dim addedFile As EnvDTE.ProjectItem = templateProjectItem.ProjectItems.AddFromFile(outputFilePath)

    'Extension=.sql
    'FileName=HNS.2.sql 
    'CustomToolOutput=
    'DateModified=14.10.2013 15:44:13
    'IsLink=False
    'BuildAction=2  '(0=None;1=Compile;2=Content;3=EmbeddedResource;4=CodeAnalysis;5=AppDefinition;6=Page;7=Resource;8=SplashScreen;9=DesignData)
    'SubType=
    'CopyToOutputDirectory=0
    'IsSharedDesignTimeBuildInput=False
    'ItemType=Content 
    'IsCustomToolOutput=False 
    'HTMLTitle=
    'CustomTool=
    'Filesize=559 
    'CustomToolNamespace=
    'Author=
    'FullPath=C:\...\HNS.2.sql
    'IsDependentFile=True 
    'IsDesignTimeBuildInput=False 
    'DateCreated=14.10.2013 15:35:41
    'LocalPath=C:\...\HNS.2.sql
    'ModifiedBy=

    For Each propertyOfAddedFile As EnvDTE.Property In addedFile.Properties
      Select Case propertyOfAddedFile.Name
        Case "BuildAction" : propertyOfAddedFile.Value = DirectCast(solutionItemType, Integer)
      End Select
    Next

    _SavedOutputs.Add(outputFileName)
  End Sub

#End Region

#Region " Reporting "

  Public Sub WriteToStatusBar(completed As Integer, total As Integer, Optional text As String = "")
    'If (total = 0) Then
    '  Me.DTE.StatusBar.Progress(False, "", 0, 0)

    'Else
    '  Me.DTE.StatusBar.Progress(True, text, completed, total)
    'End If
  End Sub

  Public Sub WriteToOutputWindow(text As String) Implements IGenerationHost.WriteToOutputWindow
    ''http://msdn.microsoft.com/en-us/library/bb187346(v=vs.80).aspx

    Dim ow As IVsOutputWindow = Me.GetService(Of IVsOutputWindow)()

    Dim pane As IVsOutputWindowPane = Nothing
    ow.GetPane(VSConstants.GUID_OutWindowGeneralPane, pane)

    If (pane IsNot Nothing) Then
      pane.OutputString(text)
    End If

  End Sub

  Public Sub LogToErrorList(message As String, Optional fileName As String = "", Optional row As Integer = 1, Optional column As Integer = 1) Implements IGenerationHost.LogToErrorList

    If (fileName = "" AndAlso _VsHost IsNot Nothing) Then
      fileName = _VsHost.TemplateFile
    End If

    Me.VsHost.LogErrors(New CompilerErrorCollection({New CompilerError(fileName, row, column, "1", message)}))
  End Sub

#End Region


End Class
