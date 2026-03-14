#nullable enable

using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Screenbox.Controls;

/// <summary>
/// Represents a control that replaces the default system title bar with a container,
/// enabling the app view to extend into the title bar area.
/// </summary>
/// <remarks>
/// Use the <b>TitleBar</b> control to replace the default system title bar with
/// a custom title bar that integrates with your app UI.
/// <para>Key features include:</para>
/// <list type="bullet">
/// <item><description><b>Interactive content:</b> Place arbitrary UI in the title bar,
/// like buttons, menus, or a search box using the <see cref="Header"/>, <see cref="Content"/>,
/// and <see cref="Footer"/> properties.</description></item>
/// <item><description><b>Colors:</b> Customize caption button colors.</description></item>
/// <item><description><b>Title and icon:</b> Change the display title and app icon.</description></item>
/// </list>
/// </remarks>
/// <example>
/// This example creates a simple title bar that replaces the system title bar.
/// It has a title, icon, custom content, and a footer.
/// <code lang="xml"><![CDATA[
/// <Page>
///     <Grid x:Name="RootGrid">
///         <local:TitleBar x:Name="AppTitleBar" Title="App title">
///             <local:TitleBar.Icon>
///                 <BitmapIcon UriSource="ms-appx:///Assets/StoreLogo.png" />
///             </local:TitleBar.Icon>
///             <local:TitleBar.Content>
///                 <AutoSuggestBox PlaceholderText="Search" QueryIcon="Find" />
///             </local:TitleBar.Content>
///             <local:TitleBar.Footer>
///                 <PersonPicture Initials="JD" Width="32" Height="32" />
///             </local:TitleBar.Footer>
///         </local:TitleBar>
///     </Grid>
/// </Page>
/// ]]></code>
/// <code lang="c#">
/// public Page()
/// {
///     InitializeComponent();
///     SetupTitleBar();
/// }
///
/// private void SetupTitleBar()
/// {
///     var customTitleBar = new TitleBar
///     {
///         Name = "AppTitleBar",
///         Title = "App title",
///         Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/StoreLogo.png") },
///         Content = new AutoSuggestBox() { QueryIcon = new SymbolIcon(Symbol.Find) },
///         Footer = new PersonPicture() { Initials = "JD", Width = 32, Height = 32 }
///     };
///     RootGrid.Children.Add(customTitleBar);
/// }
/// </code>
/// </example>
public sealed partial class TitleBar : ContentControl
{
    private const string LeftPaddingColumnName = "LeftPaddingColumn";
    private const string RightPaddingColumnName = "RightPaddingColumn";
    private const string DragRegionName = "DragRegion";

    private const string ActivatedStateName = "Activated";
    private const string DeactivatedStateName = "Deactivated";
    private const string StandardStateName = "Standard";
    private const string TallStateName = "Tall";
    //private const string TitleBarCollapsedStateName = "TitleBarCollapsed";
    //private const string TitleBarVisibleStateName = "TitleBarVisible";
    private const string HeaderCollapsedStateName = "HeaderCollapsed";
    private const string HeaderVisibleStateName = "HeaderVisible";
    private const string IconCollapsedStateName = "IconCollapsed";
    private const string IconVisibleStateName = "IconVisible";
    private const string TitleCollapsedStateName = "TitleCollapsed";
    private const string TitleVisibleStateName = "TitleVisible";
    private const string ContentCollapsedStateName = "ContentCollapsed";
    private const string ContentVisibleStateName = "ContentVisible";
    private const string FooterCollapsedStateName = "FooterCollapsed";
    private const string FooterVisibleStateName = "FooterVisible";

    private CoreApplicationViewTitleBar? _coreTitleBar;
    private Window? _window;

    private long _flowDirectionCallbackToken;

