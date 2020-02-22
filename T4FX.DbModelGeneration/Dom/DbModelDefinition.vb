
Public Class DbModelDefinition

  Public Property Name As String

  Public DefineEntities As DefineEntityNode()

  Public UndefineEntities As UndefineEntityNode()

  Public DefineRelations As DefineRelationNode()

  Public UndefineRelation As UndefineRelationNode()

End Class

Public Class DefineEntityNode

  Public Property Name As String

  Public DefineFields As DefineFieldNode()

  Public UndefineFields As UndefineFieldNode()

End Class

Public Class UndefineEntityNode
  Public Property Name As String
End Class

Public Class DefineFieldNode
  Public Property Name As String
End Class

Public Class UndefineFieldNode
  Public Property Name As String
End Class

Public Class DefineRelationNode
  Public Property Name As String
End Class

Public Class UndefineRelationNode
  Public Property Name As String
End Class
