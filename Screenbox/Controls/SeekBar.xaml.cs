﻿#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class SeekBar : UserControl
    {
        public static readonly DependencyProperty ProgressOnlyProperty = DependencyProperty.Register(
            nameof(ProgressOnly),
            typeof(bool),
            typeof(SeekBar),
            new PropertyMetadata(false));

        public bool ProgressOnly
        {
            get => (bool)GetValue(ProgressOnlyProperty);
            set => SetValue(ProgressOnlyProperty, value);
        }

        internal SeekBarViewModel ViewModel => (SeekBarViewModel)DataContext;
        private readonly DispatcherQueueTimer _previewToolTipTimer;
        private bool _overridePreviewToolTipDelay;
        private Thumb? _seekBarThumb;

        private readonly ToolTip _previewToolTip;

        public SeekBar()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SeekBarViewModel>();
            RegisterSeekBarPointerHandlers();
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _previewToolTipTimer = dispatcherQueue.CreateTimer();
            _previewToolTip = new ToolTip { Padding = new Thickness(8, 3, 8, 5), FontSize = 14 };

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ShouldShowPreview) && !ViewModel.ShouldShowPreview)
            {
                ResetPreviewToolTip();
            }
        }

        private void RegisterSeekBarPointerHandlers()
        {
            SeekBarSlider.AddHandler(PointerPressedEvent, (PointerEventHandler)PointerPressedEventHandler, true);
            SeekBarSlider.AddHandler(PointerReleasedEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
            SeekBarSlider.AddHandler(PointerCanceledEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
            SeekBarSlider.AddHandler(PointerMovedEvent, (PointerEventHandler)PointerMovedEventHandler, true);
        }

        private void PointerReleasedEventHandler(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.OnSeekBarPointerEvent(false);
            if (!ViewModel.ShouldShowPreview)
            {
                _previewToolTipTimer.Debounce(() =>
                {
                    ViewModel.ShouldShowPreview = true;
                }, TimeSpan.FromMilliseconds(500));
            }
        }

        private void PointerPressedEventHandler(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.OnSeekBarPointerEvent(true);
            ViewModel.ShouldShowPreview = false;
        }

        private void PointerMovedEventHandler(object s, PointerRoutedEventArgs e)
        {
            if (!ViewModel.ShouldShowPreview) return;
            PointerPoint pointerPoint = e.GetCurrentPoint(SeekBarSlider);
            UpdatePreviewTime(pointerPoint);
            if (_previewToolTip.IsOpen || _overridePreviewToolTipDelay)
            {
                _overridePreviewToolTipDelay = false;
                _previewToolTipTimer.Stop();
                ShowPreviewToolTip(pointerPoint);
            }
            else
            {
                _previewToolTipTimer.Debounce(() => ShowPreviewToolTip(pointerPoint), TimeSpan.FromMilliseconds(50));
            }
        }

        private void SeekBarSlider_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.ShouldShowPreview = true;
            _previewToolTipTimer.Stop();
            _previewToolTip.IsOpen = false;
            ToolTipService.SetToolTip(SeekBarSlider, null);
        }

        private void SeekBarSlider_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _previewToolTip.PlacementRect = new Rect(new Point(0, 0), e.NewSize);
        }

        private void SeekBarSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            _seekBarThumb = SeekBarSlider.FindDescendant<Thumb>();
            if (_seekBarThumb != null)
            {
                _seekBarThumb.PointerEntered += SeekBarThumb_PointerEntered;
                _seekBarThumb.PointerExited += SeekBarThumb_PointerExited;
                _seekBarThumb.PointerCanceled += SeekBarSlider_OnPointerExited;
            }
        }

        private void SeekBarThumb_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.ShouldShowPreview = true;
            _overridePreviewToolTipDelay = true;
        }

        private void SeekBarThumb_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.ShouldShowPreview = false;
        }

        private void SeekBarSlider_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.IsGenerated) return;
            PointerPoint pointer = e.GetCurrentPoint((UIElement)sender);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            ViewModel.OnSeekBarPointerWheelChanged(mouseWheelDelta);
        }

        private void ResetPreviewToolTip()
        {
            _previewToolTipTimer.Stop();
            _previewToolTip.IsOpen = false;
        }

        private void ShowPreviewToolTip(PointerPoint pointerPoint)
        {
            double halfWidth = SeekBarSlider.ActualWidth / 2;
            if (ToolTipService.GetToolTip(SeekBarSlider) is not ToolTip)
            {
                ToolTipService.SetToolTip(SeekBarSlider, _previewToolTip);
            }
            else
            {
                _previewToolTip.IsOpen = true;
            }

            _previewToolTip.Translation = new Vector3((float)(-halfWidth + pointerPoint.Position.X), 0, 0);
        }

        private void UpdatePreviewTime(PointerPoint pointerPoint)
        {
            double pointerOffset = pointerPoint.Position.X;
            double pointerOffsetRelative = pointerOffset / SeekBarSlider.ActualWidth; // have not accounted for padding
            double thumbOffset = 0;
            if (_seekBarThumb != null)
            {
                double thumbWidth = _seekBarThumb.ActualWidth;
                thumbOffset = thumbWidth * (pointerOffsetRelative - 0.5);
            }

            double normalizedPosition = (pointerOffset + thumbOffset) / SeekBarSlider.ActualWidth;
            ViewModel.UpdatePreviewTime(normalizedPosition);
            _previewToolTip.Content = Humanizer.ToDuration(ViewModel.PreviewTime);
        }
    }
}
