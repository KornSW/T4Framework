  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Module __EntityName__Extensions

#Region " __EntityName__ List "

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function Find(__EntityNameFirstLCase__List As DomItem.IDomItemList(Of __EntityName__), __CommaSeparatedKeyPropertyParams__) As __EntityName__
    Return (From existingItem As __EntityName__
            In __EntityNameFirstLCase__List
            Where __KeyPropertyExpression__
           ).FirstOrDefault()
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function AddNew(__EntityNameFirstLCase__List As DomItem.IDomItemList(Of __EntityName__), __CommaSeparatedKeyPropertyParams__) As __EntityName__
    Dim new__EntityName__ As New __EntityName__(__CommaSeparatedKeyPropertyNames__)
    __EntityNameFirstLCase__List.Add(new__EntityName__)
    Return new__EntityName__
  End Function

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function FindOrAddNew(__EntityNameFirstLCase__List As DomItem.IDomItemList(Of __EntityName__), __CommaSeparatedKeyPropertyParams__) As __EntityName__
    Dim found__EntityName__ As __EntityName__
    found__EntityName__ = Find(__EntityNameFirstLCase__List, __CommaSeparatedKeyPropertyNames__)

    If (found__EntityName__ IsNot Nothing) Then
      Return found__EntityName__
    Else
      Return AddNew(__EntityNameFirstLCase__List, __CommaSeparatedKeyPropertyNames__)
    End If

  End Function

#End Region

  End Module
