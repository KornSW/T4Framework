Imports System
Imports System.IO
Imports System.Reflection
Imports System.Text

Public Class DbModelXmlSerializer

  Public Shared Function DeserializeFromFile(inputFile As String) As DbModelDefinition
    Throw New NotImplementedException()
  End Function

  Public Shared Function Deserialize(input As String) As DbModelDefinition
    Throw New NotImplementedException()
  End Function

  Public Shared Function Deserialize(input As TextReader) As DbModelDefinition
    Throw New NotImplementedException()
  End Function

  Public Shared Function DeserializeFromEmbeddedFile(assembly As Assembly, resourceName As String) As DbModelDefinition
    Throw New NotImplementedException()
  End Function

  Public Shared Sub Serialize(defintion As DbModelDefinition, target As StringBuilder)
    Throw New NotImplementedException()
  End Sub

  Public Shared Sub Serialize(defintion As DbModelDefinition, target As TextWriter)
    Throw New NotImplementedException()
  End Sub

End Class
