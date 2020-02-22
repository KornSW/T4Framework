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

Public Class PocoGenerator
    Inherits CommonTemplateBase

    Public Sub New(model As DbModelDefinition, generator As GeneratorContext)
      MyBase.New(generator)
    End Sub

    Protected Overrides Sub GenerateInternal(log As StringBuilder)
      log.AppendLine("Generating " & 2.ToString() & " files...")

      Me.Generator.Host.DeleteOldOutputs()

      Me.Generator.Host.WriteToOutputWindow("Hallo !!!")

      'Me.Generator.Host.WriteToStatusBar(0, NumberOfFiles)

      'My.Snippets.ReadonlyProperty("", name)

      For i As Integer = 1 To 2

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
