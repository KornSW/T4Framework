
Dim code As New CodeGenerationTools(Me)
Dim loader As New MetadataLoader(Me)
Dim region As New CodeRegion(Me, 1)
Dim ef As New MetadataTools(Me)

Dim inputFile As String
Dim rootObject As String
Dim fileClassName As String
Dim namespaceName As String = code.VsNamespaceSuggestion()

'#####################################################
'# INFO:                                             #
'#   template customized by Tobias Haensel           #
'#                                                   #
'#   version    = 1.9                                #
'#   date       = 2013-10-07                         #
'#                                                   #
'#####################################################
'# CONFIGURATION:                                    #
'#                                                   #
   inputFile     = ".\itl.edmx"
     namespaceName = "%TTFILE%"
   fileClassName = "%TTFILE%File"
   rootObject    = "Library"
'#                                                   #
'#####################################################

Dim fileTypeName As String = IO.Path.GetFileNameWithoutExtension(Host.TemplateFile)

Dim ItemCollection As EdmItemCollection = loader.CreateEdmItemCollection(inputFile)
Dim fileManager As EntityFrameworkTemplateFileManager = EntityFrameworkTemplateFileManager.Create(Me)
WriteHeader(fileManager)

Dim container As EntityContainer = ItemCollection.GetItems(Of EntityContainer)().FirstOrDefault()
If (container Is Nothing) Then
    Return String.Empty
End If

If (fileTypeName = String.Empty) Then
  fileTypeName = container.ToString()
End If

fileClassName = fileClassName.Replace("%TTFILE%",fileTypeName)
namespaceName = namespaceName.Replace("%TTFILE%",fileTypeName)

'#######################################################################################################################
'# ROOT ENTRY POINT (REPRESENTS THE FILE)                                                                              #
'#######################################################################################################################
#>

Namespace <#= Code.Escape(namespaceName) #>

Partial Public NotInheritable Class <#= Code.Escape(fileClassName) #>

#Region " Constructors & Properties "

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Private _RawDocument As XDocument

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Private _Content As <#= Code.Escape(rootObject) #>

  Public Shared Function ReadFromFile(fileName As String) As <#= Code.Escape(fileClassName) #>
    Return New <#= Code.Escape(fileClassName) #>(fileName)
  End Function

  Public Shared Function CreateNew() As <#= Code.Escape(fileClassName) #>
    Return New <#= Code.Escape(fileClassName) #>()
  End Function

  Private Sub New(fileName As String)
    Me.Customize()
    Me.ReadFile(fileName)
  End Sub

  Private Sub New()
    Me.Customize()
    _RawDocument = New XDocument()
    _Content = <#= Code.Escape(rootObject) #>.CreateAndAddToDocumentRoot(Me, _RawDocument)
    Me.InitDefaultStructure()
  End Sub

  Public ReadOnly Property Content As <#= Code.Escape(rootObject) #>
    Get
      Return _Content
    End Get
  End Property

#End Region

#Region " Serialize & Deserialize "

    Private Const XmlFileHeader As String = "<?xml version=""1.0"" encoding=""utf-8""?>"

  Private Property Encoding As System.Text.Encoding = System.Text.Encoding.Default

  Partial Private Sub Customize()
  End Sub

  Public Sub ReadFile(fileName As String)
    Me.UpdateRawContent(File.ReadAllText(fileName, Me.Encoding))
  End Sub

  Public Sub SaveFile(fileName As String)
    File.WriteAllText(fileName, XmlFileHeader & Environment.NewLine & Me.GetRawContent(), Me.Encoding)
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Function GetRawContent() As String
    Dim rawContentString As String
    rawContentString = _RawDocument.ToString()
    Me.SpecializeRawContentString(rawContentString)
    Return rawContentString
  End Function

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub UpdateRawContent(rawContentString As String)
    Me.NormalizeRawContentString(rawContentString)
    _RawDocument = XDocument.Parse(rawContentString)
    _Content = <#= Code.Escape(rootObject) #>.FromDocumentRoot(Me, _RawDocument)
  End Sub

#End Region

#Region " Customizing (Partial Methods) "

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Partial Private Sub InitDefaultStructure()
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Partial Private Sub SpecializeRawContentString(ByRef rawContentString As String)
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Partial Private Sub NormalizeRawContentString(ByRef rawContentString As String)
  End Sub

#End Region

End Class

End Namespace
<#
'#######################################################################################################################
'# FIXED BASE TYPES (ITEM & ITEMCOLLECTION)                                                                            #
'#######################################################################################################################
 fileManager.StartNewFile(Code.Escape(fileTypeName) & ".Base.Generated.vb")
#>

Namespace <#= Code.Escape(namespaceName) #>

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Partial Public MustInherit Class DomItem

