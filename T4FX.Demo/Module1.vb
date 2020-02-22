Imports System
Imports System.IO
Imports System.Text
Imports KSW.T4FX
Imports KSW.T4FX.DbModelGeneration
Imports KSW.T4FX.DbModelGeneration.EFSupport

Module Module1

  Sub Main()
    Dim output As New StringBuilder
    '-----------------------------------------------------------------------------------
    Using context As New GeneratorContext("C:\Temp", output)

      Dim genHost = context.Host
      Dim generationEnvironment = context.GenerationEnvironment

      '###########################################################

      Dim inputFiles As String() = {
        ".\Model.edmx",
        ".\Model.custom.xml"
      }

      '###########################################################

      Dim loadedDefinition As DbModelDefinition
      Dim finalDefinition As DbModelDefinition = Nothing
      For Each inputFile In inputFiles
        Select Case IO.Path.GetExtension(inputFile).ToLower()
          Case ".edmx" : loadedDefinition = EdmxToDbModelTransformer.ReadAndTransformEdmxFile(inputFile)
          Case ".xml" : loadedDefinition = DbModelXmlSerializer.DeserializeFromFile(inputFile)
          Case ".json" : loadedDefinition = DbModelJsonSerializer.DeserializeFromFile(inputFile)
          Case Else
            Throw New Exception("Invalid input file type")
        End Select
        If (finalDefinition Is Nothing) Then
          finalDefinition = loadedDefinition
        Else
          finalDefinition = DbModelMerger.MergeDefinitions(finalDefinition, loadedDefinition)
        End If
      Next

      genHost.DeleteOldOutputs()

      DbModelXmlSerializer.Serialize(finalDefinition, generationEnvironment)
      context.Host.SaveOutput(genHost.TemplateFileNameWithoutExtension + ".xml", VisualStudioGenerationHostWrapper.ESolutionItemType.EmbeddedResource)

      'DbModelJsonSerializer.Serialize(finalDefinition, generationEnvironment)
      'context.Host.SaveOutput(genHost.TemplateFileNameWithoutExtension + ".json", VisualStudioGenerationHostWrapper.ESolutionItemType.EmbeddedResource)

      Dim pocoGenerator As New PocoGenerator(finalDefinition, context)
      pocoGenerator.Generate()


    End Using
    '-----------------------------------------------------------------------------------
    Using context As New GeneratorContext("C:\Temp", output)

      Dim genHost = context.Host
      Dim generationEnvironment = context.GenerationEnvironment

      Dim inputFile = ".\Model.xml"
      Dim definition = DbModelXmlSerializer.DeserializeFromFile(inputFile)

      genHost.DeleteOldOutputs()


      Dim dbContextGenerator As New EfDbContextGenerator(definition, context)
      dbContextGenerator.Generate()

      Dim mappingGenerator As New EfMappingGenerator(definition, context)
      mappingGenerator.Generate()

    End Using
    '-----------------------------------------------------------------------------------
    Console.Write(output.ToString())
    Console.ReadLine()
  End Sub

End Module
