#nullable enable

using Windows.UI.Xaml.Controls;
using Screenbox.ViewModels;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.Media;
using Windows.UI.Xaml.Input;
using FocusState = Windows.UI.Xaml.FocusState;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Toolkit.Uwp.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class MediaListViewItem : UserControl
    {
        public static readonly DependencyProperty ShowMediaIconProperty = DependencyProperty.Register(
            "ShowMediaIcon",
            typeof(bool),
            typeof(MediaListViewItem),
            new PropertyMetadata(false));

        public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
            "PlayCommand",
            typeof(ICommand),
            typeof(MediaListViewItem),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            "SelectionMode",
            typeof(ListViewSelectionMode),
            typeof(MediaListViewItem),
            new PropertyMetadata(default(ListViewSelectionMode), OnSelectionModeChanged));

        public ListViewSelectionMode SelectionMode
        {
            get => (ListViewSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public ICommand? PlayCommand
        {
            get => (ICommand)GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        public bool ShowMediaIcon
        {
            get => (bool)GetValue(ShowMediaIconProperty);
            set => SetValue(ShowMediaIconProperty, value);
        }

        internal MediaViewModel ViewModel => (MediaViewModel)DataContext;

        private SelectorItem? _selector;

        public MediaListViewItem()
        {
            this.InitializeComponent();

            EffectiveViewportChanged += OnEffectiveViewportChanged;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaListViewItem control = (MediaListViewItem)d;
            VisualStateManager.GoToState(control,
                control.SelectionMode == ListViewSelectionMode.Multiple ? "MultiSelectEnabled" : "MultiSelectDisabled",
                true);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.FindAscendant<SelectorItem>() is not {} selector) return;
            _selector = selector;

            selector.FocusEngaged -= SelectorFocusEngaged;
            selector.GettingFocus -= SelectorOnGettingFocus;
            selector.LostFocus -= SelectorOnLostFocus;
            selector.PointerEntered -= SelectorOnPointerEntered;
            selector.PointerExited -= SelectorOnPointerExited;
            selector.PointerCanceled -= SelectorOnPointerExited;
            selector.DoubleTapped -= SelectorOnDoubleTapped;

            selector.FocusEngaged += SelectorFocusEngaged;
            selector.GettingFocus += SelectorOnGettingFocus;
            selector.LostFocus += SelectorOnLostFocus;
            selector.PointerEntered += SelectorOnPointerEntered;
            selector.PointerExited += SelectorOnPointerExited;
            selector.PointerCanceled += SelectorOnPointerExited;
            selector.DoubleTapped += SelectorOnDoubleTapped;
        }

        private void SelectorOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PlayCommand?.Execute(ViewModel);
        }

        private void SelectorOnLostFocus(object sender, RoutedEventArgs e)
        {
            Control? control = FocusManager.GetFocusedElement() as Control;
            if (control?.FindParentOrSelf<MediaListViewItem>() == this) return;
            if (control == _selector) return;
            VisualStateManager.GoToState(this, "Normal", false);
        }

        private void SelectorOnGettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            VisualStateManager.GoToState(this, "PointerOver", false);
        }

        private void SelectorOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Normal", false);
        }

        private void SelectorOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "PointerOver", false);
        }

        private void SelectorFocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            PlayButton.Focus(FocusState.Programmatic);
        }

        private static async void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            const double threshold = 400;
            if (args.BringIntoViewDistanceY > threshold) return;
            MediaListViewItem item = (MediaListViewItem)sender;
            if (item.DataContext == null) return;
            await item.ViewModel.LoadDetailsAsync();
            UpdateDetailsLevel(item, item.ViewModel);
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            MediaListViewItem item = (MediaListViewItem)sender;
            if (item.DataContext == null) return;
            UpdateDetailsLevel(item, item.ViewModel);
        }

        private static void UpdateDetailsLevel(Control templateRoot, MediaViewModel media)
        {
            if (media.MusicProperties == null || media.MediaType != MediaPlaybackType.Music)
            {
                VisualStateManager.GoToState(templateRoot, "Level0", true);
                return;
            }

            if (templateRoot.ActualWidth > 800)
            {
                VisualStateManager.GoToState(templateRoot, "Level3", true);
            }
            else if (templateRoot.ActualWidth > 620)
            {
                VisualStateManager.GoToState(templateRoot, "Level2", true);
            }
            else
            {
                VisualStateManager.GoToState(templateRoot, "Level1", true);
            }
        }
    }
}