#Region " Element Handling "

  Protected Delegate Function PropertyGetter(Of T)(propertyName As String, defaultValue As T, format As String) As T
  Protected Delegate Sub PropertySetter(Of T)(propertyName As String, newValue As T, format As String)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)> Private _Element As XElement
  <DebuggerBrowsable(DebuggerBrowsableState.Never)> Private _Xml As XmlElementEditor
  <DebuggerBrowsable(DebuggerBrowsableState.Never)> Private _ParentList As IDomItemList
  <DebuggerBrowsable(DebuggerBrowsableState.Never)> Private _InheritedType As Type

  Public Sub New(inheritedType As Type)
    _InheritedType = inheritedType
    _Element = New XElement(Me.GetElementName())
    _Xml = New XmlElementEditor(_Element)
    Me.InitializeDomItem()
  End Sub

  Protected Overridable Sub InitializeDomItem()
  End Sub

  Protected Sub SetElement(element As XElement, parentList As IDomItemList)
    If (Not element.Name.ToString().ToLower() = Me.GetElementName().ToLower()) Then
      Throw New ApplicationException(String.Format("The base XML element for the '{0}'-objects must have the name '{1}'. A XML element named '{2}' cannot be applied!", _InheritedType.Name, Me.GetElementName(), element.Name))
    End If
    _Element = element
    _Xml = New XmlElementEditor(_Element)
    _ParentList = parentList
    Me.ReadElement(_Element.Elements)
  End Sub

  Protected Function GetParent(Of T As DomItem)() As T
    If (_ParentList Is Nothing) Then
      Return Nothing
    Else
      Return DirectCast(_ParentList.Parent, T)
    End If
  End Function

  Protected ReadOnly Property Element As XElement
    Get
      Return _Element
    End Get
  End Property

  Protected Overridable Function GetElementName() As String
    Return _InheritedType.Name
  End Function

  Protected Overridable Sub ReadElement(childs As IEnumerable(Of XElement))
  End Sub

  Protected ReadOnly Property Xml As XmlElementEditor
    Get
      Return _Xml
    End Get
  End Property

  'customizing
  Protected Overridable Sub OnNewChildElementInserting(Of T As {DomItem, New})(index As Integer, newChild As T, ByRef xmlNodesCreated As Boolean, ByRef cancelInsert As Boolean)
  End Sub
  Protected Overridable Sub OnNewChildElementInserted(Of T As {DomItem, New})(index As Integer, newChild As T, newElement As XElement)
  End Sub
  Protected Overridable Sub OnChildElementRemoving(Of T As {DomItem, New})(oldChild As T, oldElement As XElement, ByRef xmlNodesRemoved As Boolean, ByRef cancelRemove As Boolean)
  End Sub
  Protected Overridable Sub OnChildElementRemoved(Of T As {DomItem, New})(oldChild As T)
  End Sub

#Region " Getter & Setter "

  Protected Overridable Function DefaultPropertyGetter(Of T)(propertyName As String, defaultValue As T, format As String) As T
    Return Me.Xml.GetAttributeValue(Of T)(propertyName, defaultValue, format)
  End Function
  Protected Overridable Sub DefaultPropertySetter(Of T)(propertyName As String, newValue As T, formatInfo As String)
    Me.Xml.SetAttributeValue(Of T)(propertyName, newValue, formatInfo)
  End Sub

  Protected Overridable Function XmlAttributeGetter(Of T)(propertyName As String, defaultValue As T, format As String) As T
    Return Me.Xml.GetAttributeValue(Of T)(propertyName, defaultValue, format)
  End Function
  Protected Overridable Sub XmlAttributeSetter(Of T)(propertyName As String, newValue As T, formatInfo As String)
    Me.Xml.SetAttributeValue(Of T)(propertyName, newValue, formatInfo)
  End Sub

  Protected Overridable Function XmlRawContentGetter(Of T)(propertyName As String, defaultValue As T, format As String) As T
    Return Me.Xml.GetRawElementContent(Of T)(propertyName, defaultValue, format)
  End Function
  Protected Overridable Sub XmlRawContentSetter(Of T)(propertyName As String, newValue As T, formatInfo As String)
    Me.Xml.SetRawElementContent(Of T)(propertyName, newValue, formatInfo)
  End Sub

#End Region

#End Region

#Region " Child Handling "

  Protected Property Childs As New Dictionary(Of Type, IDomItemList)

  Protected Function ChildCollection(Of T As {DomItem, New})() As DomItemList(Of T)
    Dim childType As Type = GetType(T)
    If (Not Childs.ContainsKey(childType)) Then
      Childs.Add(childType, New DomItemList(Of T)(_Element, Me))
    End If
    Return DirectCast(Childs(childType), DomItemList(Of T))
  End Function

#End Region

#Region " XML Element Editor (nested Class) "

  Partial Protected NotInheritable Class XmlElementEditor

    Private _Element As XElement

    Public Sub New(element As XElement)
      _Element = element

    End Sub

#Region " Attributes "

    Public Function GetRawElementContent() As String
      Return _Element.Value
    End Function

    Public Sub SetRawElementContent(newValue As String)
      _Element.Value = newValue
    End Sub

    Public Function GetRawElementContent(Of T)(Optional formatInfo As String = "") As T
      Return StringToProperty(Of T)(Me.GetRawElementContent(), formatInfo)
    End Function

    'additional overload to have the same signature as 'GetAttributeValue(Of T)' for switching the getter delegates to this
    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Function GetRawElementContent(Of T)(attributeName As String, defaultValue As T, Optional formatInfo As String = "") As T
      Dim content As String = Me.GetRawElementContent()
      If (content = String.Empty) Then
        Return defaultValue
      Else
        Return StringToProperty(Of T)(Me.GetRawElementContent(), formatInfo)
      End If
    End Function

    Public Sub SetRawElementContent(Of T)(newValue As T, Optional formatInfo As String = "")
      Me.SetRawElementContent(StringFromProperty(Of T)(newValue, formatInfo))
    End Sub

    'additional overload to have the same signature as 'SetAttributeValue(Of T)' for switching the setter delegates to this
    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Sub SetRawElementContent(Of T)(attributeName As String, newValue As T, Optional formatInfo As String = "")
      Me.SetRawElementContent(StringFromProperty(Of T)(newValue, formatInfo))
    End Sub

    Public Function GetAttributeValue(attributeName As String, defaultValue As String) As String
      Dim lowerAttributeName = attributeName.ToLower()

      For Each attribute As XAttribute In _Element.Attributes()
        If (attribute.Name.LocalName.ToLower() = lowerAttributeName) Then
          Return attribute.Value
        End If
      Next

      Return defaultValue
    End Function

    Public Function GetAttributeValue(Of T)(attributeName As String, defaultValue As T, Optional formatInfo As String = "") As T
      Dim value As String = Me.GetAttributeValue(attributeName, Nothing)
      Dim returnValue As Object = Nothing

      If (value Is Nothing) Then
        Return defaultValue
      Else
        Return StringToProperty(Of T)(value, formatInfo)
      End If

    End Function

    Public Sub SetAttributeValue(attributeName As String, newValue As String)
      Dim lowerAttributeName = attributeName.ToLower()

      For Each attribute As XAttribute In _Element.Attributes()
        If (attribute.Name.LocalName.ToLower() = lowerAttributeName) Then
          attribute.Value = newValue
          Exit Sub
        End If
      Next

      _Element.SetAttributeValue(attributeName, newValue)
    End Sub

    Public Sub SetAttributeValue(Of T)(attributeName As String, newValue As T, Optional formatInfo As String = "")
      Me.SetAttributeValue(attributeName, StringFromProperty(Of T)(newValue, formatInfo))
    End Sub

