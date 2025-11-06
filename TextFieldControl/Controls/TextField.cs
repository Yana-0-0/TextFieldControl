using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace TextFieldControl.Controls;

/// <summary>
/// Кастомный текстовый контрол с поддержкой различных вариантов оформления и многострочного ввода
/// </summary>
public class TextField : TextBox
{
    #region Enums

    /// <summary>
    /// Варианты оформления текстового поля
    /// </summary>
    public enum FieldVariant { Standard, Filled, Outlined }

    /// <summary>
    /// Размеры текстового поля
    /// </summary>
    public enum FieldSize { Medium, Small }

    #endregion

    #region Styled Properties

    /// <summary>
    /// Определяет вариант оформления текстового поля
    /// </summary>
    public static readonly StyledProperty<FieldVariant> VariantProperty =
        AvaloniaProperty.Register<TextField, FieldVariant>(nameof(Variant), FieldVariant.Standard);

    /// <summary>
    /// Определяет размер текстового поля
    /// </summary>
    public static readonly StyledProperty<FieldSize> SizeProperty =
        AvaloniaProperty.Register<TextField, FieldSize>(nameof(Size), FieldSize.Medium);

    /// <summary>
    /// Текст метки (лейбла) поля
    /// </summary>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<TextField, string>(nameof(Label), "Label");

