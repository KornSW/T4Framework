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
Imports System.CodeDom.Compiler

Namespace CommonTemplates

  Public Class Demo
    Inherits CommonTemplateBase

    Public Sub New(generator As GeneratorContext)
      MyBase.New(generator)
    End Sub

    Public Property NumberOfFiles As Integer = 1

    Protected Overrides Sub GenerateInternal(log As StringBuilder)
      log.AppendLine("Generating " & NumberOfFiles.ToString() & " files...")

      Me.Generator.Host.DeleteOldOutputs()

      Me.Generator.Host.WriteToOutputWindow("Hallo !!!")

      'Me.Generator.Host.WriteToStatusBar(0, NumberOfFiles)

      'My.Snippets.ReadonlyProperty("", name)

      For i As Integer = 1 To NumberOfFiles

        'Me.Generator.Host.WriteToStatusBar(i, NumberOfFiles)

        Dim filename As String = "File_" & i.ToString() & ".vb"
        Dim writer As New WriterBase(Sub(text) Me.OutputFileContent(filename).Append(text))
        'Me.OutputFileType(filename) = SolutionFileHelper.ESolutionItemType.EmbeddedResource

        writer.WriteLine("'<Class>" & Environment.NewLine & "'Class Test" & i.ToString())

        Using writer.NewIndentScope

          writer.WriteLine("'<Method>" & Environment.NewLine & "'Sub New")

          Using writer.NewIndentScope
            writer.WriteLine("' das sit code")
          End Using

          writer.WriteLine("'End Sub")

        End Using

        writer.WriteLine("'End Class")

      Next

      Me.SaveOutputFiles()

      'Me.Generator.Host.WriteToStatusBar(0, 0)
      log.AppendLine("done")
    End Sub

  End Class

End Namespace
