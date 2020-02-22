Imports System
Imports System.Text
Imports KSW.T4FX
Imports KSW.T4FX.VisualStudioGenerationHostWrapper

Public Class MockGenerationHost
  Implements IGenerationHost

  Public ReadOnly Property TemplateFileNameWithoutExtension As String Implements IGenerationHost.TemplateFileNameWithoutExtension
    Get
      Throw New NotImplementedException()
    End Get
  End Property

  Public Sub DeleteOldOutputs() Implements IGenerationHost.DeleteOldOutputs
    Throw New NotImplementedException()
  End Sub

  Public Sub LogToErrorList(message As String, Optional fileName As String = "", Optional row As Integer = 1, Optional column As Integer = 1) Implements IGenerationHost.LogToErrorList
    Throw New NotImplementedException()
  End Sub

  Public Sub SaveOutput(outputFileName As String, Optional solutionItemType As ESolutionItemType = ESolutionItemType.Compile, Optional encoding As Encoding = Nothing) Implements IGenerationHost.SaveOutput
    Throw New NotImplementedException()
  End Sub

  Public Sub WriteToOutputWindow(test As String) Implements IGenerationHost.WriteToOutputWindow
    Throw New NotImplementedException()
  End Sub

  Public Function RessolveRelativeFileName(relativeFileName As String) As String Implements IGenerationHost.RessolveRelativeFileName
    Throw New NotImplementedException()
  End Function

End Class
