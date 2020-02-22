Imports System
Imports KSW.T4FX.VisualStudioGenerationHostWrapper

Public Interface IGenerationHost

  'hier auf die dauer alles abstrahieren um später zu unittests ein eigenes generationenvironment zu mocken

  Sub LogToErrorList(message As String, Optional fileName As String = "", Optional row As Integer = 1, Optional column As Integer = 1)

  Sub DeleteOldOutputs()

  Sub SaveOutput(outputFileName As String, Optional solutionItemType As ESolutionItemType = ESolutionItemType.Compile, Optional encoding As System.Text.Encoding = Nothing)
  Sub WriteToOutputWindow(test As String)

  Function RessolveRelativeFileName(relativeFileName As String) As String

  ReadOnly Property TemplateFileNameWithoutExtension As String




End Interface