#End Region

#Region " Type Conversion "

    Protected Enum EConversionDirection As Integer
      StringFromObject
      ObjectFromString
    End Enum

    Protected Shared Function StringFromProperty(Of T)(propertyValue As T, Optional formatInfo As String = "") As String
      Dim stringValue As String = String.Empty
      Dim untypedPropertyValue As Object = propertyValue
      If (StringFromPropertyCustom(Of T)(propertyValue, stringValue, formatInfo)) Then
        Return stringValue
      End If
      ConvertDefault(GetType(T), stringValue, untypedPropertyValue, EConversionDirection.StringFromObject, formatInfo)
      Return stringValue
    End Function

    Protected Shared Function StringToProperty(Of T)(stringValue As String, Optional formatInfo As String = "") As T
      Dim propertyValue As T = Nothing
      Dim untypedPropertyValue As Object = Nothing
      If (StringToPropertyCustom(Of T)(stringValue, propertyValue, formatInfo)) Then
        Return propertyValue
      End If
      ConvertDefault(GetType(T), stringValue, untypedPropertyValue, EConversionDirection.ObjectFromString, formatInfo)
      propertyValue = DirectCast(untypedPropertyValue, T)
      Return propertyValue
    End Function

    Private Shared Function StringFromPropertyCustom(Of T)(propertyValue As T, ByRef stringValue As String, Optional formatInfo As String = "") As Boolean
      Dim untypedPropertyValue As Object = propertyValue
      Dim buffer As String = Nothing
      ConvertCustom(GetType(T), buffer, untypedPropertyValue, EConversionDirection.StringFromObject, formatInfo)
      If (buffer IsNot Nothing) Then
        stringValue = buffer
        Return True
      Else
        Return False
      End If
    End Function

    Private Shared Function StringToPropertyCustom(Of T)(stringValue As String, ByRef propertyValue As T, Optional formatInfo As String = "") As Boolean
      Dim buffer As Object = Nothing
      ConvertCustom(GetType(T), stringValue, buffer, EConversionDirection.ObjectFromString, formatInfo)
      If (buffer IsNot Nothing) Then
        propertyValue = DirectCast(buffer, T)
        Return True
      Else
        Return False
      End If
    End Function

    Partial Private Shared Sub ConvertCustom(propertyType As Type, ByRef valueString As String, ByRef valueObject As Object, direction As EConversionDirection, Optional formatInfo As String = "")
    End Sub