    /// <summary>
    /// Указывает, находится ли поле в состоянии ошибки
    /// </summary>
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(IsError), false);

    /// <summary>
    /// Включение многострочного режима
    /// </summary>
    public static readonly StyledProperty<bool> IsMultiLineProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(IsMultiLine), false);

    /// <summary>
    /// Минимальное количество строк в многострочном режиме
    /// </summary>
    public static readonly StyledProperty<int?> MinRowsProperty =
        AvaloniaProperty.Register<TextField, int?>(nameof(MinRows));

    /// <summary>
    /// Максимальное количество строк в многострочном режиме
    /// </summary>
    public static readonly StyledProperty<int?> MaxRowsProperty =
        AvaloniaProperty.Register<TextField, int?>(nameof(MaxRows));

    /// <summary>
    /// Указывает, что поле имеет минимальное количество строк
    /// </summary>
    public static readonly StyledProperty<bool> HasMinRowsProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(HasMinRows));

    /// <summary>
    /// Указывает, что поле имеет максимальное количество строк
    /// </summary>
    public static readonly StyledProperty<bool> HasMaxRowsProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(HasMaxRows));

    /// <summary>
    /// Указывает, что поле имеет гибкую высоту
    /// </summary>
    public static readonly StyledProperty<bool> IsFlexibleProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(IsFlexible));

    #endregion

    #region Private Fields

    private ScrollViewer? _scrollViewer;
    private TextPresenter? _textPresenter;
    private ScrollBar? _customScrollBar;
    private Border? _textContainer;
    private bool _isScrollingFromCustomBar = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Инициализирует новый экземпляр класса TextField
    /// </summary>
    public TextField()
    {
        UpdateMultiLineStates();
        UpdateMultiLineHeight();
        UpdateTextFieldBehavior();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Вариант оформления текстового поля
    /// </summary>
    public FieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Размер текстового поля
    /// </summary>
    public FieldSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Текст метки (лейбла) поля
    /// </summary>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Указывает, находится ли поле в состоянии ошибки
    /// </summary>
    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    /// <summary>
    /// Включение многострочного режима
    /// </summary>
    public bool IsMultiLine
    {
        get => GetValue(IsMultiLineProperty);
        set => SetValue(IsMultiLineProperty, value);
    }

    /// <summary>
    /// Минимальное количество строк в многострочном режиме
    /// </summary>
    public int? MinRows
    {
        get => GetValue(MinRowsProperty);
        set => SetValue(MinRowsProperty, value);
    }

    /// <summary>
    /// Максимальное количество строк в многострочном режиме
    /// </summary>
    public int? MaxRows
    {
        get => GetValue(MaxRowsProperty);
        set => SetValue(MaxRowsProperty, value);
    }

    /// <summary>
    /// Указывает, что поле имеет минимальное количество строк
    /// </summary>
    public bool HasMinRows
    {
        get => GetValue(HasMinRowsProperty);
        private set => SetValue(HasMinRowsProperty, value);
    }

    /// <summary>
    /// Указывает, что поле имеет максимальное количество строк
    /// </summary>
    public bool HasMaxRows
    {
        get => GetValue(HasMaxRowsProperty);
        private set => SetValue(HasMaxRowsProperty, value);
    }

    /// <summary>
    /// Указывает, что поле имеет гибкую высоту
    /// </summary>
    public bool IsFlexible
    {
        get => GetValue(IsFlexibleProperty);
        private set => SetValue(IsFlexibleProperty, value);
    }

    #endregion

    #region Template Methods

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        InitializeTemplateElements(e);
        SetupScrollBar();
        UpdateMultiLineHeight();
        UpdateTextFieldBehavior();

        Dispatcher.UIThread.Post(UpdateCustomScrollBar, DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        HandlePropertyChanges(change);
    }

    #endregion

    #region MultiLine Logic

    /// <summary>
    /// Обновляет состояния многострочного режима
    /// </summary>
    private void UpdateMultiLineStates()
    {
        if (!IsMultiLine)
        {
            HasMinRows = false;
            HasMaxRows = false;
            IsFlexible = false;
            return;
        }

        HasMinRows = MinRows.HasValue && !MaxRows.HasValue;
        HasMaxRows = MaxRows.HasValue && !MinRows.HasValue;
        IsFlexible = !MinRows.HasValue && !MaxRows.HasValue;
    }

    /// <summary>
    /// Обновляет поведение текстового поля в соответствии с настройками
    /// </summary>
    private void UpdateTextFieldBehavior()
    {
        AcceptsReturn = IsMultiLine;
    }

    /// <summary>
    /// Обновляет высоту поля в многострочном режиме
    /// </summary>
    private void UpdateMultiLineHeight()
    {
        UpdateMultiLineStates();

        if (!IsMultiLine)
        {
            ClearValue(MinHeightProperty);
            ClearValue(MaxHeightProperty);
            ClearValue(HeightProperty);
            return;
        }

        double lineHeight = FontSize * 1.55;
        double verticalPadding = GetVerticalPadding();

        if (HasMaxRows)
        {
            var maxHeight = (lineHeight * MaxRows!.Value) + verticalPadding;
            SetCurrentValue(MinHeightProperty, 0);
            SetCurrentValue(MaxHeightProperty, maxHeight);
            SetCurrentValue(HeightProperty, double.NaN);
        }
        else if (HasMinRows)
        {
            SetCurrentValue(MinHeightProperty, (lineHeight * MinRows!.Value) - 10);
            ClearValue(MaxHeightProperty);
            SetCurrentValue(HeightProperty, double.NaN);
        }
        else if (IsFlexible)
        {
            SetCurrentValue(MinHeightProperty, lineHeight);
            ClearValue(MaxHeightProperty);
            SetCurrentValue(HeightProperty, double.NaN);
        }
    }

    /// <summary>
    /// Возвращает вертикальные отступы в зависимости от варианта оформления
    /// </summary>
    private double GetVerticalPadding() => Variant switch
    {
        FieldVariant.Standard => 30,
        FieldVariant.Filled => 30,
        FieldVariant.Outlined => 22,
        _ => 30
    };

    #endregion

    #region ScrollBar Implementation

    /// <summary>
    /// Настраивает кастомный скроллбар
    /// </summary>
    private void SetupScrollBar()
    {
        if (_scrollViewer == null || _customScrollBar == null || _textPresenter == null) return;

        _customScrollBar.Opacity = 0;
        _customScrollBar.IsHitTestVisible = false;
        _textPresenter.RenderTransform = new TranslateTransform();

        _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;

        Dispatcher.UIThread.Post(UpdateCustomScrollBar, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Обновляет состояние и положение кастомного скроллбара
    /// </summary>
    private void UpdateCustomScrollBar()
    {
        if (_customScrollBar == null || _scrollViewer == null || _textPresenter == null)
            return;

        if (!IsMultiLine || !HasMaxRows)
        {
            ResetScrollBar();
            return;
        }

        var textLayout = _textPresenter.TextLayout;
        if (textLayout == null) return;

        var textHeight = textLayout.Height;
        var viewportHeight = _scrollViewer.Bounds.Height;
        bool shouldShowScroll = textHeight > viewportHeight && viewportHeight > 0;

        if (shouldShowScroll)
        {
            SetupScrollBarForScrolling(textHeight, viewportHeight);
        }
        else
        {
            ResetScrollBar();
        }
    }

    /// <summary>
    /// Настраивает скроллбар когда требуется прокрутка
    /// </summary>
    private void SetupScrollBarForScrolling(double textHeight, double viewportHeight)
    {
        if (_customScrollBar == null || _textPresenter == null) return;

        _customScrollBar.Opacity = 1;
        _customScrollBar.IsHitTestVisible = true;

        var scrollableHeight = Math.Max(0, textHeight - viewportHeight);

        _customScrollBar.Minimum = 0;
        _customScrollBar.Maximum = scrollableHeight;
        _customScrollBar.ViewportSize = viewportHeight;
        _customScrollBar.LargeChange = viewportHeight;
        _customScrollBar.SmallChange = 16;

        if (!_isScrollingFromCustomBar)
        {
            UpdateScrollBarValue(scrollableHeight);
        }
    }

    /// <summary>
    /// Сбрасывает состояние скроллбара
    /// </summary>
    private void ResetScrollBar()
    {
        if (_customScrollBar == null || _textPresenter == null) return;

        _customScrollBar.Opacity = 0;
        _customScrollBar.IsHitTestVisible = false;
        _customScrollBar.Value = 0;

        if (_textPresenter.RenderTransform is TranslateTransform transform)
        {
            transform.Y = 0;
        }
    }

    /// <summary>
    /// Обновляет значение скроллбара на основе текущей позиции текста
    /// </summary>
    private void UpdateScrollBarValue(double scrollableHeight)
    {
        if (_customScrollBar == null || _textPresenter == null) return;

        if (_textPresenter.RenderTransform is TranslateTransform transform)
        {
            double currentTransformY = transform.Y;
            double currentScrollOffset = Math.Max(0, -currentTransformY);
            currentScrollOffset = Math.Min(scrollableHeight, currentScrollOffset);

            double scrollBarValue = scrollableHeight - currentScrollOffset;
            _customScrollBar.Value = Math.Max(0, Math.Min(scrollableHeight, scrollBarValue));
        }
        else
        {
            _customScrollBar.Value = 0;
            _textPresenter.RenderTransform = new TranslateTransform(0, -scrollableHeight);
        }
    }

    #endregion

    #region Text Container Management

    /// <summary>
    /// Обновляет отступы текстового контейнера в зависимости от варианта и состояния
    /// </summary>
    private void UpdateTextContainerPadding()
    {
        if (_textContainer == null) return;

        if (!IsMultiLine)
        {
            _textContainer.Padding = new Thickness(0);
            return;
        }

        // Для small размера используем нулевые отступы, чтобы избежать двойного отступа
        if (Size == FieldSize.Small)
        {
            var padding = Variant switch
            {
                FieldVariant.Standard => new Thickness(0, 0, 0, 0),
                FieldVariant.Filled => new Thickness(0, 0, 0, 0),
                FieldVariant.Outlined => new Thickness(0, 0, 0, 0),
                _ => new Thickness(0, 0, 0, 0)
            };
            _textContainer.Padding = padding;
        }
        else
        {
            // Для medium размера оставляем оригинальные отступы
            var padding = Variant switch
            {
                FieldVariant.Standard => new Thickness(0, 22, 0, 0),
                FieldVariant.Filled => new Thickness(0, 22, 0, 0),
                FieldVariant.Outlined => new Thickness(0, 14, 0, 0),
                _ => new Thickness(0, 22, 0, 0)
            };
            _textContainer.Padding = padding;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Обработчик изменения значения скроллбара
    /// </summary>
    private void OnCustomScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_textPresenter != null && _customScrollBar != null && sender == _customScrollBar)
        {
            _isScrollingFromCustomBar = true;
            HandleScrollBarValueChange();
            _isScrollingFromCustomBar = false;
        }
    }

    /// <summary>
    /// Обработчик скролла основного ScrollViewer
    /// </summary>
    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateCustomScrollBar();
    }

    /// <summary>
    /// Обработчик колесика мыши для прокрутки текста
    /// </summary>
    private void OnTextPresenterPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_customScrollBar != null && IsMultiLine && HasMaxRows && _customScrollBar.IsHitTestVisible)
        {
            double delta = e.Delta.Y * 30;
            double newValue = Math.Max(_customScrollBar.Minimum,
                                     Math.Min(_customScrollBar.Maximum, _customScrollBar.Value + delta));

            _isScrollingFromCustomBar = true;
            _customScrollBar.Value = newValue;
            _isScrollingFromCustomBar = false;

            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (IsMultiLine && HasMaxRows && _customScrollBar != null && _textPresenter != null)
        {
            Dispatcher.UIThread.Post(HandleTextInputScroll, DispatcherPriority.Render);
        }
        else
        {
            Dispatcher.UIThread.Post(UpdateCustomScrollBar, DispatcherPriority.Render);
        }
    }

    /// <inheritdoc/>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateTextContainerPadding();
        Dispatcher.UIThread.Post(UpdateCustomScrollBar);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromEvents();
        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Инициализирует элементы шаблона
    /// </summary>
    private void InitializeTemplateElements(TemplateAppliedEventArgs e)
    {
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
        _customScrollBar = e.NameScope.Find<ScrollBar>("PART_CustomScrollBar");
        _textContainer = e.NameScope.Find<Border>("PART_TextContainer");

        UpdateTextContainerPadding();

        if (_scrollViewer != null && _customScrollBar != null && _textPresenter != null)
        {
            _customScrollBar.ValueChanged += OnCustomScrollBarValueChanged;
            _textPresenter.PointerWheelChanged += OnTextPresenterPointerWheelChanged;
        }
    }

    /// <summary>
    /// Обрабатывает изменения свойств
    /// </summary>
    private void HandlePropertyChanges(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IsMultiLineProperty ||
            change.Property == MinRowsProperty ||
            change.Property == MaxRowsProperty ||
            change.Property == FontSizeProperty ||
            change.Property == VariantProperty)
        {
            UpdateMultiLineStates();
            UpdateMultiLineHeight();
            UpdateTextContainerPadding();
        }

        if (change.Property == TextProperty ||
            change.Property == IsMultiLineProperty ||
            change.Property == MinRowsProperty ||
            change.Property == MaxRowsProperty)
        {
            Dispatcher.UIThread.Post(UpdateCustomScrollBar);
        }
    }

    /// <summary>
    /// Обрабатывает изменение значения скроллбара
    /// </summary>
    private void HandleScrollBarValueChange()
    {
        if (_textPresenter == null || _customScrollBar == null || _scrollViewer == null) return;

        double scrollBarValue = _customScrollBar.Value;
        var textLayout = _textPresenter.TextLayout;
        var viewportHeight = _scrollViewer.Bounds.Height;

        double scrollOffset = 0;
        if (textLayout != null)
        {
            double scrollableHeight = Math.Max(0, textLayout.Height - viewportHeight);
            scrollOffset = scrollableHeight - scrollBarValue;
            scrollOffset = Math.Max(0, Math.Min(scrollableHeight, scrollOffset));
        }

        double transformY = -scrollOffset;

        if (_textPresenter.RenderTransform is TranslateTransform transform)
        {
            transform.Y = transformY;
        }
        else
        {
            _textPresenter.RenderTransform = new TranslateTransform(0, transformY);
        }

        _textPresenter.InvalidateMeasure();
        _textPresenter.InvalidateArrange();
        _textPresenter.InvalidateVisual();
    }

    /// <summary>
    /// Обрабатывает автоматическую прокрутку при вводе текста
    /// </summary>
    private void HandleTextInputScroll()
    {
        if (_textPresenter == null || _customScrollBar == null || _scrollViewer == null) return;

        var textLayout = _textPresenter.TextLayout;
        var viewportHeight = _scrollViewer.Bounds.Height;

        if (textLayout != null)
        {
            if (textLayout.Height > viewportHeight)
            {
                double maxOffset = Math.Max(0, textLayout.Height - viewportHeight);

                _isScrollingFromCustomBar = true;
                _customScrollBar.Value = 0;

                if (_textPresenter.RenderTransform is TranslateTransform transform)
                {
                    transform.Y = -maxOffset;
                }
                else
                {
                    _textPresenter.RenderTransform = new TranslateTransform(0, -maxOffset);
                }

                _textPresenter.InvalidateVisual();
                _isScrollingFromCustomBar = false;
            }
            else
            {
                if (_textPresenter.RenderTransform is TranslateTransform transform)
                {
                    transform.Y = 0;
                }
            }
        }

        UpdateCustomScrollBar();
    }

    /// <summary>
    /// Отписывается от событий при удалении контрола
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
        }

        if (_customScrollBar != null)
        {
            _customScrollBar.ValueChanged -= OnCustomScrollBarValueChanged;
        }

        if (_textPresenter != null)
        {
            _textPresenter.PointerWheelChanged -= OnTextPresenterPointerWheelChanged;
        }
    }

    #endregion
}