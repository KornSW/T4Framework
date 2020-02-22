Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Entity
Imports System.Data.Entity.Design
Imports System.Data.Metadata
Imports System.Linq
Imports KSW.T4FX.EF_Utility_VB_ttinclude

Public Class EdmxToDbModelTransformer

#Region " Declarations & Constructor "

  Private _EdmxInputFileFullPath As String
  'Private _TargetModel As Container
  Private _Transformation As Object
  Private _Code As CodeGenerationTools
  Private _Loader As MetadataLoader
  Private _Ef As MetadataTools

  Public Sub New(edmxInputFileFullPath As String, transformation As Object)

    _EdmxInputFileFullPath = edmxInputFileFullPath
    '_TargetModel = targetModel
    _Transformation = transformation

    _Code = New CodeGenerationTools(_Transformation)
    _Loader = New MetadataLoader(_Transformation)
    _Ef = New MetadataTools(_Transformation)

  End Sub

  Public Shared Function ReadAndTransformEdmxFile(inputFile As String) As DbModelDefinition
    Throw New NotImplementedException()
  End Function

#End Region

  Public Sub Process()

    Dim itemCollection As Edm.EdmItemCollection
    Dim sourceContainer As Edm.EntityContainer
    Dim entitySets As IEnumerable(Of Edm.EntitySet)
    Dim entities As IEnumerable(Of Edm.EntityType)
    Dim associations As IEnumerable(Of Edm.AssociationType)

    If (IO.File.Exists(_EdmxInputFileFullPath)) Then
      itemCollection = _Loader.CreateEdmItemCollection(_EdmxInputFileFullPath)
    Else
      Throw New IO.FileNotFoundException(_EdmxInputFileFullPath & " was not found.", _EdmxInputFileFullPath)
    End If


    sourceContainer = itemCollection.GetItems(Of Edm.EntityContainer)().FirstOrDefault()
    If (sourceContainer Is Nothing) Then
      Exit Sub
    End If

    _TargetModel.AttributeValue("SourceInfo") = "EMDX Model (EDM V. " & itemCollection.EdmVersion.ToString() & ")"

    entitySets = sourceContainer.BaseEntitySets.OfType(Of Edm.EntitySet)().OrderBy(Function(s) s.Name)
    entities = itemCollection.GetItems(Of Edm.EntityType)().OrderBy(Function(e) e.Name)
    associations = itemCollection.GetItems(Of Edm.AssociationType)().OrderBy(Function(a) a.Name)

    For Each existingEntitySet As Edm.EntitySet In entitySets
      Dim entityOfSet As Edm.EntityType
      entityOfSet = (From e In entities Where e.Name = existingEntitySet.ElementType.Name).Single
      Me.MapHierachy(existingEntitySet, entityOfSet, entityOfSet, _TargetModel, entities)
    Next

    For Each existingAssociation As Edm.AssociationType In associations

      With _TargetModel.Model.Associations.AddNew()

        .Name = _Code.Escape(existingAssociation.Name)

        .PrimaryEntityId = _Code.Escape((From r In existingAssociation.RelationshipEndMembers
                                         Where r.RelationshipMultiplicity = Edm.RelationshipMultiplicity.One).Single().GetEntityType().Name).ToUpper()
        .ForeignEntityId = _Code.Escape((From r In existingAssociation.RelationshipEndMembers
                                         Where r.RelationshipMultiplicity = Edm.RelationshipMultiplicity.Many Or
                            r.RelationshipMultiplicity = Edm.RelationshipMultiplicity.ZeroOrOne).Single().GetEntityType().Name).ToUpper()

        .Id = .PrimaryEntityId & "_" & .ForeignEntityId

        '.UpdateConstraint 
        '.DeleteConstraint

      End With

    Next

  End Sub

  Private Sub MapHierachy(baseEntitySet As Edm.EntitySet, baseEntity As Edm.EntityType, sourceEntity As Edm.EntityType, targetContainer As Container, allEntities As IEnumerable(Of Edm.EntityType))

    Dim targetEntity As Entity = targetContainer.Entities.AddNew()

    If (sourceEntity.BaseType IsNot Nothing) Then
      targetEntity.InheritedEntity = (From e In targetContainer.Model.EnumerateAllEntities()
                                      Where e.Name = sourceEntity.BaseType.Name).SingleOrDefault
    End If

    'attributes

    targetEntity.Id = _Code.Escape(sourceEntity.Name).ToUpper
    targetEntity.Name = _Code.Escape(sourceEntity.Name)
    targetEntity.PluralName = _Code.Escape(baseEntitySet.Name)
    targetEntity.IsAbstract = sourceEntity.Abstract

    If (sourceEntity.Documentation IsNot Nothing AndAlso sourceEntity.Documentation.Summary IsNot Nothing) Then
      targetEntity.CodeSummary = sourceEntity.Documentation.Summary.Trim()
    End If

    'scalar properties

    For Each sourceProperty In sourceEntity.Properties
      Me.MapScalarProperty(sourceProperty, targetEntity.ScalarProperties.AddNew, sourceEntity, targetContainer)
    Next

    'navigation properties

    For Each sourceProperty In sourceEntity.NavigationProperties
      Me.MapNavigationProperty(sourceProperty, targetEntity.NavigationProperties.AddNew, sourceEntity, targetContainer)
    Next

    'recursive call for inheriting entities
    Dim inheritingEntities = From e In allEntities Where e.BaseType IsNot Nothing AndAlso e.BaseType.Name = sourceEntity.Name
    For Each inheritingEntity As Edm.EntityType In inheritingEntities
      Me.MapHierachy(baseEntitySet, baseEntity, inheritingEntity, targetContainer, allEntities)
    Next

  End Sub

  Private Sub MapScalarProperty(sourceProperty As Edm.EdmProperty, targetProperty As ScalarProperty, sourceEntity As Edm.EntityType, targetContainer As Container)

    targetProperty.Id = _Code.Escape(sourceEntity.Name).ToUpper & "_" & _Code.Escape(sourceProperty.Name).ToUpper()
    targetProperty.Name = _Code.Escape(sourceProperty.Name)
    targetProperty.TypeName = _Code.Escape(sourceProperty.TypeUsage)

    targetProperty.IsPrimaryKey = False
    targetProperty.IsNullable = sourceProperty.Nullable

    If (sourceProperty.DefaultValue IsNot Nothing) Then
      targetProperty.DefaultValue = sourceProperty.DefaultValue.ToString()
    End If

    'targetProperty.CodeGetterAccess = sourceProperty.
    'targetProperty.CodeSetterAccess
    'targetProperty.AutoValue = 

    If (sourceProperty.TypeUsage.Facets.Contains("MaxLength")) Then
      Dim value As Object = sourceProperty.TypeUsage.Facets("MaxLength").Value
      If (value IsNot Nothing) Then
        Integer.TryParse(value.ToString(), targetProperty.MaxLength)
      End If
    End If

    If (sourceProperty.Documentation IsNot Nothing AndAlso sourceProperty.Documentation.Summary IsNot Nothing) Then
      targetProperty.CodeSummary = sourceProperty.Documentation.Summary.Trim()
    End If

    If (sourceEntity.KeyMembers IsNot Nothing) Then
      For Each keyProperty As Edm.EdmMember In sourceEntity.KeyMembers
        If (targetProperty.Name = _Code.Escape(keyProperty.Name)) Then
          targetProperty.IsPrimaryKey = True
          Exit For
        End If
      Next
    End If

  End Sub

  Private Sub MapNavigationProperty(sourceProperty As Edm.NavigationProperty, targetProperty As NavigationProperty, sourceEntity As Edm.EntityType, targetContainer As Container)

    targetProperty.Id = _Code.Escape(sourceEntity.Name).ToUpper & "_" & _Code.Escape(sourceProperty.Name).ToUpper()
    targetProperty.Name = _Code.Escape(sourceProperty.Name)



    If (sourceProperty.Documentation IsNot Nothing AndAlso sourceProperty.Documentation.Summary IsNot Nothing) Then
      targetProperty.CodeSummary = sourceProperty.Documentation.Summary.Trim()
    End If



    Dim localEntity = sourceProperty.FromEndMember.GetEntityType() 'it is possible that this is a base entity instead of our sourceEntity!!!
    Dim localEntityName = _Code.Escape(localEntity)
    Dim remoteEntity = sourceProperty.ToEndMember.GetEntityType()
    Dim remoteEntityName = _Code.Escape(remoteEntity)

    'what is the REMOTE multiplicity, out association is pointin to?
    Select Case sourceProperty.ToEndMember.RelationshipMultiplicity

      Case Edm.RelationshipMultiplicity.One '(this means: we are the foreign entity)
        targetProperty.AssociationId = _Code.Escape(remoteEntityName).ToUpper() & "_" & _Code.Escape(localEntityName).ToUpper()

      Case Edm.RelationshipMultiplicity.Many '(this means: we are the PRIMARY entity)
        targetProperty.AssociationId = _Code.Escape(localEntityName).ToUpper() & "_" & _Code.Escape(remoteEntityName).ToUpper()

      Case Edm.RelationshipMultiplicity.ZeroOrOne '(this means: we are the PRIMARY entity)
        targetProperty.AssociationId = _Code.Escape(localEntityName).ToUpper() & "_" & _Code.Escape(remoteEntityName).ToUpper()

    End Select

    'Dim endTypeProperties As IEnumerable(Of Edm.EdmProperty) = foreignSourceEntity.Properties.Where(Function(p) TypeOf p.TypeUsage.EdmType Is Edm.PrimitiveType AndAlso p.DeclaringType Is foreignSourceEntity)
    'If endTypeProperties.Any() Then
    '  For Each endTypeProperty As Edm.EdmProperty In endTypeProperties
    '    If (TypeOf endTypeProperty.TypeUsage.EdmType Is Edm.PrimitiveType) Then
    '       sourceProperty.ToEndMember.
    '    End If
    '  Next
    'End If

  End Sub


  Private Function FirstToLower(input As String) As String
    If (input.Length > 1) Then
      Return input.Substring(0, 1).ToLower() & input.Substring(1, input.Length - 1)
    Else
      Return input.ToLower()
    End If
  End Function







  Public Sub Temp()



    '    Dim fileManager As EntityFrameworkTemplateFileManager = EntityFrameworkTemplateFileManager.Create(Me)
    '    WriteHeader(fileManager)

    '    Dim container As EntityContainer = ItemCollection.GetItems(Of EntityContainer)().FirstOrDefault()
    '    If (container Is Nothing) Then
    '      Return String.Empty
    '    End If

    '    If (fileTypeName = String.Empty) Then
    '      fileTypeName = container.ToString()
    '    End If



    '    For Each entity As EntityType In ItemCollection.GetItems(Of EntityType)().OrderBy(Function(e) e.Name)


    '    Next
    '    Dim entitySet As EntitySet = Nothing
    '    Dim baseEntity As EntityType = Entity.baseType
    '    Dim isInherited As Boolean = (baseEntity IsNot Nothing)

    '    If (Not isInherited) Then
    '      baseEntity = Entity
    '    End If

    '    For Each existingEntitySet As EntitySet In container.BaseEntitySets.OfType(Of EntitySet)()
    '      If (isInherited) Then
    '        If (existingEntitySet.ElementType.Name = baseEntity.Name) Then
    '          entitySet = existingEntitySet
    '          Exit For
    '        End If
    '      Else
    '        If (existingEntitySet.ElementType.Name = Entity.Name) Then
    '          entitySet = existingEntitySet
    '          Exit For
    '        End If
    '      End If
    '    Next

    '    fileManager.StartNewFile(code.Escape(fileTypeName) & "." & code.Escape(Entity.Name) & ".Generated.vb")

    '    Dim primitiveBaseProperties As IEnumerable(Of EdmProperty) = baseEntity.Properties.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is baseEntity)
    '    Dim primitiveProperties As IEnumerable(Of EdmProperty) = Entity.Properties.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is Entity)
    '    Dim navigationProperties As IEnumerable(Of NavigationProperty) = Entity.NavigationProperties.Where(Function(np) np.DeclaringType Is Entity)
    '    Dim DebuggerDisplayString As String

    '    DebuggerDisplayString = Entity.Name
    '    If (Entity.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(Entity.Documentation.LongDescription)) Then
    '      DebuggerDisplayString = String.Format("{0} ({1})", DebuggerDisplayString, Entity.Documentation.LongDescription)
    '    End If

    '    Dim keyPropertyParams As System.Text.StringBuilder
    '    Dim keyPropertyNames As System.Text.StringBuilder
    '    Dim keyPropertyExpression As System.Text.StringBuilder
    '    Dim keyPropertyMapping As System.Text.StringBuilder
    '    Dim allPropertyParams As System.Text.StringBuilder
    '    Dim allPropertyMapping As System.Text.StringBuilder

    '    allPropertyParams = New System.Text.StringBuilder()
    '    allPropertyMapping = New System.Text.StringBuilder()
    '    If primitiveProperties.Any() Then
    '      For Each edmProperty As EdmProperty In primitiveProperties
    '        If (allPropertyParams.Length > 0) Then
    '          allPropertyParams.Append(", ")
    '          allPropertyMapping.AppendLine()
    '          allPropertyMapping.Append("      ")
    '        End If
    '        allPropertyParams.Append(FirstToLower(code.Escape(edmProperty.Name)))
    '        allPropertyParams.Append(" As ")
    '        allPropertyParams.Append(code.Escape(edmProperty.TypeUsage))
    '        allPropertyMapping.Append("Me.")
    '        allPropertyMapping.Append(code.Escape(edmProperty.Name))
    '        allPropertyMapping.Append(" = ")
    '        allPropertyMapping.Append(FirstToLower(code.Escape(edmProperty.Name)))
    '      Next
    '    End If
    '    If (isInherited) Then
    '      If primitiveBaseProperties.Any() Then
    '        For Each edmProperty As EdmProperty In primitiveBaseProperties
    '          If (allPropertyParams.Length > 0) Then
    '            allPropertyParams.Append(", ")
    '            allPropertyMapping.AppendLine()
    '            allPropertyMapping.Append("      ")
    '          End If
    '          allPropertyParams.Append(FirstToLower(code.Escape(edmProperty.Name)))
    '          allPropertyParams.Append(" As ")
    '          allPropertyParams.Append(code.Escape(edmProperty.TypeUsage))
    '          allPropertyMapping.Append("Me.")
    '          allPropertyMapping.Append(code.Escape(edmProperty.Name))
    '          allPropertyMapping.Append(" = ")
    '          allPropertyMapping.Append(FirstToLower(code.Escape(edmProperty.Name)))
    '        Next
    '      End If
    '    End If

    '    keyPropertyParams = New System.Text.StringBuilder()
    '    keyPropertyMapping = New System.Text.StringBuilder()
    '    keyPropertyNames = New System.Text.StringBuilder()
    '    keyPropertyExpression = New System.Text.StringBuilder()

    '    For Each keyProperty As EdmMember In baseEntity.KeyMembers

    '      If (keyPropertyParams.Length > 0) Then
    '        keyPropertyParams.Append(", ")
    '        keyPropertyNames.Append(", ")
    '        keyPropertyExpression.Append(" And ")
    '        keyPropertyMapping.AppendLine()
    '        keyPropertyMapping.Append("      ")
    '      End If

    '      keyPropertyParams.Append(FirstToLower(code.Escape(keyProperty.Name)))
    '      keyPropertyParams.Append(" As ")
    '      keyPropertyParams.Append(code.Escape(keyProperty.TypeUsage))
    '      keyPropertyNames.Append(FirstToLower(code.Escape(keyProperty.Name)))
    '      keyPropertyMapping.Append("Me.")
    '      keyPropertyMapping.Append(code.Escape(keyProperty.Name))
    '      keyPropertyMapping.Append(" = ")
    '      keyPropertyMapping.Append(FirstToLower(code.Escape(keyProperty.Name)))
    '      keyPropertyExpression.Append("existingItem.")
    '      keyPropertyExpression.Append(code.Escape(keyProperty.Name))
    '      keyPropertyExpression.Append(" = ")
    '      keyPropertyExpression.Append(FirstToLower(code.Escape(keyProperty.Name)))
    '    Next


    '    If (Entity.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(Entity.Documentation.Summary)) Then
    '#>	''' <summary> <#= entity.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "''' ") #> </summary>  
    '<#
    '    End If

    '#>  <EditorBrowsable(EditorBrowsableState.Advanced), DebuggerDisplay("<#= DebuggerDisplayString #>")>
    '  Partial Public Class <#= Code.Escape(entity.Name) #>
    '#Region "..."
    '<#
    '    If (isInherited) Then
    '#>		Inherits <#= Code.Escape(baseEntity.Name) #>
    '<#
    '  Else
    '#>		Inherits DomItem
    '<#
    '    End If
    '#>

    '  Partial Private Shared Sub CustomizeNewItem(newItem as <#= Code.Escape(entity.Name) #>)
    '  End Sub
    '<#
    '  If(Not allPropertyParams.ToString() = keyPropertyParams.ToString())Then
    '#>

    '  ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> and sets ALL properties.</summary>
    '    Public Sub New(<#= allPropertyParams.ToString() #>)
    '    Me.New()
    '        <#= allPropertyMapping.ToString() #>
    '    End Sub
    '<#
    '  End If
    '#>

    '  ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> and sets ONLY the properties which are flagged as KEY.</summary>
    '    Public Sub New(<#= keyPropertyParams.ToString() #>)
    '    Me.New()
    '        <#= keyPropertyMapping.ToString() #>
    '    End Sub

    '  ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> using a lambda-expression to custiomize.</summary>
    '    Public Sub New(customizing As Action(Of <#= Code.Escape(entity.Name) #>))
    '    Me.New()
    '    customizing.Invoke(Me)
    '  End Sub

    '  ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> without setting any properties.</summary>
    '  Public Sub New()
    '      MyBase.New(GetType(<#= Code.Escape(entity.Name) #>))
    '      CustomizeNewItem(Me)
    '    End Sub

    '  ''' <summary>This constructor is for clases which inheriting from <#= Code.Escape(entity.Name) #> (this classes will need to change the xml Element name in this way)</summary>
    '    Protected Sub New(inheritedType as Type)
    '      MyBase.New(inheritedType)
    '      CustomizeNewItem(Me)
    '    End Sub


    '    If primitiveProperties.Any() Then
    '      For Each edmProperty As EdmProperty In primitiveProperties


    '#Region " <#= Code.Escape(edmProperty.Name) #> "

    '    <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>AttributeName As String = "<#= Code.Escape(edmProperty.Name) #>"
    '    <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>Format As String = String.Empty

    '<#	   
    '    If (edmProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(edmProperty.Documentation.Summary))
    '#>		''' <summary> <#= edmProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "  ''' ") #> </summary>  
    '<#
    '    End If
    '#>
    '    Public Property <#= Code.Escape(edmProperty.Name) #> As <#= Code.Escape(edmProperty.TypeUsage) #>
    '      Get
    '        Return MyBase.Xml.GetAttributeValue(Of <#= Code.Escape(edmProperty.TypeUsage) #>)(<#= (Code.Escape(edmProperty.Name)) #>AttributeName, <#= DefaultValueForType(Code.Escape(edmProperty.TypeUsage)) #>, <#= (Code.Escape(edmProperty.Name)) #>Format)
    '      End Get
    '      Set(value As <#= Code.Escape(edmProperty.TypeUsage) #>)
    '        MyBase.Xml.SetAttributeValue(Of <#= Code.Escape(edmProperty.TypeUsage) #>)(<#= (Code.Escape(edmProperty.Name)) #>AttributeName, value, <#= (Code.Escape(edmProperty.Name)) #>Format)
    '      End Set
    '    End Property

    '#End Region
    '<#

    '        Next
    '    End If

    '   If navigationProperties.Any() Then
    '        For Each navigationProperty As NavigationProperty In navigationProperties

    '        If (navigationProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(navigationProperty.Documentation.Summary))
    '#>		''' <summary> <#= navigationProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "  ''' ") #> </summary>  
    '<#
    '      End If

    '  Dim endEntity = NavigationProperty.ToEndMember.GetEntityType()
    '  Dim endType = code.Escape(endEntity)

    '        If(navigationProperty.ToEndMember.RelationshipMultiplicity = RelationshipMultiplicity.Many)
    '  'CHILD COLLECTION:


    '  Dim endTypeProperties As IEnumerable(Of EdmProperty) = endEntity.Properties '.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is endEntity)

    '  Dim simpleProperties As New System.Text.StringBuilder
    '  Dim simplePropertySummaries As New System.Text.StringBuilder
    '        If endTypeProperties.Any() Then
    '            For Each endTypeProperty As EdmProperty In endTypeProperties
    '            If(TypeOf endTypeProperty.TypeUsage.EdmType Is PrimitiveType)
    '              If (simpleProperties.Length > 0) Then
    '                simpleProperties.Append (", ")
    '              End If
    '              simpleProperties.Append(FirstToLower(code.Escape(endTypeProperty.Name)))
    '              simpleProperties.Append(" As ")
    '              simpleProperties.Append(code.Escape(endTypeProperty.TypeUsage))

    '              If (endTypeProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(endTypeProperty.Documentation.Summary))
    '                simplePropertySummaries.AppendLine()
    '                simplePropertySummaries.Append("		''' <param name=""")
    '                simplePropertySummaries.Append(FirstToLower(code.Escape(endTypeProperty.Name)))
    '                simplePropertySummaries.Append(""">")
    '                simplePropertySummaries.Append(endTypeProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "		''' "))
    '                simplePropertySummaries.Append("</param>")
    '              End If
    '            End If
    '            Next
    '        End If

    '#>

    '#Region " <#= Code.Escape(navigationProperty) #> (Child Collection) "

    '    Public ReadOnly Property <#= Code.Escape(navigationProperty) #> As IDomItemList(Of <#= Code.Escape(endType) #>)
    '      Get
    '        Return MyBase.ChildCollection(Of <#= Code.Escape(endType) #>)()
    '      End Get
    '    End Property

    '<#
    '      Else
    '#>

    '#Region " <#= Code.Escape(endType) #> (Parent) "

    '    Public ReadOnly Property <#= Code.Escape(navigationProperty) #> As <#= Code.Escape(endType) #>
    '      Get
    '        Return MyBase.GetParent(Of <#= Code.Escape(endType) #>)()
    '      End Get
    '    End Property

    '<#	
    '      End If
    '#>
    '#End Region
    '<#		  	  
    '        Next
    '    End If
    '#>

    '  End Class

    '  <EditorBrowsable(EditorBrowsableState.Advanced)>
    '  Public Module <#= Code.Escape(entity.Name) #>Extensions

    '#Region " <#= Code.Escape(entity.Name) #> List "

    '    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    '    Public Function Find(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
    '    Return (From existingItem As <#= Code.Escape(entity.Name) #>
    '            In <#= FirstToLower(Code.Escape(entity.Name)) #>List
    '            Where <#= keyPropertyExpression.ToString() #>
    '           ).FirstOrDefault()
    '  End Function

    '    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    '    Public Function AddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
    '    Dim new<#= Code.Escape(entity.Name) #> As New <#= Code.Escape(entity.Name) #>(<#= keyPropertyNames.ToString() #>)
    '    <#= FirstToLower(Code.Escape(entity.Name)) #>List.Add(new<#= Code.Escape(entity.Name) #>)
    '    Return new<#= Code.Escape(entity.Name) #>
    '    End Function

    '    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    '    Public Function FindOrAddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
    '        Dim found<#= Code.Escape(entity.Name) #> As <#= Code.Escape(entity.Name) #>
    '        found<#= Code.Escape(entity.Name) #> = Find(<#= FirstToLower(Code.Escape(entity.Name)) #>List, <#= keyPropertyNames.ToString() #>)

    '        If (found<#= Code.Escape(entity.Name) #> IsNot Nothing)
    '      Return found<#= Code.Escape(entity.Name) #>
    '    Else
    '      Return AddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List, <#= keyPropertyNames.ToString() #>)
    '    End If

    '    End Function

    '#End Region

    '  End Module

    'End Namespace
    '<#

    'Next

    'If Not VerifyTypesAreCaseInsensitiveUnique(ItemCollection) Then
    '    Return ""
    'End If

    'fileManager.Process()














    'Private Sub BeginNamespace(ByVal namespaceName As String, ByVal code As CodeGenerationTools)
    '  Dim region As CodeRegion = New CodeRegion(Me)
    '  If Not String.IsNullOrEmpty(namespaceName) Then
    '#>
    'Namespace <#=code.EscapeNamespace(namespaceName)#>

    '<#+
    '        PushIndent(CodeRegion.GetIndent(1))
    '    End If
    'End Sub

    '  Private Sub EndNamespace(namespaceName As String)
    '    If Not String.IsNullOrEmpty(namespaceName) Then
    '      PopIndent()
    '#>

    'End Namespace





  End Sub

  '  Private Sub WriteProperty(code As CodeGenerationTools, edmProperty As EdmProperty)
  '    WriteProperty(code, edmProperty, code.StringBefore(" = ", code.CreateLiteral(edmProperty.DefaultValue)))
  '  End Sub

  '  Private Sub WriteComplexProperty(code As CodeGenerationTools, complexProperty As EdmProperty)
  '    WriteProperty(code, complexProperty, " = New " & code.Escape(complexProperty.TypeUsage))
  '  End Sub

  '  Private Sub WriteProperty(code As CodeGenerationTools, edmProperty As EdmProperty, defaultValue As String)
  '    WriteProperty(Accessibility.ForProperty(edmProperty), _
  '                  code.Escape(edmProperty.TypeUsage), _
  '                  code.Escape(edmProperty), _
  '                  code.SpaceAfter(Accessibility.ForGetter(edmProperty)), _
  '                  code.SpaceAfter(Accessibility.ForSetter(edmProperty)), _
  '                  defaultValue)
  '  End Sub

  '  Private Sub WriteNavigationProperty(code As CodeGenerationTools, navigationProperty As NavigationProperty)
  '    Dim endType = code.Escape(navigationProperty.ToEndMember.GetEntityType())
  '    Dim defaultValue = ""
  '    Dim propertyType = code.Escape(endType)

  '    If (navigationProperty.ToEndMember.RelationshipMultiplicity = RelationshipMultiplicity.Many) Then
  '      defaultValue = " = New " & propertyType & "Collection"
  '      propertyType = propertyType & "Collection"
  '    End If

  '    WriteProperty(PropertyAccessibilityAndVirtual(navigationProperty), _
  '                  propertyType, _
  '                  code.Escape(navigationProperty), _
  '                  code.SpaceAfter(Accessibility.ForGetter(navigationProperty)), _
  '                  code.SpaceAfter(Accessibility.ForSetter(navigationProperty)), _
  '                  defaultValue)
  '  End Sub

  '  Private Sub WriteProperty(accessibility As String, type As String, name As String, getterAccessibility As String, setterAccessibility As String, defaultValue As String)
  '    If ([String].IsNullOrEmpty(getterAccessibility) AndAlso [String].IsNullOrEmpty(setterAccessibility)) Then
  '#>
  '    <#=accessibility#> Property <#=name#> As <#=type#><#=defaultValue#>
  '<#+
  '    Else
  '#>

  '    Private _<#=name#> As <#=type#><#=defaultValue#>
  '    <#=accessibility#> Property <#=name#> As <#=type#>
  '        <#=getterAccessibility#>Get
  '            Return _<#=name#>
  '        End Get
  '        <#=setterAccessibility#>Set(ByVal value As <#=type#>)
  '            _<#=name#> = value
  '        End Set
  '    End Property
  '<#+
  '    End If
  'End Sub

  '  Private Function PropertyAccessibilityAndVirtual(ByVal member As EdmMember) As String
  '    Dim propertyAccess As String = Accessibility.ForProperty(member)
  '    Dim setAccess As String = Accessibility.ForSetter(member)
  '    Dim getAccess As String = Accessibility.ForGetter(member)
  '    If propertyAccess <> "Private" AndAlso setAccess <> "Private" AndAlso getAccess <> "Private" Then
  '      Return propertyAccess & " Overridable"
  '    End If

  '    Return propertyAccess
  '  End Function

  '  Private Function VerifyTypesAreCaseInsensitiveUnique(ByVal itemCollection As EdmItemCollection) As Boolean
  '    Dim alreadySeen As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

  '    For Each type As StructuralType In itemCollection.GetItems(Of StructuralType)()
  '      If Not (TypeOf type Is EntityType OrElse TypeOf type Is ComplexType) Then
  '        Continue For
  '      End If

  '      If alreadySeen.ContainsKey(type.FullName) Then
  '        [Error](String.Format("This template does not support types that differ only by case, the types {0} are not supported", type.FullName))
  '        Return False
  '      Else
  '        alreadySeen.Add(type.FullName, True)
  '      End If
  '    Next

  '    Return True
  '  End Function

  '  Private Function FirstToLower(input As String) As String
  '    If (input.Length > 1) Then
  '      Return input.Substring(0, 1).ToLower() & input.Substring(1, input.Length - 1)
  '    Else
  '      Return input.ToLower()
  '    End If
  '  End Function

  '  Private Function DefaultValueForType(typeName As String) As String
  '    Select Case typeName.Replace("System.", "").ToLower()
  '      Case "string" : Return "String.Empty"
  '      Case "datetime" : Return "DateTime.MinValue"
  '      Case "boolean" : Return "False"
  '      Case "integer", "int16", "int32", "int64", "decimal", "double", "short" : Return "0"
  '      Case "guid" : Return "System.Guid.Empty "

  '      Case Else : Return "Nothing"
  '    End Select
  '  End Function












End Class
