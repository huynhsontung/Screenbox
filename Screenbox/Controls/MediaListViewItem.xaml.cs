#nullable enable

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Screenbox.ViewModels;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.Media;
using Windows.UI.Xaml.Input;
using FocusState = Windows.UI.Xaml.FocusState;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class MediaListViewItem : UserControl
    {
        public static readonly DependencyProperty ShowMediaIconProperty = DependencyProperty.Register(
            nameof(ShowMediaIcon),
            typeof(bool),
            typeof(MediaListViewItem),
            new PropertyMetadata(false));

        public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
            nameof(PlayCommand),
            typeof(ICommand),
            typeof(MediaListViewItem),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            nameof(SelectionMode),
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

        internal MediaViewModel? ViewModel { get; private set; }

        private SelectorItem? _selector;

        public MediaListViewItem()
        {
            this.InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue == null) return;
            ViewModel = (MediaViewModel)args.NewValue;
            await UpdateContent();
            UpdateDetailsLevel();
        }

        private async Task UpdateContent()
        {
            if (ViewModel == null) return;
            if (ViewModel.BasicProperties == null)
            {
                await ViewModel.LoadDetailsAsync();
            }

            ItemIcon.Glyph = ViewModel.Glyph;
            PlayButton.CommandParameter = ViewModel;
            TitleText.Text = ViewModel.Name;
            AlbumText.Text = ViewModel.Album?.Name ?? string.Empty;
            GenreText.Text = ViewModel.Genre ?? string.Empty;

            if (ViewModel.Duration != null)
            {
                DurationText.Text = HumanizedDurationConverter.Convert((TimeSpan)ViewModel.Duration);
            }

            if (ViewModel.Artists?.Length > 0)
            {
                ArtistText.Text = ViewModel.Artists[0].Name;
            }
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

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            MediaListViewItem item = (MediaListViewItem)sender;
            if (item.DataContext == null) return;
            item.UpdateDetailsLevel();
        }

        public void UpdateDetailsLevel()
        {
            if (ViewModel?.MusicProperties == null || ViewModel.MediaType != MediaPlaybackType.Music)
            {
                VisualStateManager.GoToState(this, "Level0", true);
                return;
            }

            if (ActualWidth > 800)
            {
                VisualStateManager.GoToState(this, "Level3", true);
            }
            else if (ActualWidth > 620)
            {
                VisualStateManager.GoToState(this, "Level2", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Level1", true);
            }
        }
    }
}