    private ColumnDefinition? _leftPaddingColumn;
    private ColumnDefinition? _rightPaddingColumn;
    private Grid? _dragRegion;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBar"/> class.
    /// </summary>
    public TitleBar()
    {
        DefaultStyleKey = typeof(TitleBar);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TitleBarAutomationPeer(this);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _leftPaddingColumn = (ColumnDefinition?)GetTemplateChild(LeftPaddingColumnName);
        _rightPaddingColumn = (ColumnDefinition?)GetTemplateChild(RightPaddingColumnName);
        _dragRegion = (Grid?)GetTemplateChild(DragRegionName);

        SetDragRegion();
        UpdateCaptionButtonColor();
        //UpdateVisibility();
        UpdateHeight();
        UpdatePadding();
        UpdateHeader();
        UpdateIcon();
        UpdateTitle();
        UpdateContent();
        UpdateFooter();
    }

    /// <summary>
    /// Resets the title bar to its default appearance.
    /// </summary>
    public void ResetTitleBar()
    {
        _window?.SetTitleBar(null);
        if (_coreTitleBar != null)
            _coreTitleBar.ExtendViewIntoTitleBar = false;
    }

    /// <summary>
    /// Sets the drag region for the window title bar.
    /// </summary>
    public void SetDragRegion()
    {
        if (_window is null) return;

        if (_dragRegion is not null)
        {
            _window.SetTitleBar(_dragRegion);
        }
        else
        {
            _window.SetTitleBar(null);
        }
    }

    public void SetCaptionButtonColor()
    {
        UpdateCaptionButtonColor();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (CoreApplication.GetCurrentView() is { } coreApplicationView)
        {
            _coreTitleBar = coreApplicationView.TitleBar;
            _coreTitleBar.ExtendViewIntoTitleBar = true;
            _coreTitleBar.LayoutMetricsChanged += CoreTitleBar_OnLayoutMetricsChanged;
            //_coreTitleBar.IsVisibleChanged += CoreTitleBar_OnIsVisibleChanged;
        }

        _window = Window.Current;
        if (_window != null)
            _window.Activated += Window_OnActivated;

        _flowDirectionCallbackToken = RegisterPropertyChangedCallback(FlowDirectionProperty, OnFlowDirectionChanged);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_coreTitleBar != null)
        {
            _coreTitleBar.LayoutMetricsChanged -= CoreTitleBar_OnLayoutMetricsChanged;
            //_coreTitleBar.IsVisibleChanged -= CoreTitleBar_OnIsVisibleChanged;
            _coreTitleBar = null;
        }

        if (_window != null)
        {
            _window.Activated -= Window_OnActivated;
            _window = null;
        }

        UnregisterPropertyChangedCallback(FlowDirectionProperty, _flowDirectionCallbackToken);