#Region " Default Conversion Methods "

    Private Shared Sub ConvertDefault(propertyType As Type, ByRef valueString As String, ByRef valueObject As Object, direction As EConversionDirection, Optional formatInfo As String = "")

      If (propertyType.IsEnum) Then
        Dim typedObject As System.Enum = DirectCast(System.Enum.GetValues(propertyType).GetValue(0), System.Enum)
        If (valueObject IsNot Nothing) Then
          typedObject = DirectCast(valueObject, System.Enum)
        End If
        ConvertEnum(propertyType, valueString, typedObject, direction, formatInfo)
        valueObject = typedObject

      Else

        Select Case propertyType
          Case GetType(String)
            Dim typedObject As String = String.Empty
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, String)
            End If
            ConvertString(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Boolean)
            Dim typedObject As Boolean = False
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Boolean)
            End If
            ConvertBoolean(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Decimal)
            Dim typedObject As Decimal = 0D
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Decimal)
            End If
            ConvertDecimal(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Single)
            Dim typedObject As Single = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Single)
            End If
            ConvertSingle(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Double)
            Dim typedObject As Double = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Double)
            End If
            ConvertDouble(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Short)
            Dim typedObject As Short = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Short)
            End If
            ConvertShort(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Long)
            Dim typedObject As Long = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Long)
            End If
            ConvertLong(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Int16)
            Dim typedObject As Int16 = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Int16)
            End If
            ConvertInt16(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Int32)
            Dim typedObject As Int32 = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Int32)
            End If
            ConvertInt32(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Int64)
            Dim typedObject As Int64 = 0
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Int64)
            End If
            ConvertInt64(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(DateTime)
            Dim typedObject As DateTime = DateTime.MinValue
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, DateTime)
            End If
            ConvertDateTime(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Guid)
            Dim typedObject As Guid = Guid.Empty
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Guid)
            End If
            ConvertGuid(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case GetType(Net.IPAddress)
            Dim typedObject As Net.IPAddress = Net.IPAddress.None
            If (valueObject IsNot Nothing) Then
              typedObject = DirectCast(valueObject, Net.IPAddress)
            End If
            ConvertIpAddress(valueString, typedObject, direction, formatInfo)
            valueObject = typedObject

          Case Else
            ConvertObject(valueString, valueObject, direction, formatInfo)
        End Select

      End If

    End Sub

    Protected Shared Sub ConvertEnum(propertyEnumType As Type, ByRef valueString As String, ByRef valueObject As System.Enum, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = DirectCast(System.Enum.Parse(propertyEnumType, valueString), System.Enum)
        Case EConversionDirection.StringFromObject : valueString = System.Enum.GetName(propertyEnumType, valueObject)
      End Select
    End Sub

    Protected Shared Sub ConvertString(ByRef valueString As String, ByRef valueObject As String, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = valueString
        Case EConversionDirection.StringFromObject : valueString = valueObject
      End Select
    End Sub

    Protected Shared Sub ConvertInt16(ByRef valueString As String, ByRef valueObject As Int16, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Int16.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertInt32(ByRef valueString As String, ByRef valueObject As Int32, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Int32.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertSingle(ByRef valueString As String, ByRef valueObject As Single, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Single.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertShort(ByRef valueString As String, ByRef valueObject As Short, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Short.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertLong(ByRef valueString As String, ByRef valueObject As Long, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Long.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Overloads Shared Sub ConvertInt64(ByRef valueString As String, ByRef valueObject As Int64, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Int64.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertBoolean(ByRef valueString As String, ByRef valueObject As Boolean, direction As EConversionDirection, Optional formatInfo As String = "")
      If (formatInfo = "") Then
        formatInfo = "0|1"
      End If
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Boolean.Parse(valueString)
        Case EConversionDirection.StringFromObject
          If (valueObject = False) Then
            valueString = formatInfo.Split("|"c)(0)
          Else
            valueString = formatInfo.Split("|"c)(1)
          End If
      End Select
    End Sub

    Protected Shared Sub ConvertDateTime(ByRef valueString As String, ByRef valueObject As DateTime, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case formatInfo
        Case "" : formatInfo = "yyyy-MM-dd HH:mm:ss"
        Case "%TIME%" : formatInfo = "HH:mm:ss"
        Case "%DATE%" : formatInfo = "yyyy-MM-dd"
      End Select
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = DateTime.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString(formatInfo).Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertDecimal(ByRef valueString As String, ByRef valueObject As Decimal, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Decimal.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertDouble(ByRef valueString As String, ByRef valueObject As Double, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Double.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().Trim()
      End Select
    End Sub

    Protected Shared Sub ConvertGuid(ByRef valueString As String, ByRef valueObject As Guid, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Guid.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().ToLower()
      End Select
    End Sub

    Protected Shared Sub ConvertIpAddress(ByRef valueString As String, ByRef valueObject As Net.IPAddress, direction As EConversionDirection, Optional formatInfo As String = "")
      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = Net.IPAddress.Parse(valueString)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString().ToLower()
      End Select
    End Sub

    Protected Shared Sub ConvertObject(ByRef valueString As String, ByRef valueObject As Object, direction As EConversionDirection, Optional formatInfo As String = "")
      Dim parseMethod As Reflection.MethodInfo = Nothing
      Dim parseMethodParams As Object() = {Nothing}

      Try
        parseMethod = valueObject.GetType().GetMethod("Parse", {GetType(String)})
        parseMethodParams(0) = valueString

        If (Not parseMethod.IsStatic()) Then
          Throw New ApplicationException("The Parse Method is not Static")
        End If

      Catch ex As Exception
        Throw New NotImplementedException(String.Format("There is no method implemented to run conversions between '{0}' and 'String'. {1}", valueObject.GetType().Name, ex.Message), ex)
      End Try

      Select Case direction
        Case EConversionDirection.ObjectFromString : valueObject = parseMethod.Invoke(Nothing, parseMethodParams)
        Case EConversionDirection.StringFromObject : valueString = valueObject.ToString()
      End Select

    End Sub

#End Region

#End Region

  End Class

#End Region

#Region " DomItemList (nested Class) "

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Interface IDomItemList
    ReadOnly Property Parent As DomItem
  End Interface

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Interface IDomItemList(Of T As {DomItem, New})
    Inherits IDomItemList
    Inherits IList(Of T)

    Function AddNew(Of TInherited As {T, New})() As TInherited
    Function AddNew() As T
    Shadows Sub Add(ParamArray childs() As T)

  End Interface

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Class DomItemList(Of T As {DomItem, New})
    Implements IDomItemList(Of T)

#Region "..."

    Protected Shared ChildElementName As String = String.Empty

    Protected Property ParentItem As DomItem
    Protected Property MyElement As XElement
    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)> Protected Property BaseList As New List(Of T)

    Public Sub New(element As XElement, parent As DomItem)
      Me.ParentItem = parent
      Me.MyElement = element

      If (ChildElementName = String.Empty) Then
        Dim instance As New T
        ChildElementName = instance.GetElementName()
        instance = Nothing
      End If

      Me.ReadChildsFromElement(ChildElementName)
    End Sub

    Protected ReadOnly Property Element As XElement
      Get
        Return Me.MyElement
      End Get
    End Property

    Public ReadOnly Property Parent As DomItem Implements IDomItemList.Parent
      Get
        Return Me.ParentItem
      End Get
    End Property

#End Region

#Region " Read (init) "

    Public Sub ReadChildsFromElement(childElementName As String)
      childElementName = childElementName.ToLower()
      Me.Clear()
      For Each childElement As XElement In Me.Element.Elements
        If (childElementName = childElement.Name.LocalName.ToLower()) Then
          Dim newChild As New T
          newChild.SetElement(childElement, Me)
          Me.BaseList.Add(newChild)
        End If
      Next
    End Sub

#End Region

#Region " Add & Insert "

    Public Function AddNew(Of TInherited As {T, New})() As TInherited Implements IDomItemList(Of T).AddNew
      Dim newChild As New TInherited
      Me.Add(newChild)
      Return (newChild)
    End Function

    Public Function AddNew() As T Implements IDomItemList(Of T).AddNew
      Dim newChild As New T
      Me.Add(newChild)
      Return (newChild)
    End Function

    Public Sub Add(ParamArray childs() As T) Implements IDomItemList(Of T).Add
      Dim count As Integer = Me.Count()
      For i As Integer = 0 To childs.length - 1
        Me.Insert(count + i, childs(i))
      Next
    End Sub

    Public Sub Add(child As T) Implements ICollection(Of T).Add
      Me.Insert(Me.Count(), child)
    End Sub

    Public Sub Insert(index As Integer, child As T) Implements IList(Of T).Insert
      If (index > Me.Count) Then
        index = Me.Count
      End If
      If (Not Me.BaseList.Contains(child)) Then

        Dim handled As Boolean = False
        Dim cancel As Boolean = False
        Me.Parent.OnNewChildElementInserting(Of T)(index, child, handled, cancel)
        If (Not cancel) Then
          Me.BaseList.Insert(index, child)
        End If
        If (Not handled) Then
          InsertNode(child.Element, index)
          Me.Parent.OnNewChildElementInserted(Of T)(index, child, child.Element)
        End If

        child._ParentList = Me
      End If
    End Sub

    Protected Sub InsertNode(rawXmlElement As XElement, index As Integer)
      Select Case index
        Case Is < 0
          Me.Element.Add(rawXmlElement)
        Case 0
          Me.Element.AddFirst(rawXmlElement)
        Case Is > 0
          Me.Item(index - 1).Element.AddAfterSelf(rawXmlElement)
      End Select
    End Sub

#End Region

#Region " Remove "

    Public Sub Clear() Implements ICollection(Of T).Clear
      For Each child As T In Me.ToArray()
        Me.Remove(child)
      Next
    End Sub

    <EditorBrowsable(EditorBrowsableState.Never)>
    Public Sub RemoveAt(index As Integer) Implements IList(Of T).RemoveAt
      Me.Remove(Me.Item(index))
    End Sub

    Public Function Remove(child As T) As Boolean Implements ICollection(Of T).Remove
      If (Me.BaseList.Contains(child)) Then
        Dim handled As Boolean = False
        Dim cancel As Boolean = False
        Me.Parent.OnChildElementRemoving(Of T)(child, child.Element, handled, cancel)
        If (Not cancel) Then
          Me.BaseList.Remove(child)
        End If
        If (Not handled) Then
          RemoveNode(child.Element)
          Me.Parent.OnChildElementRemoved(Of T)(child)
        End If

        child._ParentList = Nothing

        Return True
      Else
        Return False
      End If
    End Function

    Protected Sub RemoveNode(rawXmlElement As XElement)
      rawXmlElement.Remove()
    End Sub

#End Region

#Region " Item Access "

    Public ReadOnly Property Count As Integer Implements ICollection(Of T).Count
      Get
        Return Me.BaseList.Count
      End Get
    End Property

    Public Function Contains(item As T) As Boolean Implements ICollection(Of T).Contains
      Return Me.BaseList.Contains(item)
    End Function

    Public Function GetEnumerator_Typed() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
      Return Me.BaseList.GetEnumerator()
    End Function

    Public Function GetEnumerator_Untyped() As IEnumerator Implements IEnumerable.GetEnumerator
      Return Me.BaseList.GetEnumerator()
    End Function

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Default Public Property Item(index As Integer) As T Implements IList(Of T).Item
      Get
        Return Me.BaseList.Item(index)
      End Get
      Set(value As T)
        Me.BaseList.Item(index) = value
      End Set
    End Property

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Function IndexOf(item As T) As Integer Implements IList(Of T).IndexOf
      Return Me.BaseList.IndexOf(item)
    End Function

#End Region

#Region " System "

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of T).IsReadOnly
      Get
        Return False
      End Get
    End Property

    <EditorBrowsable(EditorBrowsableState.Never)>
    Public Sub CopyTo(array() As T, arrayIndex As Integer) Implements ICollection(Of T).CopyTo
      Me.BaseList.CopyTo(array, arrayIndex)
    End Sub

#End Region

  End Class

#End Region

  End Class

End Namespace
<#
'#######################################################################################################################
'# NODE CLASSES                                                                                                        #
'#######################################################################################################################

For Each entity As EntityType In ItemCollection.GetItems(Of EntityType)().OrderBy(Function(e) e.Name)
Dim entitySet As EntitySet = Nothing
Dim baseEntity As EntityType = entity.baseType
Dim isInherited As Boolean = (baseEntity IsNot Nothing)
  If (Not isInherited) Then
    baseEntity = entity
  End If

  For Each existingEntitySet As EntitySet In container.BaseEntitySets.OfType(Of EntitySet)()
    If (isInherited)Then
      If (existingEntitySet.ElementType.Name = baseEntity.Name) Then
        entitySet = existingEntitySet
        Exit For
      End If
    Else
      If (existingEntitySet.ElementType.Name = entity.Name) Then
        entitySet = existingEntitySet
        Exit For
      End If
    End If
  Next

    fileManager.StartNewFile(Code.Escape(fileTypeName) & "." & Code.Escape(entity.Name) & ".Generated.vb")

Dim primitiveBaseProperties As IEnumerable(Of EdmProperty) = baseEntity.Properties.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is baseEntity)
Dim primitiveProperties As IEnumerable(Of EdmProperty) = entity.Properties.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is entity)
Dim navigationProperties As IEnumerable(Of NavigationProperty) = entity.NavigationProperties.Where(Function(np) np.DeclaringType Is entity)
Dim DebuggerDisplayString As String

  DebuggerDisplayString = entity.Name 
  If (entity.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(entity.Documentation.LongDescription))
    DebuggerDisplayString = string.Format("{0} ({1})", DebuggerDisplayString, entity.Documentation.LongDescription)
  End If

Dim keyPropertyParams As System.Text.StringBuilder
Dim keyPropertyNames As System.Text.StringBuilder
Dim keyPropertyExpression As System.Text.StringBuilder
Dim keyPropertyMapping As System.Text.StringBuilder
Dim allPropertyParams As System.Text.StringBuilder
Dim allPropertyMapping As System.Text.StringBuilder

  allPropertyParams = new System.Text.StringBuilder()
  allPropertyMapping = new System.Text.StringBuilder()
    If primitiveProperties.Any() Then
        For Each edmProperty As EdmProperty In primitiveProperties
      If (allPropertyParams.Length > 0) then				
        allPropertyParams.Append(", ")
        allPropertyMapping.AppendLine()
        allPropertyMapping.Append("      ")
        End If
      allPropertyParams.Append(FirstToLower(Code.Escape(edmProperty.Name)))
      allPropertyParams.Append(" As ")
      allPropertyParams.Append(Code.Escape(edmProperty.TypeUsage))
      allPropertyMapping.Append("Me.")
      allPropertyMapping.Append(Code.Escape(edmProperty.Name))
      allPropertyMapping.Append(" = ")
      allPropertyMapping.Append(FirstToLower(Code.Escape(edmProperty.Name)))
    Next
  End If
  If (isInherited) Then
    If primitiveBaseProperties.Any() Then
      For Each edmProperty As EdmProperty In primitiveBaseProperties
        If (allPropertyParams.Length > 0) then				
          allPropertyParams.Append(", ")
          allPropertyMapping.AppendLine()
          allPropertyMapping.Append("      ")
          End If
        allPropertyParams.Append(FirstToLower(Code.Escape(edmProperty.Name)))
        allPropertyParams.Append(" As ")
        allPropertyParams.Append(Code.Escape(edmProperty.TypeUsage))
        allPropertyMapping.Append("Me.")
        allPropertyMapping.Append(Code.Escape(edmProperty.Name))
        allPropertyMapping.Append(" = ")
        allPropertyMapping.Append(FirstToLower(Code.Escape(edmProperty.Name)))
      Next
    End If
  End If

  keyPropertyParams = new System.Text.StringBuilder()
  keyPropertyMapping = new System.Text.StringBuilder()
  keyPropertyNames = new System.Text.StringBuilder()
  keyPropertyExpression = new System.Text.StringBuilder()
    For Each keyProperty As EdmMember In baseEntity.KeyMembers
    If (keyPropertyParams.Length > 0) then
      keyPropertyParams.Append(", ")
      keyPropertyNames.Append(", ")
      keyPropertyExpression.Append(" And ")
      keyPropertyMapping.AppendLine()
      keyPropertyMapping.Append("      ")
    End If
    keyPropertyParams.Append(FirstToLower(Code.Escape(keyProperty.Name)))
    keyPropertyParams.Append(" As ")
    keyPropertyParams.Append(Code.Escape(keyProperty.TypeUsage))
    keyPropertyNames.Append(FirstToLower(Code.Escape(keyProperty.Name)))
    keyPropertyMapping.Append("Me.")
    keyPropertyMapping.Append(Code.Escape(keyProperty.Name))
    keyPropertyMapping.Append(" = ")
    keyPropertyMapping.Append(FirstToLower(Code.Escape(keyProperty.Name)))
    keyPropertyExpression.Append("existingItem.")
    keyPropertyExpression.Append(Code.Escape(keyProperty.Name))
    keyPropertyExpression.Append(" = ")
    keyPropertyExpression.Append(FirstToLower(Code.Escape(keyProperty.Name)))
  Next

#>

Namespace <#= Code.Escape(namespaceName) #>

<#

    If (entity.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(entity.Documentation.Summary))
#>	''' <summary> <#= entity.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "''' ") #> </summary>  
<#
    End If
#>  <EditorBrowsable(EditorBrowsableState.Advanced), DebuggerDisplay("<#= DebuggerDisplayString #>")>
  Partial Public Class <#= Code.Escape(entity.Name) #>
#Region "..."
<#
  If (isInherited) Then
#>		Inherits <#= Code.Escape(baseEntity.Name) #>
<#
  Else
#>		Inherits DomItem
<#
  End If
#>

  Partial Private Shared Sub CustomizeNewItem(newItem as <#= Code.Escape(entity.Name) #>)
    End Sub
<#
  If(Not allPropertyParams.ToString() = keyPropertyParams.ToString())Then
#>

    ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> and sets ALL properties.</summary>
    Public Sub New(<#= allPropertyParams.ToString() #>)
      Me.New()
        <#= allPropertyMapping.ToString() #>
    End Sub
<#
  End If
#>

    ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> and sets ONLY the properties which are flagged as KEY.</summary>
    Public Sub New(<#= keyPropertyParams.ToString() #>)
      Me.New()
        <#= keyPropertyMapping.ToString() #>
    End Sub

    ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> using a lambda-expression to custiomize.</summary>
    Public Sub New(customizing As Action(Of <#= Code.Escape(entity.Name) #>))
      Me.New()
      customizing.Invoke(Me)
    End Sub

    ''' <summary>Initializes a new Instance of <#= Code.Escape(entity.Name) #> without setting any properties.</summary>
    Public Sub New()
      MyBase.New(GetType(<#= Code.Escape(entity.Name) #>))
      CustomizeNewItem(Me)
    End Sub

  ''' <summary>This constructor is for clases which inheriting from <#= Code.Escape(entity.Name) #> (this classes will need to change the xml Element name in this way)</summary>
    Protected Sub New(inheritedType as Type)
      MyBase.New(inheritedType)
      CustomizeNewItem(Me)
    End Sub

<#
      If (Code.Escape(rootObject) = Code.Escape(entity.Name)) Then
#>
        ''' <summary>This is a special constructor which is only needed for ROOT-Objects of a XML file.</summary>
    Private Sub New(parent As <#= Code.Escape(fileClassName) #>)
      Me.New()
      _Parent = parent
    End Sub

    ''' <summary>This is a special function which is only needed for ROOT-Objects of a XML file.</summary>
    Friend Shared Function FromDocumentRoot(parent As <#= Code.Escape(fileClassName) #>, document As XDocument) As <#= Code.Escape(rootObject) #>
      Dim new<#= Code.Escape(rootObject) #> As <#= Code.Escape(rootObject) #>
      new<#= Code.Escape(rootObject) #> = New <#= Code.Escape(rootObject) #>(parent)
      new<#= Code.Escape(rootObject) #>.SetElement(document.Root, Nothing)
      Return new<#= Code.Escape(rootObject) #>
    End Function

  ''' <summary>This is a special function which is only needed for ROOT-Objects of a XML file.</summary>
    Friend Shared Function CreateAndAddToDocumentRoot(parent As <#= Code.Escape(fileClassName) #>, document As XDocument) As <#= Code.Escape(rootObject) #>
      Dim new<#= Code.Escape(rootObject) #> As <#= Code.Escape(rootObject) #>
      new<#= Code.Escape(rootObject) #> = New <#= Code.Escape(rootObject) #>(parent)
      document.Add(new<#= Code.Escape(rootObject) #>.Element)
      Return new<#= Code.Escape(rootObject) #>
    End Function

    Private _Parent As <#= Code.Escape(fileClassName) #> = Nothing
    Public ReadOnly Property Parent As <#= Code.Escape(fileClassName) #>
      Get
        Return _Parent
      End Get
    End Property

<#		  	  
    End If
#>
#End Region
<#
      If primitiveProperties.Any() Then
        For Each edmProperty As EdmProperty In primitiveProperties
#>

#Region " <#= Code.Escape(edmProperty.Name) #> "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>AttributeName As String = "<#= Code.Escape(edmProperty.Name) #>"
    <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>Format As String = String.Empty
  <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>Getter As PropertyGetter(Of <#= Code.Escape(edmProperty.TypeUsage) #>) = AddressOf DefaultPropertyGetter(Of <#= Code.Escape(edmProperty.TypeUsage) #>)
    <DebuggerBrowsable(DebuggerBrowsableState.Never)> Protected Property <#= (Code.Escape(edmProperty.Name)) #>Setter As PropertySetter(Of <#= Code.Escape(edmProperty.TypeUsage) #>) = AddressOf DefaultPropertySetter(Of <#= Code.Escape(edmProperty.TypeUsage) #>)

<#	   
    If (edmProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(edmProperty.Documentation.Summary))
#>		''' <summary> <#= edmProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "  ''' ") #> </summary>  
<#
    End If
#>
    Public Property <#= Code.Escape(edmProperty.Name) #> As <#= Code.Escape(edmProperty.TypeUsage) #>
      Get
        Return <#= (Code.Escape(edmProperty.Name)) #>Getter.Invoke(<#= (Code.Escape(edmProperty.Name)) #>AttributeName, <#= DefaultValueForType(Code.Escape(edmProperty.TypeUsage)) #>, <#= (Code.Escape(edmProperty.Name)) #>Format)
      End Get
      Set(value As <#= Code.Escape(edmProperty.TypeUsage) #>)
        <#= (Code.Escape(edmProperty.Name)) #>Setter.Invoke(<#= (Code.Escape(edmProperty.Name)) #>AttributeName, value, <#= (Code.Escape(edmProperty.Name)) #>Format)
      End Set
    End Property

#End Region
<#

        Next
    End If

   If navigationProperties.Any() Then
        For Each navigationProperty As NavigationProperty In navigationProperties

        If (navigationProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(navigationProperty.Documentation.Summary))
#>		''' <summary> <#= navigationProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "  ''' ") #> </summary>  
<#
      End If

    Dim endEntity = navigationProperty.ToEndMember.GetEntityType()
    Dim endType = code.Escape(endEntity)

        If(navigationProperty.ToEndMember.RelationshipMultiplicity = RelationshipMultiplicity.Many)
    'CHILD COLLECTION:


    Dim endTypeProperties As IEnumerable(Of EdmProperty) = endEntity.Properties '.Where(Function(p) TypeOf p.TypeUsage.EdmType Is PrimitiveType AndAlso p.DeclaringType Is endEntity)

    Dim simpleProperties As New System.Text.StringBuilder
    Dim simplePropertySummaries As New System.Text.StringBuilder
        If endTypeProperties.Any() Then
            For Each endTypeProperty As EdmProperty In endTypeProperties
            If(TypeOf endTypeProperty.TypeUsage.EdmType Is PrimitiveType)
              If (simpleProperties.Length > 0) Then
                simpleProperties.Append (", ")
              End If
              simpleProperties.Append(FirstToLower(code.Escape(endTypeProperty.Name)))
              simpleProperties.Append(" As ")
              simpleProperties.Append(code.Escape(endTypeProperty.TypeUsage))

              If (endTypeProperty.Documentation IsNot Nothing AndAlso Not String.IsNullOrEmpty(endTypeProperty.Documentation.Summary))
                simplePropertySummaries.AppendLine()
                simplePropertySummaries.Append("		''' <param name=""")
                simplePropertySummaries.Append(FirstToLower(code.Escape(endTypeProperty.Name)))
                simplePropertySummaries.Append(""">")
                simplePropertySummaries.Append(endTypeProperty.Documentation.Summary.Replace(Environment.NewLine ,Environment.NewLine & "		''' "))
                simplePropertySummaries.Append("</param>")
              End If
            End If
            Next
        End If

#>

#Region " <#= Code.Escape(navigationProperty) #> (Child Collection) "

    Public ReadOnly Property <#= Code.Escape(navigationProperty) #> As IDomItemList(Of <#= Code.Escape(endType) #>)
      Get
        Return MyBase.ChildCollection(Of <#= Code.Escape(endType) #>)()
      End Get
    End Property

<#
      Else
#>

#Region " <#= Code.Escape(endType) #> (Parent) "

    Public ReadOnly Property <#= Code.Escape(navigationProperty) #> As <#= Code.Escape(endType) #>
      Get
        Return MyBase.GetParent(Of <#= Code.Escape(endType) #>)()
      End Get
    End Property

<#	
      End If
#>
#End Region
<#		  	  
        Next
    End If
#>

  End Class

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Module <#= Code.Escape(entity.Name) #>Extensions

#Region " <#= Code.Escape(entity.Name) #> List "

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function Find(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
    Return (From existingItem As <#= Code.Escape(entity.Name) #>
            In <#= FirstToLower(Code.Escape(entity.Name)) #>List
            Where <#= keyPropertyExpression.ToString() #>
           ).FirstOrDefault()
    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function AddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
    Dim new<#= Code.Escape(entity.Name) #> As New <#= Code.Escape(entity.Name) #>(<#= keyPropertyNames.ToString() #>)
    <#= FirstToLower(Code.Escape(entity.Name)) #>List.Add(new<#= Code.Escape(entity.Name) #>)
    Return new<#= Code.Escape(entity.Name) #>
    End Function

    <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    Public Function FindOrAddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List As DomItem.IDomItemList(Of <#= Code.Escape(entity.Name) #>), <#= keyPropertyParams.ToString() #>) As <#= Code.Escape(entity.Name) #>
        Dim found<#= Code.Escape(entity.Name) #> As <#= Code.Escape(entity.Name) #>
        found<#= Code.Escape(entity.Name) #> = Find(<#= FirstToLower(Code.Escape(entity.Name)) #>List, <#= keyPropertyNames.ToString() #>)

        If (found<#= Code.Escape(entity.Name) #> IsNot Nothing)
      Return found<#= Code.Escape(entity.Name) #>
    Else
      Return AddNew(<#= FirstToLower(Code.Escape(entity.Name)) #>List, <#= keyPropertyNames.ToString() #>)
    End If

    End Function

#End Region

  End Module

End Namespace
<#

Next

If Not VerifyTypesAreCaseInsensitiveUnique(ItemCollection) Then
    Return ""
End If

fileManager.Process()
#>
<#+
'#######################################################################################################################
'# HELPER CODE                                                                                                         #
'#######################################################################################################################

Private Sub WriteHeader(ByVal fileManager As EntityFrameworkTemplateFileManager)
  fileManager.StartHeader()
#>
  '==============================================================================
  ' Auto-generated code
  '==============================================================================
  ' This code was generated from the Entities.tt template.
  ' 
  ' Manual changes to this file may cause unexpected behavior in your application.
  ' Manual changes to this file will be overwritten if the code is regenerated.
  '==============================================================================

Imports System.Runtime.CompilerServices
Imports System.ComponentModel
Imports System.Collections.Generic
Imports System.Collections
Imports System.Diagnostics
Imports System.Xml.Linq
Imports System.Xml
Imports System.Linq
Imports System.Text
Imports System.IO
Imports System
<#+
  FileManager.EndBlock()
End Sub

Private Sub BeginNamespace(ByVal namespaceName As String, ByVal code As CodeGenerationTools)
  Dim region As CodeRegion = New CodeRegion(Me)
  If Not String.IsNullOrEmpty(namespaceName) Then
#>
Namespace <#=code.EscapeNamespace(namespaceName)#>

<#+
        PushIndent(CodeRegion.GetIndent(1))
    End If
End Sub

  Private Sub EndNamespace(namespaceName As String)
    If Not String.IsNullOrEmpty(namespaceName) Then
      PopIndent()
#>

End Namespace
<#+
    End If
End Sub

Private Sub WriteProperty(code As CodeGenerationTools, edmProperty As EdmProperty)
  WriteProperty(code, edmProperty, code.StringBefore(" = ", code.CreateLiteral(edmProperty.DefaultValue)))
End Sub

Private Sub WriteComplexProperty(code As CodeGenerationTools, complexProperty As EdmProperty)
  WriteProperty(code, complexProperty, " = New " & code.Escape(complexProperty.TypeUsage))
End Sub

Private Sub WriteProperty(code As CodeGenerationTools, edmProperty As EdmProperty, defaultValue As String)
  WriteProperty(Accessibility.ForProperty(edmProperty), _
                code.Escape(edmProperty.TypeUsage), _
                code.Escape(edmProperty), _
                code.SpaceAfter(Accessibility.ForGetter(edmProperty)), _
                code.SpaceAfter(Accessibility.ForSetter(edmProperty)), _
                defaultValue)
End Sub

Private Sub WriteNavigationProperty(code As CodeGenerationTools, navigationProperty As NavigationProperty)
  Dim endType = code.Escape(navigationProperty.ToEndMember.GetEntityType())
  Dim defaultValue = ""
  Dim propertyType = code.Escape(endType)

  If (navigationProperty.ToEndMember.RelationshipMultiplicity = RelationshipMultiplicity.Many) Then
    defaultValue = " = New " & propertyType & "Collection"
    propertyType = propertyType & "Collection"
  End If

  WriteProperty(PropertyAccessibilityAndVirtual(navigationProperty), _
                propertyType, _
                code.Escape(navigationProperty), _
                code.SpaceAfter(Accessibility.ForGetter(navigationProperty)), _
                code.SpaceAfter(Accessibility.ForSetter(navigationProperty)), _
                defaultValue)
End Sub

Private Sub WriteProperty(accessibility As String, type As String, name As String, getterAccessibility As String, setterAccessibility As String, defaultValue As String)
  If ([String].IsNullOrEmpty(getterAccessibility) AndAlso [String].IsNullOrEmpty(setterAccessibility)) Then
#>
    <#=accessibility#> Property <#=name#> As <#=type#><#=defaultValue#>
<#+
    Else
#>

    Private _<#=name#> As <#=type#><#=defaultValue#>
    <#=accessibility#> Property <#=name#> As <#=type#>
        <#=getterAccessibility#>Get
            Return _<#=name#>
        End Get
        <#=setterAccessibility#>Set(ByVal value As <#=type#>)
            _<#=name#> = value
        End Set
    End Property
<#+
  End If
End Sub

Private Function PropertyAccessibilityAndVirtual(ByVal member As EdmMember) As String
  Dim propertyAccess As String = Accessibility.ForProperty(member)
  Dim setAccess As String = Accessibility.ForSetter(member)
  Dim getAccess As String = Accessibility.ForGetter(member)
  If propertyAccess <> "Private" AndAlso setAccess <> "Private" AndAlso getAccess <> "Private" Then
    Return propertyAccess & " Overridable"
  End If

  Return propertyAccess
End Function

Private Function VerifyTypesAreCaseInsensitiveUnique(ByVal itemCollection As EdmItemCollection) As Boolean
  Dim alreadySeen As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

  For Each type As StructuralType In itemCollection.GetItems(Of StructuralType)()
    If Not (TypeOf type Is EntityType OrElse TypeOf type Is ComplexType) Then
      Continue For
    End If

    If alreadySeen.ContainsKey(type.FullName) Then
      [Error](String.Format("This template does not support types that differ only by case, the types {0} are not supported", type.FullName))
      Return False
    Else
      alreadySeen.Add(type.FullName, True)
    End If
  Next

  Return True
End Function

Private Function FirstToLower(input As String) As String
  If (input.Length > 1) Then
    Return input.Substring(0, 1).ToLower() & input.Substring(1, input.Length - 1)
  Else
    Return input.ToLower()
  End If
End Function

Private Function DefaultValueForType(typeName As String) As String
  Select Case typeName.Replace("System.", "").ToLower()
    Case "string" : Return "String.Empty"
    Case "datetime" : Return "DateTime.MinValue"
    Case "boolean" : Return "False"
    Case "integer", "int16", "int32", "int64", "decimal", "double", "short" : Return "0"
    Case "guid" : Return "System.Guid.Empty "

    Case Else : Return "Nothing"
  End Select
End Function
#>
