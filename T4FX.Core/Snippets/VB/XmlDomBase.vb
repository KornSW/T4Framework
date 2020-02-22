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

Namespace __NamespaceName__

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
          Case EConversionDirection.ObjectFromString
            valueObject = (valueString = formatInfo.Split("|"c)(1))
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
