﻿Imports Microsoft.VisualBasic

Imports System
Imports System.Collections.Generic
Imports System.Collections
Imports System.Linq
Imports System.Data.Entity
Imports System.Data.Entity.Design
Imports System.IO
Imports System.Data.Objects
Imports System.Data.Objects.DataClasses
Imports System.Xml
Imports System.Xml.Linq
Imports System.Globalization
Imports System.Reflection
Imports System.Data.Metadata.Edm
Imports System.Data.Mapping
Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.Text

Public Class EF_Utility_VB_ttinclude

  Public Shared TemplateMetadata As New Dictionary(Of String, String)()

  ''' <summary>
  ''' Responsible for helping to create source code that is
  ''' correctly formated and functional
  ''' </summary>
  Public Class CodeGenerationTools
    Private ReadOnly _textTransformation As DynamicTextTransformation
    Private ReadOnly _code As VBCodeProvider
    Private ReadOnly _ef As MetadataTools

    ''' <summary>
    ''' Initializes a new CodeGenerationTools object with the TextTransformation (T4 generated class)
    ''' that is currently running.
    ''' </summary>
    Public Sub New(ByVal textTransformation As Object)
      If textTransformation Is Nothing Then
        Throw New ArgumentNullException("textTransformation")
      End If

      _textTransformation = DynamicTextTransformation.Create(textTransformation)
      _code = New VBCodeProvider()
      _ef = New MetadataTools(_textTransformation)
      FullyQualifySystemTypes = False
      CamelCaseFields = True
    End Sub

    ''' <summary>
    ''' When true, all types that are not being generated
    ''' are fully qualified to keep them from conflicting with
    ''' types that are being generated. Useful when you have
    ''' something like a type being generated named System.
    '''
    ''' Default is false.
    ''' </summary>
    Private _FullyQualifySystemTypes As Boolean
    Public Property FullyQualifySystemTypes() As Boolean
      Get
        Return _FullyQualifySystemTypes
      End Get
      Set(ByVal value As Boolean)
        _FullyQualifySystemTypes = value
      End Set
    End Property

    ''' <summary>
    ''' When true, the field names are Camel Cased,
    ''' otherwise they will preserve the case they
    ''' start with.
    '''
    ''' Default is true.
    ''' </summary>
    Private _CamelCaseFields As Boolean
    Public Property CamelCaseFields() As Boolean
      Get
        Return _CamelCaseFields
      End Get
      Set(ByVal value As Boolean)
        _CamelCaseFields = value
      End Set
    End Property

    ''' <summary>
    ''' Returns the NamespaceName suggested by VS if running inside VS. Otherwise, returns
    ''' null.
    ''' </summary>
    Public Function VsNamespaceSuggestion() As String
      Dim suggestion As String = _textTransformation.Host.ResolveParameterValue("directiveId", "namespaceDirectiveProcessor", "namespaceHint")
      If String.IsNullOrEmpty(suggestion) Then
        Return Nothing
      End If

      Return suggestion
    End Function

    ''' <summary>
    ''' Returns a string that is safe for use as an identifier in C#.
    ''' Keywords are escaped.
    ''' </summary>
    Public Function Escape(ByVal name As String) As String
      If name Is Nothing Then
        Return Nothing
      End If

      Return _code.CreateEscapedIdentifier(name)
    End Function

    ''' <summary>
    ''' Returns the name of the TypeUsage's EdmType that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal typeUsage As TypeUsage) As String
      If typeUsage Is Nothing Then
        Return Nothing
      End If

      If TypeOf typeUsage.EdmType Is ComplexType OrElse TypeOf typeUsage.EdmType Is EntityType Then
        Return Escape(typeUsage.EdmType.Name)
      ElseIf TypeOf typeUsage.EdmType Is SimpleType Then
        Dim clrType As Type = _ef.UnderlyingClrType(typeUsage.EdmType)
        Dim typeName As String = If(TypeOf typeUsage.EdmType Is EnumType, Escape(typeUsage.EdmType.Name), Escape(clrType))
        If clrType.IsValueType AndAlso _ef.IsNullable(typeUsage) Then
          Return String.Format(CultureInfo.InvariantCulture, "Nullable(Of {0})", typeName)
        End If

        Return typeName
      ElseIf TypeOf typeUsage.EdmType Is CollectionType Then
        Return String.Format(CultureInfo.InvariantCulture, "ICollection(Of {0})", Escape(DirectCast(typeUsage.EdmType, CollectionType).TypeUsage))
      End If

      Throw New ArgumentException("typeUsage")
    End Function

    ''' <summary>
    ''' Returns the name of the EdmMember that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Return Escape(member.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the EdmType that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal type As EdmType) As String
      If type Is Nothing Then
        Return Nothing
      End If

      Return Escape(type.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the EdmFunction that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal edmFunction As EdmFunction) As String
      If edmFunction Is Nothing Then
        Return Nothing
      End If

      Return Escape(edmFunction.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the EnumMember that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal member As EnumMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Return Escape(member.Name)
    End Function


    ''' <summary>
    ''' Returns the name of the EntityContainer that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal container As EntityContainer) As String
      If container Is Nothing Then
        Return Nothing
      End If

      Return Escape(container.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the EntitySet that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal edmSet As EntitySet) As String
      If edmSet Is Nothing Then
        Return Nothing
      End If

      Return Escape(edmSet.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the StructuralType that is safe for
    ''' use as an identifier.
    ''' </summary>
    Public Function Escape(ByVal type As StructuralType) As String
      If type Is Nothing Then
        Return Nothing
      End If

      Return Escape(type.Name)
    End Function

    ''' <summary>
    ''' Returns the NamespaceName with each segment safe to
    ''' use as an identifier.
    ''' </summary>
    Public Function EscapeNamespace(ByVal namespaceName As String) As String
      If String.IsNullOrEmpty(namespaceName) Then
        Return namespaceName
      End If

      Dim parts As String() = namespaceName.Split("."c)
      namespaceName = String.Empty
      For Each part As String In parts
        If namespaceName <> String.Empty Then
          namespaceName += "."
        End If

        namespaceName += Escape(part)
      Next

      Return namespaceName
    End Function

    ''' <summary>
    ''' Returns the name of the EdmMember formatted for
    ''' use as a field identifier.
    '''
    ''' This method changes behavior based on the CamelCaseFields
    ''' setting.
    ''' </summary>
    Public Function FieldName(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Return FieldName(member.Name)
    End Function

    ''' <summary>
    ''' Returns the name of the EntitySet formatted for
    ''' use as a field identifier.
    '''
    ''' This method changes behavior based on the CamelCaseFields
    ''' setting.
    ''' </summary>
    Public Function FieldName(ByVal edmSet As EntitySet) As String
      If edmSet Is Nothing Then
        Return Nothing
      End If


      Return FieldName(edmSet.Name)
    End Function

    Private Function FieldName(ByVal name As String) As String
      If CamelCaseFields Then
        Return "_" & CamelCase(name)
      Else
        Return "_" & name
      End If
    End Function

    ''' <summary>
    ''' Returns the name of the Type object formatted for
    ''' use in source code.
    '''
    ''' This method changes behavior based on the FullyQualifySystemTypes
    ''' setting.
    ''' </summary>
    Public Function Escape(ByVal clrType As Type) As String
      Return Escape(clrType, FullyQualifySystemTypes)
    End Function

    ''' <summary>
    ''' Returns the name of the Type object formatted for
    ''' use in source code.
    ''' </summary>
    Public Function Escape(ByVal clrType As Type, ByVal fullyQualifySystemTypes As Boolean) As String
      If clrType Is Nothing Then
        Return Nothing
      End If

      Dim typeName As String
      If fullyQualifySystemTypes Then
        If (Not clrType.IsArray) Then
          typeName = clrType.FullName
        Else
          typeName = clrType.GetElementType().FullName & "()"
        End If

        typeName = "Global." & typeName
      Else
        typeName = _code.GetTypeOutput(New CodeTypeReference(clrType))
      End If
      Return typeName
    End Function

    ''' <summary>
    ''' Returns the abstract option if the entity is Abstract, otherwise returns String.Empty
    ''' </summary>
    Public Function MustInheritOption(entity As EntityType) As String
      If entity.Abstract Then
        Return "MustInherit"
      End If

      Return String.Empty
    End Function

    ''' <summary>
    ''' Returns the passed in identifier with the first letter changed to lowercase
    ''' </summary>
    Public Function CamelCase(ByVal identifier As String) As String
      If String.IsNullOrEmpty(identifier) Then
        Return identifier
      End If

      If identifier.Length = 1 Then
        Return identifier(0).ToString(CultureInfo.InvariantCulture).ToLowerInvariant()
      End If

      Return identifier(0).ToString(CultureInfo.InvariantCulture).ToLowerInvariant() + identifier.Substring(1)
    End Function

    ''' <summary>
    ''' If the value parameter is null or empty an empty string is returned,
    ''' otherwise it retuns value with a single space concatenated on the end.
    ''' </summary>
    Public Function SpaceAfter(ByVal value As String) As String
      Return StringAfter(value, " ")
    End Function

    ''' <summary>
    ''' If the value parameter is null or empty an empty string is returned,
    ''' otherwise it retuns value with a single space concatenated on the end.
    ''' </summary>
    Public Function SpaceBefore(ByVal value As String) As String
      Return StringBefore(" ", value)
    End Function

    ''' <summary>
    ''' If the value parameter is null or empty an empty string is returned,
    ''' otherwise it retuns value with append concatenated on the end.
    ''' </summary>
    Public Function StringAfter(ByVal value As String, ByVal append As String) As String
      If String.IsNullOrEmpty(value) Then
        Return String.Empty
      End If

      Return value + append
    End Function

    ''' <summary>
    ''' If the value parameter is null or empty an empty string is returned,
    ''' otherwise it retuns value with prepend concatenated on the front.
    ''' </summary>
    Public Function StringBefore(ByVal prepend As String, ByVal value As String) As String
      If String.IsNullOrEmpty(value) Then
        Return String.Empty
      End If

      Return prepend + value
    End Function

    ''' <summary>
    ''' Returns false and shows an error if the supplied type names aren't case-insensitively unique,
    ''' otherwise returns true.
    ''' </summary>
    Public Function VerifyCaseInsensitiveTypeUniqueness(types As IEnumerable(Of String), ByVal sourceFile As String) As Boolean
      Return VerifyCaseInsensitiveUniqueness(types, _
          Function(t) String.Format(CultureInfo.CurrentCulture, GetResourceString("Template_CaseInsensitiveTypeConflict"), t), sourceFile)
    End Function

    ''' <summary>
    ''' Returns false and shows an error if the supplied entity set names aren't case-insensitively unique,
    ''' otherwise returns true.
    ''' </summary>
    Public Function VerifyCaseInsensitiveEntitySetUniqueness(entitySets As IEnumerable(Of String), ByVal entityContainerName As String, ByVal sourceFile As String) As Boolean
      Return VerifyCaseInsensitiveUniqueness(entitySets, Function(e) String.Format(CultureInfo.CurrentCulture, _
              GetResourceString("Template_CaseInsensitiveEntitySetConflict"), entityContainerName, e), sourceFile)
    End Function

    ''' <summary>
    ''' Returns false and shows an error if the supplied type members names aren't case-insensitively unique,
    ''' otherwise returns true.
    ''' </summary>
    Public Function VerifyCaseInsensitiveMemberUniqueness(members As IEnumerable(Of String), ByVal declaringType As String, ByVal sourceFile As String) As Boolean
      Return VerifyCaseInsensitiveUniqueness(members, _
          Function(m) String.Format(CultureInfo.CurrentCulture, _
              GetResourceString("Template_CaseInsensitiveMemberConflict"), declaringType, m), sourceFile)
    End Function

    ''' <summary>
    ''' Returns false and shows an error if the supplied strings aren't case-insensitively unique,
    ''' otherwise returns true.
    ''' </summary>
    Private Function VerifyCaseInsensitiveUniqueness(items As IEnumerable(Of String), formatMessage As Func(Of String, String), ByVal sourceFile As String) As Boolean
      Dim hash As HashSet(Of String) = New HashSet(Of String)(StringComparer.InvariantCultureIgnoreCase)
      For Each item As String In items
        If Not hash.Add(item) Then
          _textTransformation.Errors.Add(New System.CodeDom.Compiler.CompilerError(sourceFile, -1, -1, "6023", formatMessage(item)))
          Return False
        End If
      Next
      Return True
    End Function

    ''' <summary>
    ''' Returns the names of the items in the supplied collection that correspond to O-Space types.
    ''' </summary>
    Public Function GetAllGlobalItems(ByVal itemCollection As EdmItemCollection) As IEnumerable(Of String)
      Return itemCollection.GetItems(Of GlobalItem)() _
              .Where(Function(i) TypeOf i Is EntityType OrElse TypeOf i Is ComplexType OrElse TypeOf i Is EnumType OrElse TypeOf i Is EntityContainer) _
              .Select(Function(g) GetGlobalItemName(g))
    End Function

    ''' <summary>
    ''' Returns the name of the supplied GlobalItem.
    ''' </summary>
    Private Function GetGlobalItemName(item As GlobalItem) As String
      If TypeOf item Is EdmType Then
        Return CType(item, EdmType).Name
      Else
        Return CType(item, EntityContainer).Name
      End If
    End Function

    ''' <summary>
    ''' Returns the names of the members of the supplied EdmType.
    ''' </summary>
    Public Function GetAllDeclaredMembers(edmType As EdmType) As IEnumerable(Of String)
      Dim entity As EntityType = TryCast(edmType, EntityType)
      If Not entity Is Nothing Then
        Dim declaredMembers As IEnumerable(Of EdmMember) = entity.Members.Where(Function(m) m.DeclaringType Is entity)
        Dim decalredNavigationProperties As IEnumerable(Of NavigationProperty) = entity.NavigationProperties.Where(Function(n) n.DeclaringType Is entity)

        Return declaredMembers.Union(decalredNavigationProperties.Cast(Of EdmMember)()).Select(Function(m) m.Name)
      End If

      Dim structural As StructuralType = TryCast(edmType, StructuralType)
      If Not structural Is Nothing Then
        Return structural.Members.Where(Function(m) m.DeclaringType Is structural).Select(Function(m) m.Name)
      End If

      Dim enumType As EnumType = TryCast(edmType, EnumType)
      If Not enumType Is Nothing Then
        Return enumType.Members.Select(Function(m) m.Name)
      End If

      Return Enumerable.Empty(Of String)()
    End Function

    ''' <summary>
    ''' Retuns as full of a name as possible, if a namespace is provided
    ''' the namespace and name are combined with a period, otherwise just
    ''' the name is returned.
    ''' </summary>
    Public Function CreateFullName(ByVal namespaceName As String, ByVal name As String) As String
      If String.IsNullOrEmpty(namespaceName) Then
        Return name
      End If

      Return (namespaceName & ".") + name
    End Function

    ''' <summary>
    ''' Retuns a literal representing the supplied value.
    ''' </summary>
    Public Function CreateLiteral(ByVal value As Object) As String
      If value Is Nothing Then
        Return String.Empty
      End If

      Dim type As Type = value.GetType()
      If type.IsEnum Then
        Return (type.FullName & ".") + value.ToString()
      End If
      If type Is GetType(Guid) Then
        Return String.Format(CultureInfo.InvariantCulture, "New Guid(""{0}"")", DirectCast(value, Guid).ToString("D", CultureInfo.InvariantCulture))
      ElseIf type Is GetType(DateTime) Then
        Return String.Format(CultureInfo.InvariantCulture, "New DateTime({0}, DateTimeKind.Unspecified)", DirectCast(value, DateTime).Ticks)
      ElseIf type Is GetType(Byte()) Then
        Dim arrayInit As String = String.Join(", ", DirectCast(value, Byte()).Select(Function(b) b.ToString(CultureInfo.InvariantCulture)).ToArray())
        Return String.Format(CultureInfo.InvariantCulture, "New Byte() {{{0}}}", arrayInit)
      ElseIf type Is GetType(DateTimeOffset) Then
        Dim dto As DateTimeOffset = DirectCast(value, DateTimeOffset)
        Return String.Format(CultureInfo.InvariantCulture, "New DateTimeOffset({0}, New TimeSpan({1}))", dto.Ticks, dto.Offset.Ticks)
      ElseIf type Is GetType(Decimal) Then
        Return String.Format(CultureInfo.InvariantCulture, "{0}D", DirectCast(value, Decimal).ToString(CultureInfo.InvariantCulture))
      ElseIf type Is GetType(TimeSpan) Then
        Return String.Format(CultureInfo.InvariantCulture, "New TimeSpan({0})", DirectCast(value, TimeSpan).Ticks)
      End If

      Dim expression As CodePrimitiveExpression = New CodePrimitiveExpression(value)
      Dim writer As StringWriter = New StringWriter()
      Dim code As VBCodeProvider = New VBCodeProvider()
      code.GenerateCodeFromExpression(expression, writer, New CodeGeneratorOptions())
      Return writer.ToString()
    End Function

    ''' <summary>
    ''' Returns a resource string from the System.Data.Entity.Design assembly.
    ''' </summary>
    Public Shared Function GetResourceString(resourceName As String, Optional culture As CultureInfo = Nothing) As String
      If _resourceManager Is Nothing Then
        _resourceManager = New System.Resources.ResourceManager("System.Data.Entity.Design", GetType(System.Data.Entity.Design.MetadataItemCollectionFactory).Assembly)
      End If

      Return _resourceManager.GetString(resourceName, culture)
    End Function
    Private Shared _resourceManager As System.Resources.ResourceManager

    Private Const ExternalTypeNameAttributeName As String = "http://schemas.microsoft.com/ado/2006/04/codegeneration:ExternalTypeName"

    ''' <summary>
    ''' Gets the entity, complex, or enum types for which code should be generated from the given item collection.
    ''' Any types for which an ExternalTypeName annotation has been applied in the conceptual model
    ''' metadata (CSDL) are filtered out of the returned list.
    ''' </summary>
    ''' <typeparam name="T">The type of item to return.</typeparam>
    ''' <param name="itemCollection">The item collection to look in.</param>
    ''' <returns>The items to generate.</returns>
    Public Function GetItemsToGenerate(Of T As GlobalItem)(itemCollection As ItemCollection) As IEnumerable(Of T)
      Return itemCollection.GetItems(Of T)().Where(Function(i) Not i.MetadataProperties.Any(Function(p) p.Name = ExternalTypeNameAttributeName))
    End Function

    ''' <summary>
    ''' Returns the escaped type name to use for the given usage of an o-space type. This might be
    ''' an external type name if the ExternalTypeName annotation has been specified in the
    ''' conceptual model metadata (CSDL).
    ''' </summary>
    ''' <param name="typeUsage">The o-space type usage to get a name for.</param>
    ''' <returns>The type name to use.</returns>
    Public Function GetTypeName(typeUsage As TypeUsage) As String
      Return If(typeUsage Is Nothing, Nothing, GetTypeName(typeUsage.EdmType, _ef.IsNullable(typeUsage), modelNamespace:=Nothing))
    End Function

    ''' <summary>
    ''' Returns the escaped type name to use for the given o-space type. This might be
    ''' an external type name if the ExternalTypeName annotation has been specified in the
    ''' conceptual model metadata (CSDL).
    ''' </summary>
    ''' <param name="edmType">The o-space type to get a name for.</param>
    ''' <returns>The type name to use.</returns>
    Public Function GetTypeName(edmType As EdmType) As String
      Return GetTypeName(edmType, isNullable:=Nothing, modelNamespace:=Nothing)
    End Function

    ''' <summary>
    ''' Returns the escaped type name to use for the given usage of an o-space type. This might be
    ''' an external type name if the ExternalTypeName annotation has been specified in the
    ''' conceptual model metadata (CSDL).
    ''' </summary>
    ''' <param name="typeUsage">The o-space type usage to get a name for.</param>
    ''' <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
    ''' fully qualified name will be returned.</param>
    ''' <returns>The type name to use.</returns>
    Public Function GetTypeName(typeUsage As TypeUsage, modelNamespace As String) As String
      Return If(typeUsage Is Nothing, Nothing, GetTypeName(typeUsage.EdmType, _ef.IsNullable(typeUsage), modelNamespace))
    End Function

    ''' <summary>
    ''' Returns the escaped type name to use for the given o-space type. This might be
    ''' an external type name if the ExternalTypeName annotation has been specified in the
    ''' conceptual model metadata (CSDL).
    ''' </summary>
    ''' <param name="edmType">The o-space type to get a name for.</param>
    ''' <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
    ''' fully qualified name will be returned.</param>
    ''' <returns>The type name to use.</returns>
    Public Function GetTypeName(edmType As EdmType, modelNamespace As String) As String
      Return GetTypeName(edmType, isNullable:=Nothing, modelNamespace:=modelNamespace)
    End Function

    ''' <summary>
    ''' Returns the escaped type name to use for the given o-space type. This might be
    ''' an external type name if the ExternalTypeName annotation has been specified in the
    ''' conceptual model metadata (CSDL).
    ''' </summary>
    ''' <param name="edmType">The o-space type to get a name for.</param>
    ''' <param name="isNullable">Set this to true for nullable usage of this type.</param>
    ''' <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
    ''' fully qualified name will be returned.</param>
    ''' <returns>The type name to use.</returns>
    Private Function GetTypeName(edmType As EdmType, isNullable As System.Nullable(Of Boolean), modelNamespace As String) As String
      If edmType Is Nothing Then
        Return Nothing
      End If

      Dim collectionType = TryCast(edmType, CollectionType)
      If collectionType IsNot Nothing Then
        Return String.Format(CultureInfo.InvariantCulture, "ICollection( Of {0})", GetTypeName(collectionType.TypeUsage, modelNamespace))
      End If

      Dim typeName = If(Escape(edmType.MetadataProperties.Where(Function(p) p.Name = ExternalTypeNameAttributeName).[Select](Function(p) DirectCast(p.Value, String)).FirstOrDefault()), (If(modelNamespace IsNot Nothing AndAlso edmType.NamespaceName <> modelNamespace, CreateFullName(EscapeNamespace(edmType.NamespaceName), Escape(edmType)), Escape(edmType))))

      If TypeOf edmType Is StructuralType Then
        Return typeName
      End If

      If TypeOf edmType Is SimpleType Then
        Dim clrType = _ef.UnderlyingClrType(edmType)
        If Not (TypeOf edmType Is EnumType) Then
          typeName = Escape(clrType)
        End If

        Return If(clrType.IsValueType AndAlso isNullable = True, String.Format(CultureInfo.InvariantCulture, "Nullable(Of {0})", typeName), typeName)
      End If

      Throw New ArgumentException("typeUsage")
    End Function

  End Class

  ''' <summary>
  ''' Responsible for making the Entity Framework Metadata more
  ''' accessible for code generation.
  ''' </summary>
  Public Class MetadataTools
    Private ReadOnly _textTransformation As DynamicTextTransformation

    ''' <summary>
    ''' Initializes an MetadataTools Instance with the
    ''' TextTransformation (T4 generated class) that is currently running
    ''' </summary>
    Public Sub New(ByVal textTransformation As Object)
      If textTransformation Is Nothing Then
        Throw New ArgumentNullException("textTransformation")
      End If

      _textTransformation = DynamicTextTransformation.Create(textTransformation)
    End Sub

    ''' <summary>
    ''' This method returns the underlying CLR type of the O-space type corresponding to the supplied <paramref name="typeUsage"/>
    ''' Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
    ''' </summary>
    Public Function ClrType(typeUsage As TypeUsage) As Type
      Return UnderlyingClrType(typeUsage.EdmType)
    End Function

    ''' <summary>
    ''' This method returns the underlying CLR type of given the O-space type.
    ''' Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
    ''' </summary>
    Public Function UnderlyingClrType(edmType As EdmType) As Type
      Dim primitiveType = TryCast(edmType, PrimitiveType)
      If primitiveType IsNot Nothing Then
        Return primitiveType.ClrEquivalentType
      End If

      Dim enumType = TryCast(edmType, EnumType)
      If enumType IsNot Nothing Then
        Return enumType.UnderlyingType.ClrEquivalentType
      End If

      Return GetType(Object)
    End Function

    ''' <summary>
    ''' True if the EdmProperty is a key of its DeclaringType, False otherwise.
    ''' </summary>
    Public Function IsKey(ByVal edmProp As EdmProperty) As Boolean
      If edmProp IsNot Nothing AndAlso edmProp.DeclaringType.BuiltInTypeKind = BuiltInTypeKind.EntityType Then
        Return DirectCast(edmProp.DeclaringType, EntityType).KeyMembers.Contains(edmProp)
      End If

      Return False
    End Function

    ''' <summary>
    ''' True if the EdmProperty TypeUsage is Nullable, False otherwise.
    ''' </summary>
    Public Function IsNullable(ByVal edmProp As EdmProperty) As Boolean
      Return edmProp IsNot Nothing AndAlso IsNullable(edmProp.TypeUsage)
    End Function

    ''' <summary>
    ''' True if the TypeUsage is Nullable, False otherwise.
    ''' </summary>
    Public Function IsNullable(ByVal typeUsage As TypeUsage) As Boolean
      Dim nullableFacet As Facet = Nothing
      If typeUsage IsNot Nothing AndAlso typeUsage.Facets.TryGetValue("Nullable", True, nullableFacet) Then
        Return CBool(nullableFacet.Value)
      End If

      Return False
    End Function

    ''' <summary>
    ''' If the passed in TypeUsage represents a collection this method returns final element
    ''' type of the collection, otherwise it returns the value passed in.
    ''' </summary>
    Public Function GetElementType(ByVal typeUsage As TypeUsage) As TypeUsage
      If typeUsage Is Nothing Then
        Return Nothing
      End If

      If TypeOf typeUsage.EdmType Is CollectionType Then
        Return GetElementType(DirectCast(typeUsage.EdmType, CollectionType).TypeUsage)
      Else
        Return typeUsage
      End If
    End Function

    ''' <summary>
    ''' Returns the NavigationProperty that is the other end of the same association set if it is
    ''' available, otherwise it returns null.
    ''' </summary>
    Public Function Inverse(ByVal navProperty As NavigationProperty) As NavigationProperty
      If navProperty Is Nothing Then
        Return Nothing
      End If

      Dim toEntity As EntityType = navProperty.ToEndMember.GetEntityType()
      Return toEntity.NavigationProperties.SingleOrDefault(Function(n) n.RelationshipType Is navProperty.RelationshipType AndAlso n IsNot navProperty)
    End Function

    ''' <summary>
    ''' Given a property on the dependent end of a referential constraint, returns the corresponding property on the principal end.
    ''' Requires: The association has a referential constraint, and the specified dependentProperty is one of the properties on the dependent end.
    ''' </summary>
    Public Function GetCorrespondingPrincipalProperty(ByVal navProperty As NavigationProperty, ByVal dependentProperty As EdmProperty) As EdmProperty
      If navProperty Is Nothing Then
        Throw New ArgumentNullException("navProperty")
      End If

      If dependentProperty Is Nothing Then
        Throw New ArgumentNullException("dependentProperty")
      End If

      Dim fromProperties As ReadOnlyMetadataCollection(Of EdmProperty) = GetPrincipalProperties(navProperty)
      Dim toProperties As ReadOnlyMetadataCollection(Of EdmProperty) = GetDependentProperties(navProperty)
      Return fromProperties(toProperties.IndexOf(dependentProperty))
    End Function

    ''' <summary>
    ''' Given a property on the principal end of a referential constraint, returns the corresponding property on the dependent end.
    ''' Requires: The association has a referential constraint, and the specified principalProperty is one of the properties on the principal end.
    ''' </summary>
    Public Function GetCorrespondingDependentProperty(ByVal navProperty As NavigationProperty, ByVal principalProperty As EdmProperty) As EdmProperty
      If navProperty Is Nothing Then
        Throw New ArgumentNullException("navProperty")
      End If

      If principalProperty Is Nothing Then
        Throw New ArgumentNullException("principalProperty")
      End If

      Dim fromProperties As ReadOnlyMetadataCollection(Of EdmProperty) = GetPrincipalProperties(navProperty)
      Dim toProperties As ReadOnlyMetadataCollection(Of EdmProperty) = GetDependentProperties(navProperty)
      Return toProperties(fromProperties.IndexOf(principalProperty))
    End Function

    ''' <summary>
    ''' Gets the collection of properties that are on the principal end of a referential constraint for the specified navigation property.
    ''' Requires: The association has a referential constraint.
    ''' </summary>
    Public Function GetPrincipalProperties(ByVal navProperty As NavigationProperty) As ReadOnlyMetadataCollection(Of EdmProperty)
      If navProperty Is Nothing Then
        Throw New ArgumentNullException("navProperty")
      End If

      Return DirectCast(navProperty.RelationshipType, AssociationType).ReferentialConstraints(0).FromProperties
    End Function

    ''' <summary>
    ''' Gets the collection of properties that are on the dependent end of a referential constraint for the specified navigation property.
    ''' Requires: The association has a referential constraint.
    ''' </summary>
    Public Function GetDependentProperties(ByVal navProperty As NavigationProperty) As ReadOnlyMetadataCollection(Of EdmProperty)
      If navProperty Is Nothing Then
        Throw New ArgumentNullException("navProperty")
      End If

      Return DirectCast(navProperty.RelationshipType, AssociationType).ReferentialConstraints(0).ToProperties
    End Function

    ''' <summary>
    ''' True if this entity type requires the HandleCascadeDelete method defined and the method has
    ''' not been defined on any base type
    ''' </summary>
    Public Function NeedsHandleCascadeDeleteMethod(itemCollection As ItemCollection, entity As EntityType) As Boolean
      Dim needsMethod As Boolean = ContainsCascadeDeleteAssociation(itemCollection, entity)
      ' Check to make sure no base types have already declared this method
      Dim baseType As EntityType = TryCast(entity.BaseType, EntityType)
      While needsMethod AndAlso baseType IsNot Nothing
        needsMethod = Not ContainsCascadeDeleteAssociation(itemCollection, baseType)
        baseType = TryCast(baseType.BaseType, EntityType)
      End While
      Return needsMethod
    End Function

    ''' <summary>
    ''' True if this entity type participates in any relationships where the other end has an OnDelete
    ''' cascade delete defined, or if it is the dependent in any identifying relationships
    ''' </summary>
    Private Function ContainsCascadeDeleteAssociation(itemCollection As ItemCollection, entity As EntityType) As Boolean
      Return itemCollection.GetItems(Of AssociationType)().Where(Function(a) DirectCast(a.AssociationEndMembers(0).TypeUsage.EdmType, RefType).ElementType Is entity AndAlso IsCascadeDeletePrincipal(a.AssociationEndMembers(1)) OrElse DirectCast(a.AssociationEndMembers(1).TypeUsage.EdmType, RefType).ElementType Is entity AndAlso IsCascadeDeletePrincipal(a.AssociationEndMembers(0))).Any()
    End Function

    ''' <summary>
    ''' True if the source end of the specified navigation property is the principal in an identifying relationship.
    ''' or if the source end has cascade delete defined.
    ''' </summary>
    Public Function IsCascadeDeletePrincipal(ByVal navProperty As NavigationProperty) As Boolean
      If navProperty Is Nothing Then
        Throw New ArgumentNullException("navProperty")
      End If

      Return IsCascadeDeletePrincipal(DirectCast(navProperty.FromEndMember, AssociationEndMember))
    End Function

    ''' <summary>
    ''' True if the specified association end is the principal in an identifying relationship.
    ''' or if the association end has cascade delete defined.
    ''' </summary>
    Public Function IsCascadeDeletePrincipal(ByVal associationEnd As AssociationEndMember) As Boolean
      If associationEnd Is Nothing Then
        Throw New ArgumentNullException("associationEnd")
      End If

      Return associationEnd.DeleteBehavior = OperationAction.Cascade OrElse IsPrincipalEndOfIdentifyingRelationship(associationEnd)
    End Function

    ''' <summary>
    ''' True if the specified association end is the principal end in an identifying relationship.
    ''' In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
    ''' </summary>
    Public Function IsPrincipalEndOfIdentifyingRelationship(ByVal associationEnd As AssociationEndMember) As Boolean
      If associationEnd Is Nothing Then
        Throw New ArgumentNullException("associationEnd")
      End If

      Dim refConstraint As ReferentialConstraint = DirectCast(associationEnd.DeclaringType, AssociationType).ReferentialConstraints.Where(Function(rc) rc.FromRole Is associationEnd).SingleOrDefault()
      If refConstraint IsNot Nothing Then
        Dim entity As EntityType = refConstraint.ToRole.GetEntityType()
        Return Not refConstraint.ToProperties.Where(Function(tp) Not entity.KeyMembers.Contains(tp)).Any()
      End If
      Return False
    End Function

    ''' <summary>
    ''' True if the specified association type is an identifying relationship.
    ''' In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
    ''' </summary>
    Public Function IsIdentifyingRelationship(ByVal association As AssociationType) As Boolean
      If association Is Nothing Then
        Throw New ArgumentNullException("association")
      End If

      Return IsPrincipalEndOfIdentifyingRelationship(association.AssociationEndMembers(0)) OrElse IsPrincipalEndOfIdentifyingRelationship(association.AssociationEndMembers(1))
    End Function

    ''' <summary>
    ''' requires: firstType is not null
    ''' effects: if secondType is among the base types of the firstType, return true,
    ''' otherwise returns false.
    ''' when firstType is same as the secondType, return false.
    ''' </summary>
    Public Function IsSubtypeOf(ByVal firstType As EdmType, ByVal secondType As EdmType) As Boolean
      If secondType Is Nothing Then
        Return False
      End If

      ' walk up firstType hierarchy list
      Dim t As EdmType = firstType.BaseType
      While t IsNot Nothing
        If t.Equals(secondType) Then
          Return True
        End If
        t = t.BaseType
      End While
      Return False
    End Function

    ''' <summary>
    ''' Returns the subtype of the EntityType in the current itemCollection
    ''' </summary>
    Public Function GetSubtypesOf(ByVal type As EntityType, ByVal itemCollection As ItemCollection, ByVal includeAbstractTypes As Boolean) As IEnumerable(Of EntityType)
      Dim subTypes As List(Of EntityType) = New List(Of EntityType)
      If type IsNot Nothing Then
        Dim typesInCollection As IEnumerable(Of EntityType) = itemCollection.GetItems(Of EntityType)()
        For Each typeInCollection As EntityType In typesInCollection
          If type.Equals(typeInCollection) = False AndAlso Me.IsSubtypeOf(typeInCollection, type) Then
            If includeAbstractTypes OrElse Not typeInCollection.Abstract Then
              subTypes.Add(typeInCollection)
            End If
          End If
        Next
      End If
      Return subTypes
    End Function

    Public Shared Function TryGetStringMetadataPropertySetting(ByVal item As MetadataItem, ByVal propertyName As String, ByRef value As String) As Boolean
      Dim [property] As MetadataProperty = item.MetadataProperties.FirstOrDefault(Function(p) p.Name = propertyName)
      If [property] IsNot Nothing Then
        value = DirectCast([property].Value, String)
      End If
      Return value IsNot Nothing
    End Function

  End Class

  ''' <summary>
  ''' Responsible for loading an EdmItemCollection from a .edmx file or .csdl files
  ''' </summary>
  Public Class MetadataLoader
    Private ReadOnly _textTransformation As DynamicTextTransformation

    ''' <summary>
    ''' Initializes an MetadataLoader Instance with the
    ''' TextTransformation (T4 generated class) that is currently running
    ''' </summary>
    Public Sub New(ByVal textTransformation As Object)
      If textTransformation Is Nothing Then
        Throw New ArgumentNullException("textTransformation")
      End If

      _textTransformation = DynamicTextTransformation.Create(textTransformation)
    End Sub

    ''' <summary>
    ''' Load the metadata for Edm, Store, and Mapping collections and register them
    ''' with a new MetadataWorkspace, returns false if any of the parts can't be
    ''' created, some of the ItemCollections may be registered and usable even if false is
    ''' returned
    ''' </summary>
    Public Function TryLoadAllMetadata(ByVal inputFile As String, ByRef metadataWorkspace As MetadataWorkspace) As Boolean
      metadataWorkspace = New MetadataWorkspace()

      Dim edmItemCollection As EdmItemCollection = CreateEdmItemCollection(inputFile)
      metadataWorkspace.RegisterItemCollection(edmItemCollection)

      Dim storeItemCollection As StoreItemCollection = Nothing
      If TryCreateStoreItemCollection(inputFile, storeItemCollection) Then
        Dim storageMappingItemCollection As StorageMappingItemCollection = Nothing
        If TryCreateStorageMappingItemCollection(inputFile, edmItemCollection, storeItemCollection, storageMappingItemCollection) Then
          metadataWorkspace.RegisterItemCollection(storeItemCollection)
          metadataWorkspace.RegisterItemCollection(storageMappingItemCollection)
          Return True
        End If
      End If

      Return False
    End Function

    ''' <summary>
    ''' Create an EdmItemCollection loaded with the metadata provided
    ''' </summary>
    Public Function CreateEdmItemCollection(ByVal sourcePath As String, ByVal ParamArray referenceSchemas As String()) As EdmItemCollection
      Dim edmItemCollection As EdmItemCollection = Nothing
      If TryCreateEdmItemCollection(sourcePath, referenceSchemas, edmItemCollection) Then
        Return edmItemCollection
      End If

      Return New EdmItemCollection()
    End Function

    ''' <summary>
    ''' Attempts to create a EdmItemCollection from the specified metadata file
    ''' </summary>
    Public Function TryCreateEdmItemCollection(ByVal sourcePath As String, ByRef edmItemCollection As EdmItemCollection) As Boolean
      Return TryCreateEdmItemCollection(sourcePath, Nothing, edmItemCollection)
    End Function

    ''' <summary>
    ''' Attempts to create a EdmItemCollection from the specified metadata file
    ''' </summary>
    Public Function TryCreateEdmItemCollection(ByVal sourcePath As String, ByVal referenceSchemas As String(), ByRef edmItemCollection As EdmItemCollection) As Boolean
      edmItemCollection = Nothing

      If Not ValidateInputPath(sourcePath, _textTransformation) Then
        Return False
      End If

      If referenceSchemas Is Nothing Then
        referenceSchemas = New String(-1) {}
      End If

      Dim itemCollection As ItemCollection = Nothing
      sourcePath = _textTransformation.Host.ResolvePath(sourcePath)
      Dim collectionBuilder As New EdmItemCollectionBuilder(_textTransformation, referenceSchemas.Select(Function(s) _textTransformation.Host.ResolvePath(s)).Where(Function(s) s <> sourcePath))
      If collectionBuilder.TryCreateItemCollection(sourcePath, itemCollection) Then
        edmItemCollection = DirectCast(itemCollection, EdmItemCollection)
      End If

      Return edmItemCollection IsNot Nothing
    End Function

    ''' <summary>
    ''' Attempts to create a StoreItemCollection from the specified metadata file
    ''' </summary>
    Public Function TryCreateStoreItemCollection(ByVal sourcePath As String, ByRef storeItemCollection As StoreItemCollection) As Boolean
      storeItemCollection = Nothing

      If Not ValidateInputPath(sourcePath, _textTransformation) Then
        Return False
      End If

      Dim itemCollection As ItemCollection = Nothing
      Dim collectionBuilder As New StoreItemCollectionBuilder(_textTransformation)
      If collectionBuilder.TryCreateItemCollection(_textTransformation.Host.ResolvePath(sourcePath), itemCollection) Then
        storeItemCollection = DirectCast(itemCollection, StoreItemCollection)
      End If
      Return storeItemCollection IsNot Nothing
    End Function

    ''' <summary>
    ''' Attempts to create a StorageMappingItemCollection from the specified metadata file, EdmItemCollection, and StoreItemCollection
    ''' </summary>
    Public Function TryCreateStorageMappingItemCollection(ByVal sourcePath As String, ByVal edmItemCollection As EdmItemCollection, ByVal storeItemCollection As StoreItemCollection, ByRef storageMappingItemCollection As StorageMappingItemCollection) As Boolean
      storageMappingItemCollection = Nothing

      If Not ValidateInputPath(sourcePath, _textTransformation) Then
        Return False
      End If

      If edmItemCollection Is Nothing Then
        Throw New ArgumentNullException("edmItemCollection")
      End If

      If storeItemCollection Is Nothing Then
        Throw New ArgumentNullException("storeItemCollection")
      End If

      Dim itemCollection As ItemCollection = Nothing
      Dim collectionBuilder As New StorageMappingItemCollectionBuilder(_textTransformation, edmItemCollection, storeItemCollection)
      If collectionBuilder.TryCreateItemCollection(_textTransformation.Host.ResolvePath(sourcePath), itemCollection) Then
        storageMappingItemCollection = DirectCast(itemCollection, StorageMappingItemCollection)
      End If
      Return storageMappingItemCollection IsNot Nothing
    End Function

    ''' <summary>
    ''' Gets the Model Namespace from the provided schema file.
    ''' </summary>
    Public Function GetModelNamespace(ByVal sourcePath As String) As String
      If Not ValidateInputPath(sourcePath, _textTransformation) Then
        Return String.Empty
      End If

      Dim builder As New EdmItemCollectionBuilder(_textTransformation)
      Dim model As XElement = Nothing
      If builder.TryLoadRootElement(_textTransformation.Host.ResolvePath(sourcePath), model) Then
        Dim attribute As XAttribute = model.Attribute("Namespace")
        If attribute IsNot Nothing Then
          Return attribute.Value
        End If
      End If

      Return String.Empty
    End Function

    ''' <summary>
    ''' Returns true if the specified file path is valid
    ''' </summary>
    Private Shared Function ValidateInputPath(sourcePath As String, textTransformation As DynamicTextTransformation) As Boolean
      If String.IsNullOrEmpty(sourcePath) Then
        Throw New ArgumentException("sourcePath")
      End If

      If sourcePath = "$edmxInputFile$" Then
        textTransformation.Errors.Add(New CompilerError(If(textTransformation.Host.TemplateFile, CodeGenerationTools.GetResourceString("Template_CurrentlyRunningTemplate")), 0, 0, String.Empty, _
            CodeGenerationTools.GetResourceString("Template_ReplaceVsItemTemplateToken")))
        Return False
      End If

      Return True
    End Function

    ''' <summary>
    ''' base class for ItemCollectionBuilder classes that
    ''' load the specific types of metadata
    ''' </summary>
    Private MustInherit Class ItemCollectionBuilder
      Private ReadOnly _textTransformation As DynamicTextTransformation
      Private ReadOnly _fileExtension As String
      Private ReadOnly _edmxSectionName As String
      Private ReadOnly _rootElementName As String

      ''' <summary>
      ''' FileExtension for individual (non-edmx) metadata file for this
      ''' specific ItemCollection type
      ''' </summary>
      Public ReadOnly Property FileExtension() As String
        Get
          Return _fileExtension
        End Get
      End Property

      ''' <summary>
      ''' The name of the XmlElement in the .edmx 'Runtime' element
      ''' to find this ItemCollection's metadata
      ''' </summary>
      Public ReadOnly Property EdmxSectionName() As String
        Get
          Return _edmxSectionName
        End Get
      End Property

      ''' <summary>
      ''' The name of the root element of this ItemCollection's metadata
      ''' </summary>
      Public ReadOnly Property RootElementName() As String
        Get
          Return _rootElementName
        End Get
      End Property

      ''' <summary>
      ''' Method to build the appropriate ItemCollection
      ''' </summary>
      Protected MustOverride Function CreateItemCollection(ByVal readers As IEnumerable(Of XmlReader), ByRef errors As IList(Of EdmSchemaError)) As ItemCollection

      ''' <summary>
      ''' Ctor to setup the ItemCollectionBuilder members
      ''' </summary>
      Protected Sub New(ByVal textTransformation As DynamicTextTransformation, ByVal fileExtension As String, ByVal edmxSectionName As String, ByVal rootElementName As String)
        _textTransformation = textTransformation
        _fileExtension = fileExtension
        _edmxSectionName = edmxSectionName
        _rootElementName = rootElementName
      End Sub

      ''' <summary>
      ''' Selects a namespace from the supplied constants.
      ''' </summary>
      Protected MustOverride Function GetNamespace(ByVal constants As SchemaConstants) As String

      ''' <summary>
      ''' Try to create an ItemCollection loaded with the metadata provided
      ''' </summary>
      Public Function TryCreateItemCollection(ByVal sourcePath As String, ByRef itemCollection As ItemCollection) As Boolean
        itemCollection = Nothing

        If Not ValidateInputPath(sourcePath, _textTransformation) Then
          Return False
        End If

        Dim schemaElement As XElement = Nothing
        If TryLoadRootElement(sourcePath, schemaElement) Then
          Dim readers As New List(Of XmlReader)()
          Try
            readers.Add(schemaElement.CreateReader())
            Dim errors As IList(Of EdmSchemaError) = Nothing

            Dim tempItemCollection As ItemCollection = CreateItemCollection(readers, errors)
            If ProcessErrors(errors, sourcePath) Then
              Return False
            End If

            itemCollection = tempItemCollection
            Return True
          Finally
            For Each reader As XmlReader In readers
              DirectCast(reader, IDisposable).Dispose()
            Next
          End Try
        End If

        Return False
      End Function

      ''' <summary>
      ''' Tries to load the root element from the metadata file provided
      ''' </summary>
      Public Function TryLoadRootElement(ByVal sourcePath As String, ByRef schemaElement As XElement) As Boolean
        schemaElement = Nothing
        Dim extension As String = Path.GetExtension(sourcePath)
        If extension.Equals(".edmx", StringComparison.InvariantCultureIgnoreCase) Then
          Return TryLoadRootElementFromEdmx(sourcePath, schemaElement)
        ElseIf extension.Equals(FileExtension, StringComparison.InvariantCultureIgnoreCase) Then
          ' load from single metadata file (.csdl, .ssdl, or .msl)
          schemaElement = XElement.Load(sourcePath, LoadOptions.SetBaseUri Or LoadOptions.SetLineInfo)
          Return True
        End If

        Return False
      End Function

      ''' <summary>
      ''' Tries to load the root element from the provided edmxDocument
      ''' </summary>
      Private Function TryLoadRootElementFromEdmx(ByVal edmxDocument As XElement, ByVal schemaConstants As SchemaConstants, ByVal sectionName As String, ByVal rootElementName As String, ByRef rootElement As XElement) As Boolean
        rootElement = Nothing

        Dim edmxNs As XNamespace = schemaConstants.EdmxNamespace
        Dim sectionNs As XNamespace = GetNamespace(schemaConstants)

        Dim runtime As XElement = edmxDocument.Element(edmxNs + "Runtime")
        If runtime Is Nothing Then
          Return False
        End If

        Dim section As XElement = runtime.Element(edmxNs + sectionName)
        If section Is Nothing Then
          Return False
        End If

        Dim templateVersion As String = Nothing

        If Not TemplateMetadata.TryGetValue(MetadataConstants.TT_TEMPLATE_VERSION, templateVersion) Then
          templateVersion = MetadataConstants.DEFAULT_TEMPLATE_VERSION
        End If

        If schemaConstants.MinimumTemplateVersion > New Version(templateVersion) Then
          _textTransformation.Errors.Add(New CompilerError(If(_textTransformation.Host.TemplateFile, CodeGenerationTools.GetResourceString("Template_CurrentlyRunningTemplate")), 0, 0, String.Empty, _
              CodeGenerationTools.GetResourceString("Template_UnsupportedSchema")) With { _
                  .IsWarning = True _
              })
        End If

        rootElement = section.Element(sectionNs + rootElementName)
        Return rootElement IsNot Nothing
      End Function

      ''' <summary>
      ''' Tries to load the root element from the provided .edmx metadata file
      ''' </summary>
      Private Function TryLoadRootElementFromEdmx(ByVal edmxPath As String, ByRef rootElement As XElement) As Boolean
        rootElement = Nothing

        Dim element As XElement = XElement.Load(edmxPath, LoadOptions.SetBaseUri Or LoadOptions.SetLineInfo)

        Return TryLoadRootElementFromEdmx(element, MetadataConstants.V3_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, rootElement) OrElse TryLoadRootElementFromEdmx(element, MetadataConstants.V2_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, rootElement) OrElse TryLoadRootElementFromEdmx(element, MetadataConstants.V1_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, rootElement)
      End Function

      ''' <summary>
      ''' Takes an Enumerable of EdmSchemaErrors, and adds them
      ''' to the errors collection of the template class
      ''' </summary>
      Private Function ProcessErrors(ByVal errors As IEnumerable(Of EdmSchemaError), ByVal sourceFilePath As String) As Boolean
        Dim foundErrors As Boolean = False
        For Each schemaError As EdmSchemaError In errors
          Dim newError As New CompilerError(schemaError.SchemaLocation, schemaError.Line, schemaError.Column, schemaError.ErrorCode.ToString(CultureInfo.InvariantCulture), schemaError.Message)
          newError.IsWarning = schemaError.Severity = EdmSchemaErrorSeverity.Warning
          foundErrors = foundErrors Or schemaError.Severity = EdmSchemaErrorSeverity.Error
          If schemaError.SchemaLocation Is Nothing Then
            newError.FileName = sourceFilePath
          End If
          _textTransformation.Errors.Add(newError)
        Next

        Return foundErrors
      End Function
    End Class

    ''' <summary>
    ''' Builder class for creating a StorageMappingItemCollection
    ''' </summary>
    Private Class StorageMappingItemCollectionBuilder
      Inherits ItemCollectionBuilder
      Private ReadOnly _edmItemCollection As EdmItemCollection
      Private ReadOnly _storeItemCollection As StoreItemCollection

      Public Sub New(ByVal textTransformation As DynamicTextTransformation, ByVal edmItemCollection As EdmItemCollection, ByVal storeItemCollection As StoreItemCollection)
        MyBase.New(textTransformation, MetadataConstants.MSL_EXTENSION, MetadataConstants.MSL_EDMX_SECTION_NAME, MetadataConstants.MSL_ROOT_ELEMENT_NAME)
        _edmItemCollection = edmItemCollection
        _storeItemCollection = storeItemCollection
      End Sub

      Protected Overloads Overrides Function CreateItemCollection(ByVal readers As IEnumerable(Of XmlReader), ByRef errors As IList(Of EdmSchemaError)) As ItemCollection
        Return MetadataItemCollectionFactory.CreateStorageMappingItemCollection(_edmItemCollection, _storeItemCollection, readers, errors)
      End Function

      ''' <summary>
      ''' Selects a namespace from the supplied constants.
      ''' </summary>
      Protected Overrides Function GetNamespace(ByVal constants As SchemaConstants) As String
        Return constants.MslNamespace
      End Function
    End Class

    ''' <summary>
    ''' Builder class for creating a StoreItemCollection
    ''' </summary>
    Private Class StoreItemCollectionBuilder
      Inherits ItemCollectionBuilder
      Public Sub New(ByVal textTransformation As DynamicTextTransformation)
        MyBase.New(textTransformation, MetadataConstants.SSDL_EXTENSION, MetadataConstants.SSDL_EDMX_SECTION_NAME, MetadataConstants.SSDL_ROOT_ELEMENT_NAME)
      End Sub

      Protected Overloads Overrides Function CreateItemCollection(ByVal readers As IEnumerable(Of XmlReader), ByRef errors As IList(Of EdmSchemaError)) As ItemCollection
        Return MetadataItemCollectionFactory.CreateStoreItemCollection(readers, errors)
      End Function

      ''' <summary>
      ''' Selects a namespace from the supplied constants.
      ''' </summary>
      Protected Overrides Function GetNamespace(ByVal constants As SchemaConstants) As String
        Return constants.SsdlNamespace
      End Function

    End Class

    ''' <summary>
    ''' Builder class for creating a EdmItemCollection
    ''' </summary>
    Private Class EdmItemCollectionBuilder
      Inherits ItemCollectionBuilder
      Private _referenceSchemas As New List(Of String)()

      Public Sub New(ByVal textTransformation As DynamicTextTransformation)
        MyBase.New(textTransformation, MetadataConstants.CSDL_EXTENSION, MetadataConstants.CSDL_EDMX_SECTION_NAME, MetadataConstants.CSDL_ROOT_ELEMENT_NAME)
      End Sub

      Public Sub New(ByVal textTransformation As DynamicTextTransformation, ByVal referenceSchemas As IEnumerable(Of String))
        Me.New(textTransformation)
        _referenceSchemas.AddRange(referenceSchemas)
      End Sub

      Protected Overloads Overrides Function CreateItemCollection(ByVal readers As IEnumerable(Of XmlReader), ByRef errors As IList(Of EdmSchemaError)) As ItemCollection
        Dim ownedReaders As New List(Of XmlReader)()
        Dim allReaders As New List(Of XmlReader)()
        Try
          allReaders.AddRange(readers)
          For Each path As String In _referenceSchemas.Distinct()
            Dim reference As XElement = Nothing
            If TryLoadRootElement(path, reference) Then
              Dim reader As XmlReader = reference.CreateReader()
              allReaders.Add(reader)
              ownedReaders.Add(reader)
            End If
          Next

          Return MetadataItemCollectionFactory.CreateEdmItemCollection(allReaders, errors)
        Finally
          For Each reader As XmlReader In ownedReaders
            DirectCast(reader, IDisposable).Dispose()
          Next
        End Try
      End Function

      ''' <summary>
      ''' Selects a namespace from the supplied constants.
      ''' </summary>
      Protected Overrides Function GetNamespace(ByVal constants As SchemaConstants) As String
        Return constants.CsdlNamespace
      End Function
    End Class
  End Class

  ''' <summary>
  ''' Responsible for encapsulating the retrieval and translation of the CodeGeneration
  ''' annotations in the EntityFramework Metadata to a form that is useful in code generation.
  ''' </summary>
  Public Class Accessibility
    Private Sub New()
    End Sub
    Private Const GETTER_ACCESS As String = "http://schemas.microsoft.com/ado/2006/04/codegeneration:GetterAccess"
    Private Const SETTER_ACCESS As String = "http://schemas.microsoft.com/ado/2006/04/codegeneration:SetterAccess"
    Private Const TYPE_ACCESS As String = "http://schemas.microsoft.com/ado/2006/04/codegeneration:TypeAccess"
    Private Const METHOD_ACCESS As String = "http://schemas.microsoft.com/ado/2006/04/codegeneration:MethodAccess"
    Private Const ACCESS_PROTECTED As String = "Protected"
    Private Const ACCESS_INTERNAL As String = "Internal"
    Private Const ACCESS_PRIVATE As String = "Private"
    Private Shared ReadOnly AccessibilityRankIdLookup As New Dictionary(Of String, Integer)() From _
        { _
            {"Private", 1}, _
            {"Friend", 2}, _
            {"Protected", 3}, _
            {"Public", 4} _
        }

    ''' <summary>
    ''' Gets the accessibility that should be applied to a type being generated from the provided GlobalItem.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForType(ByVal item As GlobalItem) As String
      If item Is Nothing Then
        Return Nothing
      End If

      Return GetAccessibility(item, TYPE_ACCESS)
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the property level for a property being
    ''' generated from the provided EdmMember.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForProperty(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Dim getterAccess As String = Nothing
      Dim setterAccess As String = Nothing
      Dim propertyAccess As String = Nothing
      CalculatePropertyAccessibility(member, propertyAccess, getterAccess, setterAccess)
      Return propertyAccess
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the property level for a Read-Only property being
    ''' generated from the provided EdmMember.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForReadOnlyProperty(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Return GetAccessibility(member, GETTER_ACCESS)
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the property level for a property being
    ''' generated from the provided EntitySet.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForReadOnlyProperty(ByVal edmSet As EntitySet) As String
      If edmSet Is Nothing Then
        Return Nothing
      End If

      Return GetAccessibility(edmSet, GETTER_ACCESS)
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the property level for a Write-Only property being
    ''' generated from the provided EdmMember.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForWriteOnlyProperty(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Return GetAccessibility(member, SETTER_ACCESS)
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the get level for a property being
    ''' generated from the provided EdmMember.
    '''
    ''' defaults to empty if no annotation is found or the accessibility is the same as the property level.
    ''' </summary>
    Public Shared Function ForGetter(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Dim getterAccess As String = Nothing
      Dim setterAccess As String = Nothing
      Dim propertyAccess As String = Nothing
      CalculatePropertyAccessibility(member, propertyAccess, getterAccess, setterAccess)
      Return getterAccess
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied at the set level for a property being
    ''' generated from the provided EdmMember.
    '''
    ''' defaults to empty if no annotation is found or the accessibility is the same as the property level.
    ''' </summary>
    Public Shared Function ForSetter(ByVal member As EdmMember) As String
      If member Is Nothing Then
        Return Nothing
      End If

      Dim getterAccess As String = Nothing
      Dim setterAccess As String = Nothing
      Dim propertyAccess As String = Nothing
      CalculatePropertyAccessibility(member, propertyAccess, getterAccess, setterAccess)
      Return setterAccess
    End Function

    ''' <summary>
    ''' Gets the accessibility that should be applied to a method being generated from the provided EdmFunction.
    '''
    ''' defaults to public if no annotation is found.
    ''' </summary>
    Public Shared Function ForMethod(ByVal edmFunction As EdmFunction) As String
      If edmFunction Is Nothing Then
        Return Nothing
      End If

      Return GetAccessibility(edmFunction, METHOD_ACCESS)
    End Function

    Private Shared Sub CalculatePropertyAccessibility(ByVal item As MetadataItem, ByRef propertyAccessibility As String, ByRef getterAccessibility As String, ByRef setterAccessibility As String)
      getterAccessibility = GetAccessibility(item, GETTER_ACCESS)
      Dim getterRank As Integer = AccessibilityRankIdLookup(getterAccessibility)

      setterAccessibility = GetAccessibility(item, SETTER_ACCESS)
      Dim setterRank As Integer = AccessibilityRankIdLookup(setterAccessibility)

      Dim propertyRank As Integer = Math.Max(getterRank, setterRank)
      If setterRank = propertyRank Then
        setterAccessibility = String.Empty
      End If

      If getterRank = propertyRank Then
        getterAccessibility = String.Empty
      End If

      propertyAccessibility = AccessibilityRankIdLookup.Where(Function(v) v.Value = propertyRank).Select(Function(v) v.Key).Single()
    End Sub

    Private Shared Function GetAccessibility(ByVal item As MetadataItem, ByVal name As String) As String
      Dim accessibility As String = Nothing
      If MetadataTools.TryGetStringMetadataPropertySetting(item, name, accessibility) Then
        Return TranslateUserAccessibilityToCSharpAccessibility(accessibility)
      End If

      Return "Public"
    End Function

    Private Shared Function TranslateUserAccessibilityToCSharpAccessibility(ByVal userAccessibility As String) As String
      If userAccessibility = ACCESS_PROTECTED Then
        Return "Protected"
      ElseIf userAccessibility = ACCESS_INTERNAL Then
        Return "Friend"
      ElseIf userAccessibility = ACCESS_PRIVATE Then
        Return "Private"
      Else
        ' default to public
        Return "Public"
      End If
    End Function

  End Class

  ''' <summary>
  ''' Responsible for creating source code regions in code when the loop inside
  ''' actually produces something.
  ''' </summary>
  Public Class CodeRegion
    Private Const STANDARD_INDENT_LENGTH As Integer = 4

    Private ReadOnly _textTransformation As DynamicTextTransformation
    Private _beforeRegionLength As Integer
    Private _emptyRegionLength As Integer
    Private _regionIndentLevel As Integer = -1

    ''' <summary>
    ''' Initializes an CodeRegion instance with the
    ''' TextTransformation (T4 generated class) that is currently running
    ''' </summary>
    Public Sub New(ByVal textTransformation As Object)
      If textTransformation Is Nothing Then
        Throw New ArgumentNullException("textTransformation")
      End If

      _textTransformation = DynamicTextTransformation.Create(textTransformation)
    End Sub

    ''' <summary>
    ''' Initializes an CodeRegion instance with the
    ''' TextTransformation (T4 generated class) that is currently running,
    ''' and the indent level to start the first region at.
    ''' </summary>
    Public Sub New(ByVal textTransformation As Object, ByVal firstIndentLevel As Integer)
      Me.New(textTransformation)
      If firstIndentLevel < 0 Then
        Throw New ArgumentException("firstIndentLevel")
      End If

      _regionIndentLevel = firstIndentLevel - 1
    End Sub

    ''' <summary>
    ''' Starts the begining of a region
    ''' </summary>
    Public Sub Begin(ByVal regionName As String)
      If regionName Is Nothing Then
        Throw New ArgumentNullException("regionName")
      End If

      Begin(regionName, 1)
    End Sub

    ''' <summary>
    ''' Start the begining of a region, indented
    ''' the numbers of levels specified
    ''' </summary>
    Public Sub Begin(ByVal regionName As String, ByVal levelsToIncreaseIndent As Integer)
      If regionName Is Nothing Then
        Throw New ArgumentNullException("regionName")
      End If

      _beforeRegionLength = _textTransformation.GenerationEnvironment.Length
      _regionIndentLevel += levelsToIncreaseIndent
      _textTransformation.Write(GetIndent(_regionIndentLevel))
      _textTransformation.WriteLine("#Region """ + regionName + """")
      _emptyRegionLength = _textTransformation.GenerationEnvironment.Length
    End Sub

    ''' <summary>
    ''' Ends a region, or totaly removes it if nothing
    ''' was generted since the begining of the region.
    ''' </summary>
    Public Sub [End]()
      [End](1)
    End Sub

    ''' <summary>
    ''' Ends a region, or totaly removes it if nothing
    ''' was generted since the begining of the region, also outdents
    ''' the number of levels specified.
    ''' </summary>
    Public Sub [End](ByVal levelsToDecrease As Integer)
      Dim indentLevel As Integer = _regionIndentLevel
      _regionIndentLevel -= levelsToDecrease

      If _emptyRegionLength = _textTransformation.GenerationEnvironment.Length Then
        _textTransformation.GenerationEnvironment.Length = _beforeRegionLength
      Else
        _textTransformation.WriteLine(String.Empty)
        _textTransformation.Write(GetIndent(indentLevel))
        _textTransformation.WriteLine("#End Region")
        _textTransformation.WriteLine(String.Empty)
      End If
    End Sub

    ''' <summary>
    ''' Gets the current indent level that the next end region statement will be written
    ''' at
    ''' </summary>
    Public ReadOnly Property CurrentIndentLevel() As Integer
      Get
        Return _regionIndentLevel
      End Get
    End Property

    ''' <summary>
    ''' Get a string of spaces equivelent to the number of indents
    ''' desired.
    ''' </summary>
    Public Shared Function GetIndent(ByVal indentLevel As Integer) As String
      If indentLevel < 0 Then
        Throw New ArgumentException("indentLevel")
      End If

      Return String.Empty.PadLeft(indentLevel * STANDARD_INDENT_LENGTH)
    End Function
  End Class


  ''' <summary>
  ''' Responsible for collecting together the actual method parameters
  ''' and the parameters that need to be sent to the Execute method.
  ''' </summary>
  Public Class FunctionImportParameter
    Private _Source As FunctionParameter
    Public Property Source() As FunctionParameter
      Get
        Return _Source
      End Get
      Set(ByVal value As FunctionParameter)
        _Source = value
      End Set
    End Property
    Private _RawFunctionParameterName As String
    Public Property RawFunctionParameterName() As String
      Get
        Return _RawFunctionParameterName
      End Get
      Set(ByVal value As String)
        _RawFunctionParameterName = value
      End Set
    End Property
    Private _FunctionParameterName As String
    Public Property FunctionParameterName() As String
      Get
        Return _FunctionParameterName
      End Get
      Set(ByVal value As String)
        _FunctionParameterName = value
      End Set
    End Property
    Private _FunctionParameterType As String
    Public Property FunctionParameterType() As String
      Get
        Return _FunctionParameterType
      End Get
      Set(ByVal value As String)
        _FunctionParameterType = value
      End Set
    End Property
    Private _LocalVariableName As String
    Public Property LocalVariableName() As String
      Get
        Return _LocalVariableName
      End Get
      Set(ByVal value As String)
        _LocalVariableName = value
      End Set
    End Property
    Private _RawClrTypeName As String
    Public Property RawClrTypeName() As String
      Get
        Return _RawClrTypeName
      End Get
      Set(ByVal value As String)
        _RawClrTypeName = value
      End Set
    End Property
    Private _ExecuteParameterName As String
    Public Property ExecuteParameterName() As String
      Get
        Return _ExecuteParameterName
      End Get
      Set(ByVal value As String)
        _ExecuteParameterName = value
      End Set
    End Property
    Private _EsqlParameterName As String
    Public Property EsqlParameterName() As String
      Get
        Return _EsqlParameterName
      End Get
      Set(ByVal value As String)
        _EsqlParameterName = value
      End Set
    End Property
    Private _NeedsLocalVariable As Boolean
    Public Property NeedsLocalVariable() As Boolean
      Get
        Return _NeedsLocalVariable
      End Get
      Set(ByVal value As Boolean)
        _NeedsLocalVariable = value
      End Set
    End Property
    Private _IsNullableOfT As Boolean
    Public Property IsNullableOfT() As Boolean
      Get
        Return _IsNullableOfT
      End Get
      Set(ByVal value As Boolean)
        _IsNullableOfT = value
      End Set
    End Property


    ''' <summary>
    ''' Creates a set of FunctionImportParameter objects from the parameters passed in.
    ''' </summary>
    Public Shared Function Create(ByVal parameters As IEnumerable(Of FunctionParameter), ByVal code As CodeGenerationTools, ByVal ef As MetadataTools) As IEnumerable(Of FunctionImportParameter)
      If parameters Is Nothing Then
        Throw New ArgumentNullException("parameters")
      End If

      If code Is Nothing Then
        Throw New ArgumentNullException("code")
      End If

      If ef Is Nothing Then
        Throw New ArgumentNullException("ef")
      End If

      Dim unique As New UniqueIdentifierService()
      Dim importParameters As New List(Of FunctionImportParameter)()
      For Each parameter As FunctionParameter In parameters
        Dim importParameter As New FunctionImportParameter()
        importParameter.Source = parameter
        importParameter.RawFunctionParameterName = unique.AdjustIdentifier(code.CamelCase(parameter.Name))
        importParameter.FunctionParameterName = code.Escape(importParameter.RawFunctionParameterName)
        If parameter.Mode = ParameterMode.In Then
          Dim typeUsage As TypeUsage = parameter.TypeUsage
          importParameter.NeedsLocalVariable = True
          importParameter.FunctionParameterType = code.GetTypeName(typeUsage)
          importParameter.EsqlParameterName = parameter.Name
          Dim clrType As Type = ef.UnderlyingClrType(parameter.TypeUsage.EdmType)
          importParameter.RawClrTypeName = If(TypeOf typeUsage.EdmType Is EnumType, code.GetTypeName(typeUsage.EdmType), code.Escape(clrType))
          importParameter.IsNullableOfT = clrType.IsValueType
        Else
          importParameter.NeedsLocalVariable = False
          importParameter.FunctionParameterType = "ObjectParameter"
          importParameter.ExecuteParameterName = importParameter.FunctionParameterName
        End If
        importParameters.Add(importParameter)
      Next

      ' we save the local parameter uniquification for a second pass to make the visible parameters
      ' as pretty and sensible as possible
      For i As Integer = 0 To importParameters.Count - 1
        Dim importParameter As FunctionImportParameter = importParameters(i)
        If importParameter.NeedsLocalVariable Then
          importParameter.LocalVariableName = unique.AdjustIdentifier(importParameter.RawFunctionParameterName & "Parameter")
          importParameter.ExecuteParameterName = importParameter.LocalVariableName
        End If
      Next

      Return importParameters
    End Function

    '
    ' Class to create unique variables within the same scope
    '
    Private NotInheritable Class UniqueIdentifierService
      Private ReadOnly _knownIdentifiers As HashSet(Of String)

      Public Sub New()
        _knownIdentifiers = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
      End Sub

      ''' <summary>
      ''' Given an identifier, makes it unique within the scope by adding
      ''' a suffix (1, 2, 3, ...), and returns the adjusted identifier.
      ''' </summary>
      Public Function AdjustIdentifier(ByVal identifier As String) As String
        ' find a unique name by adding suffix as necessary
        Dim numberOfConflicts As Integer = 0
        Dim adjustedIdentifier As String = identifier

        While Not _knownIdentifiers.Add(adjustedIdentifier)
          numberOfConflicts += 1
          adjustedIdentifier = identifier + numberOfConflicts.ToString(CultureInfo.InvariantCulture)
        End While

        Return adjustedIdentifier
      End Function
    End Class
  End Class

  ''' <summary>
  ''' Responsible for marking the various sections of the generation,
  ''' so they can be split up into separate files
  ''' </summary>
  Public Class EntityFrameworkTemplateFileManager
    ''' <summary>
    ''' Creates the VsEntityFrameworkTemplateFileManager if VS is detected, otherwise
    ''' creates the file system version.
    ''' </summary>
    Public Shared Function Create(ByVal textTransformation As Object) As EntityFrameworkTemplateFileManager
      Dim transformation As DynamicTextTransformation = DynamicTextTransformation.Create(textTransformation)
      Dim host As IDynamicHost = transformation.Host

#If Not PREPROCESSED_TEMPLATE Then
      Dim hostServiceProvider = host.AsIServiceProvider()
      If hostServiceProvider IsNot Nothing Then
        Dim dte = DirectCast(hostServiceProvider.GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        If dte IsNot Nothing Then
          Return New VsEntityFrameworkTemplateFileManager(transformation)
        End If
      End If
#End If
      Return New EntityFrameworkTemplateFileManager(transformation)
    End Function

    Private NotInheritable Class Block
      Public Name As String
      Public Start As Integer, Length As Integer
    End Class

    Private ReadOnly files As New List(Of Block)()
    Private ReadOnly footer As New Block()
    Private ReadOnly header As New Block()
    Private ReadOnly _textTransformation As DynamicTextTransformation

    ' reference to the GenerationEnvironment StringBuilder on the
    ' TextTransformation object
    Private ReadOnly _generationEnvironment As StringBuilder

    Private m_currentBlock As Block

    ''' <summary>
    ''' Initializes an EntityFrameworkTemplateFileManager Instance  with the
    ''' TextTransformation (T4 generated class) that is currently running
    ''' </summary>
    Private Sub New(ByVal textTransformation As Object)
      If textTransformation Is Nothing Then
        Throw New ArgumentNullException("textTransformation")
      End If

      _textTransformation = DynamicTextTransformation.Create(textTransformation)
      _generationEnvironment = _textTransformation.GenerationEnvironment
    End Sub

    ''' <summary>
    ''' Marks the end of the last file if there was one, and starts a new
    ''' and marks this point in generation as a new file.
    ''' </summary>
    Public Sub StartNewFile(ByVal name As String)
      If name Is Nothing Then
        Throw New ArgumentNullException("name")
      End If

      CurrentBlock = New Block() With {.Name = name}
    End Sub

    Public Sub StartFooter()
      CurrentBlock = footer
    End Sub

    Public Sub StartHeader()
      CurrentBlock = header
    End Sub

    Public Sub EndBlock()
      If CurrentBlock Is Nothing Then
        Exit Sub
      End If

      CurrentBlock.Length = _generationEnvironment.Length - CurrentBlock.Start

      If CurrentBlock IsNot header AndAlso CurrentBlock IsNot footer Then
        files.Add(CurrentBlock)
      End If

      m_currentBlock = Nothing
    End Sub

    ''' <summary>
    ''' Produce the template output files.
    ''' </summary>
    Public Overridable Function Process(Optional split As Boolean = True) As IEnumerable(Of String)
      Dim generatedFileNames As List(Of String) = New List(Of String)()

      If split Then
        EndBlock()

        Dim headerText As String = _generationEnvironment.ToString(header.Start, header.Length)
        Dim footerText As String = _generationEnvironment.ToString(footer.Start, footer.Length)
        Dim outputPath As String = Path.GetDirectoryName(_textTransformation.Host.TemplateFile)

        files.Reverse()

        For Each block As Block In files
          Dim fileName As String = Path.Combine(outputPath, block.Name)
          Dim content = headerText + _generationEnvironment.ToString(block.Start, block.Length) + footerText

          generatedFileNames.Add(fileName)
          CreateFile(fileName, content)
          _generationEnvironment.Remove(block.Start, block.Length)
        Next
      End If

      Return generatedFileNames
    End Function

    Protected Overridable Sub CreateFile(ByVal fileName As String, ByVal content As String)
      If IsFileContentDifferent(fileName, content) Then
        File.WriteAllText(fileName, content)
      End If
    End Sub

    Protected Function IsFileContentDifferent(ByVal fileName As String, ByVal newContent As String) As Boolean
      Return Not (File.Exists(fileName) AndAlso File.ReadAllText(fileName) = newContent)
    End Function

    Private Property CurrentBlock() As Block
      Get
        Return m_currentBlock
      End Get
      Set(ByVal value As Block)
        If CurrentBlock IsNot Nothing Then
          EndBlock()
        End If

        If value IsNot Nothing Then
          value.Start = _generationEnvironment.Length
        End If

        m_currentBlock = value
      End Set
    End Property

#If Not PREPROCESSED_TEMPLATE Then
    Private NotInheritable Class VsEntityFrameworkTemplateFileManager
      Inherits EntityFrameworkTemplateFileManager
      Private templateProjectItem As EnvDTE.ProjectItem
      Private dte As EnvDTE.DTE
      Private checkOutAction As Action(Of String)
      Private projectSyncAction As Action(Of IEnumerable(Of String))

      ''' <summary>
      ''' Creates an instance of the VsEntityFrameworkTemplateFileManager class with the IDynamicHost instance
      ''' </summary>
      Public Sub New(ByVal textTemplating As Object)
        MyBase.New(textTemplating)
        Dim hostServiceProvider = _textTransformation.Host.AsIServiceProvider()
        If hostServiceProvider Is Nothing Then
          Throw New ArgumentNullException("Could not obtain hostServiceProvider")
        End If

        dte = DirectCast(hostServiceProvider.GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        If dte Is Nothing Then
          Throw New ArgumentNullException("Could not obtain DTE from host")
        End If

        templateProjectItem = dte.Solution.FindProjectItem(_textTransformation.Host.TemplateFile)

        checkOutAction = Function(fileName) dte.SourceControl.CheckOutItem(fileName)
        projectSyncAction = Sub(keepFileNames) ProjectSync(templateProjectItem, keepFileNames)
      End Sub

      Public Overloads Overrides Function Process(Optional split As Boolean = True) As IEnumerable(Of String)
        If templateProjectItem.ProjectItems Is Nothing Then
          Return New List(Of String)
        End If

        Dim generatedFileNames As IEnumerable(Of String) = MyBase.Process(split)

        projectSyncAction.EndInvoke(projectSyncAction.BeginInvoke(generatedFileNames, Nothing, Nothing))

        Return generatedFileNames
      End Function

      Protected Overloads Overrides Sub CreateFile(ByVal fileName As String, ByVal content As String)
        If IsFileContentDifferent(fileName, content) Then
          CheckoutFileIfRequired(fileName)
          File.WriteAllText(fileName, content)
        End If
      End Sub

      Private Shared Sub ProjectSync(ByVal templateProjectItem As EnvDTE.ProjectItem, ByVal keepFileNames As IEnumerable(Of String))
        Dim keepFileNameSet = New HashSet(Of String)(keepFileNames)
        Dim projectFiles = New Dictionary(Of String, EnvDTE.ProjectItem)()
        Dim originalOutput = Path.GetFileNameWithoutExtension(templateProjectItem.FileNames(0))

        For Each projectItem As EnvDTE.ProjectItem In templateProjectItem.ProjectItems
          projectFiles.Add(projectItem.FileNames(0), projectItem)
        Next

        ' Remove unused items from the project
        For Each pair As KeyValuePair(Of String, EnvDTE.ProjectItem) In projectFiles
          If Not keepFileNames.Contains(pair.Key) _
            AndAlso Not (Path.GetFileNameWithoutExtension(pair.Key) + ".").StartsWith(originalOutput + ".") Then
            pair.Value.Delete()
          End If
        Next

        ' Add missing files to the project
        For Each fileName As String In keepFileNameSet
          If Not projectFiles.ContainsKey(fileName) Then
            templateProjectItem.ProjectItems.AddFromFile(fileName)
          End If
        Next
      End Sub

      Private Sub CheckoutFileIfRequired(ByVal fileName As String)
        If dte.SourceControl Is Nothing OrElse Not dte.SourceControl.IsItemUnderSCC(fileName) OrElse dte.SourceControl.IsItemCheckedOut(fileName) Then
          Exit Sub
        End If

        ' run on worker thread to prevent T4 calling back into VS
        checkOutAction.EndInvoke(checkOutAction.BeginInvoke(fileName, Nothing, Nothing))
      End Sub
    End Class
#End If
  End Class

  ''' <summary>
  ''' Responsible creating an instance that can be passed
  ''' to helper classes that need to access the TextTransformation
  ''' members. It accesses member by name and signature rather than
  ''' by type. This is necessary when the
  ''' template is being used in Preprocessed mode
  ''' and there is no common known type that can be
  ''' passed instead
  ''' </summary>
  Public Class DynamicTextTransformation
    Private _instance As Object
    Private _dynamicHost As IDynamicHost

    Private ReadOnly _write As MethodInfo
    Private ReadOnly _writeLine As MethodInfo
    Private ReadOnly _generationEnvironment As PropertyInfo
    Private ReadOnly _errors As PropertyInfo
    Private ReadOnly _host As PropertyInfo

    ''' <summary>
    ''' Creates an instance of the DynamicTextTransformation class around the passed in
    ''' TextTransformation shapped instance passed in, or if the passed in instance
    ''' already is a DynamicTextTransformation, it casts it and sends it back.
    ''' </summary>
    Public Shared Function Create(ByVal instance As Object) As DynamicTextTransformation
      If instance Is Nothing Then
        Throw New ArgumentNullException("instance")
      End If

      Dim textTransformation As DynamicTextTransformation = TryCast(instance, DynamicTextTransformation)
      If textTransformation IsNot Nothing Then
        Return textTransformation
      End If

      Return New DynamicTextTransformation(instance)
    End Function

    Private Sub New(ByVal instance As Object)
      _instance = instance
      Dim type As Type = _instance.GetType()
      _write = type.GetMethod("Write", New Type() {GetType(String)})
      _writeLine = type.GetMethod("WriteLine", New Type() {GetType(String)})
      _generationEnvironment = type.GetProperty("GenerationEnvironment", BindingFlags.Instance Or BindingFlags.NonPublic)
      _host = type.GetProperty("Host")
      _errors = type.GetProperty("Errors")
    End Sub

    ''' <summary>
    ''' Gets the value of the wrapped TextTranformation instance's GenerationEnvironment property
    ''' </summary>
    Public ReadOnly Property GenerationEnvironment() As StringBuilder
      Get
        Return DirectCast(_generationEnvironment.GetValue(_instance, Nothing), StringBuilder)
      End Get
    End Property

    ''' <summary>
    ''' Gets the value of the wrapped TextTranformation instance's Errors property
    ''' </summary>
    Public ReadOnly Property Errors() As System.CodeDom.Compiler.CompilerErrorCollection
      Get
        Return DirectCast(_errors.GetValue(_instance, Nothing), System.CodeDom.Compiler.CompilerErrorCollection)
      End Get
    End Property

    ''' <summary>
    ''' Calls the wrapped TextTranformation instance's Write method.
    ''' </summary>
    Public Sub Write(ByVal text As String)
      _write.Invoke(_instance, New Object() {text})
    End Sub

    ''' <summary>
    ''' Calls the wrapped TextTranformation instance's WriteLine method.
    ''' </summary>
    Public Sub WriteLine(ByVal text As String)
      _writeLine.Invoke(_instance, New Object() {text})
    End Sub

    ''' <summary>
    ''' Gets the value of the wrapped TextTranformation instance's Host property
    ''' if available (shows up when hostspecific is set to true in the template directive) and returns
    ''' the appropriate implementation of IDynamicHost
    ''' </summary>
    Public ReadOnly Property Host() As IDynamicHost
      Get
        If _dynamicHost Is Nothing Then
          If _host Is Nothing Then
            _dynamicHost = New NullHost()
          Else
            _dynamicHost = New DynamicHost(_host.GetValue(_instance, Nothing))
          End If
        End If
        Return _dynamicHost
      End Get
    End Property
  End Class


  ''' <summary>
  ''' Reponsible for abstracting the use of Host between times
  ''' when it is available and not
  ''' </summary>
  Public Interface IDynamicHost
    ''' <summary>
    ''' An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    ''' </summary>
    Function ResolveParameterValue(ByVal id As String, ByVal name As String, ByVal otherName As String) As String

    ''' <summary>
    ''' An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    ''' </summary>
    Function ResolvePath(ByVal path As String) As String

    ''' <summary>
    ''' An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    ''' </summary>
    ReadOnly Property TemplateFile() As String

    ''' <summary>
    ''' Returns the Host instance cast as an IServiceProvider
    ''' </summary>
    Function AsIServiceProvider() As IServiceProvider
  End Interface

  ''' <summary>
  ''' Reponsible for implementing the IDynamicHost as a dynamic
  ''' shape wrapper over the Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost interface
  ''' rather than type dependent wrapper. We don't use the
  ''' interface type so that the code can be run in preprocessed mode
  ''' on a .net framework only installed machine.
  ''' </summary>
  Public Class DynamicHost
    Implements IDynamicHost
    Private ReadOnly _instance As Object
    Private ReadOnly _resolveParameterValue As MethodInfo
    Private ReadOnly _resolvePath As MethodInfo
    Private ReadOnly _templateFile As PropertyInfo

    ''' <summary>
    ''' Creates an instance of the DynamicHost class around the passed in
    ''' Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost shapped instance passed in.
    ''' </summary>
    Public Sub New(ByVal instance As Object)
      _instance = instance
      Dim type As Type = _instance.GetType()
      _resolveParameterValue = type.GetMethod("ResolveParameterValue", New Type() {GetType(String), GetType(String), GetType(String)})
      _resolvePath = type.GetMethod("ResolvePath", New Type() {GetType(String)})

      _templateFile = type.GetProperty("TemplateFile")
    End Sub

    ''' <summary>
    ''' A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    ''' </summary>
    Public Function ResolveParameterValue(ByVal id As String, ByVal name As String, ByVal otherName As String) As String Implements IDynamicHost.ResolveParameterValue
      Return DirectCast(_resolveParameterValue.Invoke(_instance, New Object() {id, name, otherName}), String)
    End Function

    ''' <summary>
    ''' A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    ''' </summary>
    Public Function ResolvePath(ByVal path As String) As String Implements IDynamicHost.ResolvePath
      Return DirectCast(_resolvePath.Invoke(_instance, New Object() {path}), String)
    End Function

    ''' <summary>
    ''' A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    ''' </summary>
    Public ReadOnly Property TemplateFile() As String Implements IDynamicHost.TemplateFile
      Get
        Return DirectCast(_templateFile.GetValue(_instance, Nothing), String)
      End Get
    End Property

    ''' <summary>
    ''' Returns the Host instance cast as an IServiceProvider
    ''' </summary>
    Public Function AsIServiceProvider() As IServiceProvider Implements IDynamicHost.AsIServiceProvider
      Return TryCast(_instance, IServiceProvider)
    End Function
  End Class

  ''' <summary>
  ''' Reponsible for implementing the IDynamicHost when the
  ''' Host property is not available on the TextTemplating type. The Host
  ''' property only exists when the hostspecific attribute of the template
  ''' directive is set to true.
  ''' </summary>
  Public Class NullHost
    Implements IDynamicHost
    ''' <summary>
    ''' An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    ''' that simply retuns null.
    ''' </summary>
    Public Function ResolveParameterValue(ByVal id As String, ByVal name As String, ByVal otherName As String) As String Implements IDynamicHost.ResolveParameterValue
      Return Nothing
    End Function

    ''' <summary>
    ''' An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    ''' that simply retuns the path passed in.
    ''' </summary>
    Public Function ResolvePath(ByVal path As String) As String Implements IDynamicHost.ResolvePath
      Return path
    End Function

    ''' <summary>
    ''' An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    ''' that returns null.
    ''' </summary>
    Public ReadOnly Property TemplateFile() As String Implements IDynamicHost.TemplateFile
      Get
        Return Nothing
      End Get
    End Property

    ''' <summary>
    ''' Returns null.
    ''' </summary>
    Public Function AsIServiceProvider() As IServiceProvider Implements IDynamicHost.AsIServiceProvider
      Return Nothing
    End Function
  End Class

  ''' <summary>
  ''' Responsible for encapsulating the constants defined in Metadata
  ''' </summary>
  Public NotInheritable Class MetadataConstants
    Private Sub New()
    End Sub

    Public Const CSDL_EXTENSION As String = ".csdl"

    Public Const CSDL_EDMX_SECTION_NAME As String = "ConceptualModels"
    Public Const CSDL_ROOT_ELEMENT_NAME As String = "Schema"
    Public Const EDM_ANNOTATION_09_02 As String = "http://schemas.microsoft.com/ado/2009/02/edm/annotation"

    Public Const SSDL_EXTENSION As String = ".ssdl"

    Public Const SSDL_EDMX_SECTION_NAME As String = "StorageModels"
    Public Const SSDL_ROOT_ELEMENT_NAME As String = "Schema"

    Public Const MSL_EXTENSION As String = ".msl"

    Public Const MSL_EDMX_SECTION_NAME As String = "Mappings"
    Public Const MSL_ROOT_ELEMENT_NAME As String = "Mapping"

    Public Const TT_TEMPLATE_NAME As String = "TemplateName"
    Public Const TT_TEMPLATE_VERSION As String = "TemplateVersion"
    Public Const TT_MINIMUM_ENTITY_FRAMEWORK_VERSION As String = "MinimumEntityFrameworkVersion"

    Public Const DEFAULT_TEMPLATE_VERSION As String = "4.0"

    Public Shared ReadOnly V1_SCHEMA_CONSTANTS As New SchemaConstants(
        "http://schemas.microsoft.com/ado/2007/06/edmx",
        "http://schemas.microsoft.com/ado/2006/04/edm",
        "http://schemas.microsoft.com/ado/2006/04/edm/ssdl",
        "urn:schemas-microsoft-com:windows:storage:mapping:CS",
        New Version("3.5"))

    Public Shared ReadOnly V2_SCHEMA_CONSTANTS As New SchemaConstants(
        "http://schemas.microsoft.com/ado/2008/10/edmx",
        "http://schemas.microsoft.com/ado/2008/09/edm",
        "http://schemas.microsoft.com/ado/2009/02/edm/ssdl",
        "http://schemas.microsoft.com/ado/2008/09/mapping/cs",
        New Version("4.0"))

    Public Shared ReadOnly V3_SCHEMA_CONSTANTS As New SchemaConstants(
        "http://schemas.microsoft.com/ado/2009/11/edmx",
        "http://schemas.microsoft.com/ado/2009/11/edm",
        "http://schemas.microsoft.com/ado/2009/11/edm/ssdl",
        "http://schemas.microsoft.com/ado/2009/11/mapping/cs",
        New Version("4.5"))
  End Class

  Public Structure SchemaConstants
    Public Sub New(ByVal edmxNamespace As String, ByVal csdlNamespace As String, ByVal ssdlNamespace As String, ByVal mslNamespace As String, ByVal minimumTemplateVersion As Version)
      Me.EdmxNamespace = edmxNamespace
      Me.CsdlNamespace = csdlNamespace
      Me.SsdlNamespace = ssdlNamespace
      Me.MslNamespace = mslNamespace
      Me.MinimumTemplateVersion = minimumTemplateVersion
    End Sub

    Public Property EdmxNamespace() As String
      Get
        Return m_EdmxNamespace
      End Get
      Private Set(value As String)
        m_EdmxNamespace = value
      End Set
    End Property
    Private m_EdmxNamespace As String

    Public Property CsdlNamespace() As String
      Get
        Return m_CsdlNamespace
      End Get
      Private Set(value As String)
        m_CsdlNamespace = value
      End Set
    End Property
    Private m_CsdlNamespace As String

    Public Property SsdlNamespace() As String
      Get
        Return m_SsdlNamespace
      End Get
      Private Set(value As String)
        m_SsdlNamespace = value
      End Set
    End Property
    Private m_SsdlNamespace As String

    Public Property MslNamespace() As String
      Get
        Return m_MslNamespace
      End Get
      Private Set(value As String)
        m_MslNamespace = value
      End Set
    End Property
    Private m_MslNamespace As String

    Public Property MinimumTemplateVersion() As Version
      Get
        Return m_MinimumTemplateVersion
      End Get
      Private Set(value As Version)
        m_MinimumTemplateVersion = value
      End Set
    End Property
    Private m_MinimumTemplateVersion As Version
  End Structure

End Class
