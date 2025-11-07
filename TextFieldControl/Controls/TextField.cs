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
    #region Styled Properties

    /// <summary>
    /// Текст метки (label) поля
    /// </summary>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<TextField, string>(nameof(Label), "Label");

    /// <summary>
    /// Включение многострочного режима
    /// </summary>
    public static readonly StyledProperty<bool> IsMultiLineProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(IsMultiLine));

    /// <summary>
    /// Указывает, что поле имеет максимальное количество строк
    /// </summary>
    public static readonly StyledProperty<bool> HasMaxRowsProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(HasMaxRows));

    /// <summary>
    /// Указывает, что поле имеет минимальное количество строк
    /// </summary>
    public static readonly StyledProperty<bool> HasMinRowsProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(HasMinRows));

    #endregion

    #region Private Fields

    private ScrollViewer? _scrollViewer;
    private TextPresenter? _textPresenter;
    private ScrollBar? _customScrollBar;
    private bool _isScrollingFromCustomBar;
    private bool _isUpdatingFromClasses = false;

    #endregion

    #region Public Properties

    /// <summary>
    /// Текст метки (label) поля
    /// </summary>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
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
    /// Указывает, что поле имеет максимальное количество строк
    /// </summary>
    public bool HasMaxRows
    {
        get => GetValue(HasMaxRowsProperty);
        private set => SetValue(HasMaxRowsProperty, value);
    }

    /// <summary>
    /// Указывает, что поле имеет минимальное количество строк
    /// </summary>
    public bool HasMinRows
    {
        get => GetValue(HasMinRowsProperty);
        private set => SetValue(HasMinRowsProperty, value);
    }

    #endregion

    #region Template Methods

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        InitializeTemplateElements(e);
        SetupScrollBar();

        // Подписываемся на изменение коллекции Classes
        Classes.CollectionChanged += (_, _) => UpdatePropertiesFromClasses();

        UpdatePropertiesFromClasses(); // Автоматическое обновление свойств из классов
        UpdateTextFieldBehavior();

        Dispatcher.UIThread.Post(UpdateCustomScrollBar, DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Обновляем скроллбар при изменении текста или режима MultiLine
        if (change.Property == TextProperty ||
            change.Property == IsMultiLineProperty ||
            change.Property == HasMaxRowsProperty)
        {
            Dispatcher.UIThread.Post(UpdateCustomScrollBar);
        }

        // Обновляем поведение при изменении режима MultiLine
        if (change.Property == IsMultiLineProperty)
        {
            UpdateTextFieldBehavior();
        }
    }

    #endregion

    #region Class-to-Properties Mapping

    /// <summary>
    /// Автоматически обновляет свойства на основе установленных классов стилей
    /// </summary>
    private void UpdatePropertiesFromClasses()
    {
        if (_isUpdatingFromClasses) return;

        _isUpdatingFromClasses = true;
        try
        {
            // Определяем MultiLine режим по наличию классов MultiLine
            bool hasMultiLineClass = Classes.Contains("Flexible") ||
                                   Classes.Contains("MinRow") ||
                                   Classes.Contains("MaxRow");

            if (hasMultiLineClass != IsMultiLine)
            {
                SetCurrentValue(IsMultiLineProperty, hasMultiLineClass);
            }

            // Определяем тип MultiLine режима
            if (Classes.Contains("MaxRow"))
            {
                HasMaxRows = true;
                HasMinRows = false;
            }
            else if (Classes.Contains("MinRow"))
            {
                HasMaxRows = false;
                HasMinRows = true;
            }
            else if (Classes.Contains("Flexible"))
            {
                HasMaxRows = false;
                HasMinRows = false;
            }
            else
            {
                // Если нет MultiLine классов - сбрасываем оба свойства
                HasMaxRows = false;
                HasMinRows = false;
            }
        }
        finally
        {
            _isUpdatingFromClasses = false;
        }
    }

    /// <summary>
    /// Обновляет поведение текстового поля в соответствии с настройками
    /// </summary>
    private void UpdateTextFieldBehavior()
    {
        AcceptsReturn = IsMultiLine;
    }

    #endregion

    #region ScrollBar Implementation

    /// <summary>
    /// Настраивает кастомный скроллбар
    /// </summary>
    private void SetupScrollBar()
    {
        _customScrollBar.Opacity = 0;
        _customScrollBar.IsHitTestVisible = false;
        _textPresenter.RenderTransform = new TranslateTransform();

        _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
    }

    /// <summary>
    /// Обновляет состояние и положение кастомного скроллбара
    /// </summary>
    private void UpdateCustomScrollBar()
    {
        if (!IsMultiLine || !HasMaxRows)
        {
            ResetScrollBar();
            return;
        }

        var textLayout = _textPresenter.TextLayout;
        if (textLayout == null) return;

        var viewportHeight = _scrollViewer.Bounds.Height;
        var textHeight = textLayout.Height;

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
        _customScrollBar.Opacity = 1;
        _customScrollBar.IsHitTestVisible = true;

        var scrollableHeight = Math.Max(0, textHeight - viewportHeight);

        _customScrollBar.Minimum = 0;
        _customScrollBar.Maximum = scrollableHeight;
        _customScrollBar.ViewportSize = viewportHeight;
        _customScrollBar.LargeChange = viewportHeight;

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
        if (_textPresenter.RenderTransform is TranslateTransform transform)
        {
            double currentTransformY = transform.Y;
            double currentScrollOffset = Math.Max(0, -currentTransformY);
            currentScrollOffset = Math.Min(scrollableHeight, currentScrollOffset);

            // Значение скроллбара инвертируется, т.к. в UI верх считается нулём,
            // а Transform.Y работает в отрицательной системе координат.
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

    #region Event Handlers

    /// <summary>
    /// Обработчик изменения значения скроллбара
    /// </summary>
    private void OnCustomScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _isScrollingFromCustomBar = true;
        HandleScrollBarValueChange();
        _isScrollingFromCustomBar = false;
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
    private void OnTextFieldPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (IsMultiLine && HasMaxRows && _customScrollBar != null && _customScrollBar.IsHitTestVisible)
        {
            double scrollWheelSpeed = GetScrollWheelSpeed();
            double delta = e.Delta.Y * scrollWheelSpeed;
            double newValue = Math.Max(_customScrollBar.Minimum,
                                     Math.Min(_customScrollBar.Maximum, _customScrollBar.Value + delta));

            _isScrollingFromCustomBar = true;
            _customScrollBar.Value = newValue;
            _isScrollingFromCustomBar = false;

            e.Handled = true;
        }
    }

    /// <summary>
    /// Возвращает скорость прокрутки колесиком мыши из ресурсов
    /// </summary>
    private double GetScrollWheelSpeed()
    {
        var resource = this.FindResource("ScrollWheelSpeed");
        return resource is double speed ? speed : 20.0;
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (IsMultiLine && HasMaxRows)
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
        INameScope nameScope = e.NameScope;
        _scrollViewer = nameScope.Find<ScrollViewer>("PART_ScrollViewer") ?? throw new NullReferenceException(nameof(_scrollViewer));
        _textPresenter = nameScope.Find<TextPresenter>("PART_TextPresenter") ?? throw new NullReferenceException(nameof(_textPresenter));
        _customScrollBar = nameScope.Find<ScrollBar>("PART_CustomScrollBar") ?? throw new NullReferenceException(nameof(_customScrollBar));

        // Отписываемся перед подпиской (защита от дублирования)
        _customScrollBar.ValueChanged -= OnCustomScrollBarValueChanged;
        _customScrollBar.ValueChanged += OnCustomScrollBarValueChanged;

        // Подписываемся на колесико для всего контрола
        this.PointerWheelChanged -= OnTextFieldPointerWheelChanged;
        this.PointerWheelChanged += OnTextFieldPointerWheelChanged;
    }

    /// <summary>
    /// Обновляет визуальное состояние скроллбара, приводя позицию текста в соответствие с текущим значением
    /// </summary>
    private void HandleScrollBarValueChange()
    {
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
    /// Реагирует на ввод текста, автоматически прокручивая контент вниз,
    /// если достигнут предел высоты и включён режим MaxRows.
    /// </summary>
    private void HandleTextInputScroll()
    {
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

        this.PointerWheelChanged -= OnTextFieldPointerWheelChanged;
    }

    #endregion
}