# TextFieldControl
Контрол "Текстовое поле"

1. Убраны явные дефолтные значения
- Удалены enum'ы FieldVariant и FieldSize
- Убраны значения по умолчанию из StyledProperty регистрации
- Свойства: VariantProperty, SizeProperty, IsErrorProperty, MinRowsProperty, MaxRowsProperty, IsFlexibleProperty

Осталось значение по умолчанию у Label.

2. Магические числа вынесены в ресурсы
```xml
 <!-- ===== SCROLLBAR РАЗМЕРЫ ===== -->
 <x:Double x:Key="ScrollBarWidth">16</x:Double>
 <x:Double x:Key="ScrollBarThumbWidth">8</x:Double>
 <x:Double x:Key="ScrollBarThumbMinHeight">20</x:Double>
 <!-- ===== SCROLL КОНСТАНТЫ ===== -->
 <x:Double x:Key="ScrollWheelSpeed">20</x:Double>
 <!-- ===== ВЫСОТЫ ДЛЯ MULTILINE ===== -->
 <x:Double x:Key="Row4Height">86</x:Double>
 <x:Double x:Key="Row6Height">128</x:Double>
 <x:Double x:Key="Row8Height">170</x:Double>
 <x:Double x:Key="MaxRow4Height">128</x:Double>
 <x:Double x:Key="MaxRow6Height">170</x:Double>
 <x:Double x:Key="MaxRow8Height">202</x:Double>
 ```
3. Новая система классов:
- Размеры: Small, Medium
- Варианты: Standard, Filled, Outlined
- Multiline режимы: Flexible, MinRow, MaxRow
- Высоты: Row4, Row6, Row8
- Состояния: Error

4. Убраны излишние проверки на null

5. Инициализация в OnApplyTemplate

Удаленные свойства:
- FieldVariant Variant
- FieldSize Size
- bool IsError
- int? MinRows
- int? MaxRows
- bool IsFlexible

Изменился способ использования

Пример:
```csharp
<controls:TextField Classes="Standard Small" Label="Small Field" Text="Sample Text"/>
<controls:TextField Classes="Filled Medium" Label="Medium Field" Text="Sample Text"/>
<controls:TextField Classes="Outlined Medium Error" Label="Error" Text="Invalid Text"/>
<controls:TextField Classes="Standard Medium MinRow Row4" Label="Min 4 Rows"/>
<controls:TextField Classes="Filled Medium MaxRow Row6" Label="Max 6 Rows"/>
```