        _leftPaddingColumn = null;
        _rightPaddingColumn = null;
        _dragRegion = null;
    }

    private void Window_OnActivated(object sender, WindowActivatedEventArgs e)
    {
        string stateName = e.WindowActivationState is CoreWindowActivationState.Deactivated
            ? DeactivatedStateName
            : ActivatedStateName;

        VisualStateManager.GoToState(this, stateName, true);
    }

    private void CoreTitleBar_OnLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
    {
        UpdatePadding();
    }

    //private void CoreTitleBar_OnIsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
    //{
    //    UpdateVisibility();
    //}

    private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        var property = args.Property;

        if (property == HeightModeProperty)
        {
            UpdateHeight();
        }
        else if (property == HeaderProperty || property == HeaderTemplateProperty)
        {
            UpdateHeader();
        }
        else if (property == IconProperty)
        {
            UpdateIcon();
        }
        else if (property == TitleProperty)
        {
            UpdateTitle();
        }
        else if (property == ContentProperty)
        {
            UpdateContent();
        }
        else if (property == FooterProperty || property == FooterTemplateProperty)
        {
            UpdateFooter();
        }
        else if (property == CaptionButtonBackgroundBrushProperty
            || property == CaptionButtonBackgroundPointerOverBrushProperty
            || property == CaptionButtonBackgroundPressedBrushProperty
            || property == CaptionButtonBackgroundInactiveBrushProperty
            || property == CaptionButtonForegroundBrushProperty
            || property == CaptionButtonForegroundPointerOverBrushProperty
            || property == CaptionButtonForegroundPressedBrushProperty
            || property == CaptionButtonForegroundInactiveBrushProperty)
        {
            UpdateCaptionButtonColor();
        }
    }

    private void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdatePadding();
    }

    //private void UpdateVisibility()
    //{
    //    if (CoreApplication.GetCurrentView() is { } coreApplicationView)
    //    {
    //        string stateName = coreApplicationView.TitleBar.IsVisible
    //            ? TitleBarVisibleStateName
    //            : TitleBarCollapsedStateName;

    //        VisualStateManager.GoToState(this, stateName, false);
    //    }
    //}

    private void UpdateHeight()
    {
        string stateName = HeightMode is TitleBarHeightMode.Standard
            ? StandardStateName
            : TallStateName;

        VisualStateManager.GoToState(this, stateName, false);
    }

    private void UpdatePadding()
    {
        if (_coreTitleBar is null) return;

        double leadingInset = _coreTitleBar.SystemOverlayLeftInset;
        double trailingInset = _coreTitleBar.SystemOverlayRightInset;
        bool isRtl = FlowDirection is FlowDirection.RightToLeft;

        if (_leftPaddingColumn != null)
            _leftPaddingColumn.Width = new GridLength(isRtl ? trailingInset : leadingInset);

        if (_rightPaddingColumn != null)
            _rightPaddingColumn.Width = new GridLength(isRtl ? leadingInset : trailingInset);
    }

    private void UpdateHeader()
    {
        if (Header is null)
        {
            VisualStateManager.GoToState(this, HeaderCollapsedStateName, false);
        }
        else
        {
            //HeightMode = TitleBarHeightMode.Tall;
            VisualStateManager.GoToState(this, HeaderVisibleStateName, false);
        }

        //UpdateHeight();
    }

    private void UpdateIcon()
    {
        string stateName = Icon is null
            ? IconCollapsedStateName
            : IconVisibleStateName;

        VisualStateManager.GoToState(this, stateName, false);
    }

    private void UpdateTitle()
    {
        string stateName = string.IsNullOrEmpty(Title)
            ? TitleCollapsedStateName
            : TitleVisibleStateName;

        VisualStateManager.GoToState(this, stateName, false);
    }

    private void UpdateContent()
    {
        if (Content is null)
        {
            VisualStateManager.GoToState(this, ContentCollapsedStateName, false);
        }
        else
        {
            //HeightMode = TitleBarHeightMode.Tall;
            VisualStateManager.GoToState(this, ContentVisibleStateName, false);
        }

        //UpdateHeight();
    }

    private void UpdateFooter()
    {
        if (Footer is null)
        {
            VisualStateManager.GoToState(this, FooterCollapsedStateName, false);
        }
        else
        {
            //HeightMode = TitleBarHeightMode.Tall;
            VisualStateManager.GoToState(this, FooterVisibleStateName, false);
        }

        //UpdateHeight();
    }

    private void UpdateCaptionButtonColor()
    {
        // Avoid updating when not visible to prevent the Player page title bar colors
        // from being overwritten by the Main page title bar colors.
        var titleBar = ApplicationView.GetForCurrentView()?.TitleBar;
        if (titleBar is null || Visibility is Visibility.Collapsed) return;

        titleBar.ButtonBackgroundColor = GetNullableColorFromBrush(CaptionButtonBackgroundBrush);
        titleBar.ButtonHoverBackgroundColor = GetNullableColorFromBrush(CaptionButtonBackgroundPointerOverBrush);
        titleBar.ButtonPressedBackgroundColor = GetNullableColorFromBrush(CaptionButtonBackgroundPressedBrush);
        titleBar.ButtonInactiveBackgroundColor = GetNullableColorFromBrush(CaptionButtonBackgroundInactiveBrush);

        titleBar.ButtonForegroundColor = GetNullableColorFromBrush(CaptionButtonForegroundBrush);
        titleBar.ButtonHoverForegroundColor = GetNullableColorFromBrush(CaptionButtonForegroundPointerOverBrush);
        titleBar.ButtonPressedForegroundColor = GetNullableColorFromBrush(CaptionButtonForegroundPressedBrush);
        titleBar.ButtonInactiveForegroundColor = GetNullableColorFromBrush(CaptionButtonForegroundInactiveBrush);
    }

    private static Color? GetNullableColorFromBrush(Brush brush)
    {
        return brush is not SolidColorBrush solidColorBrush
            ? null
            : solidColorBrush.Color;
    }
}
