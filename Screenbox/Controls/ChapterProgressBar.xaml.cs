#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class ChapterProgressBar : UserControl
    {
        public static readonly DependencyProperty ChaptersProperty = DependencyProperty.Register(
            nameof(Chapters),
            typeof(IReadOnlyCollection<ChapterCue>),
            typeof(ChapterProgressBar),
            new PropertyMetadata(null, OnChaptersChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ChapterProgressBar),
            new PropertyMetadata(0d, OnValueChanged));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(ChapterProgressBar),
            new PropertyMetadata(0d, OnMaximumChanged));

        public static readonly DependencyProperty ChapterIndexProperty = DependencyProperty.Register(
            nameof(ChapterIndex),
            typeof(int),
            typeof(ChapterProgressBar),
            new PropertyMetadata(-1));

        public IReadOnlyCollection<ChapterCue>? Chapters
        {
            get => (IReadOnlyCollection<ChapterCue>?)GetValue(ChaptersProperty);
            set => SetValue(ChaptersProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int ChapterIndex
        {
            get => (int)GetValue(ChapterIndexProperty);
            private set => SetValue(ChapterIndexProperty, value);
        }

        private ObservableCollection<ChapterViewModel> ProgressItems { get; }

        private const double Spacing = 1;

        public ChapterProgressBar()
        {
            ProgressItems = new ObservableCollection<ChapterViewModel>();
            this.InitializeComponent();
            SizeChanged += OnSizeChanged;
        }

        private static void OnChaptersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterProgressBar view = (ChapterProgressBar)d;
            view.PopulateProgressItems();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterProgressBar view = (ChapterProgressBar)d;
            view.UpdateProgress();
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterProgressBar view = (ChapterProgressBar)d;
            if (view.ProgressItems.Count == 1)
            {
                if (view.ProgressItems[0].Width == 0)
                    view.ProgressItems[0].Width = view.ActualWidth;

                view.ProgressItems[0].Maximum = (double)e.NewValue;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Maximum == 0) return;
            foreach (ChapterViewModel item in ProgressItems)
            {
                item.Width = GetItemWidth(item);
            }
        }

        private void UpdateProgress()
        {
            if (ProgressItems.Count == 1)
            {
                ProgressItems[0].Value = Value;
                return;
            }

            // One easy way to handle progress update is to loop the progress items
            // and update the Value property. However, it invokes UI update on every
            // progress item, on every value change, which is expensive.
            // So we do all the logic below just to update 1 item at a time.

            int activeIndex = ChapterIndex;

            // Find the chapter to change value to minimize UI update
            if (activeIndex == -1 ||
                activeIndex >= ProgressItems.Count ||
                ProgressItems[activeIndex].Maximum < Value ||
                ProgressItems[activeIndex].Minimum > Value)
            {
                if (Value > Maximum)
                {
                    activeIndex = -1;
                }
                else
                {
                    for (int i = 0; i < ProgressItems.Count; i++)
                    {
                        if (Value <= ProgressItems[i].Maximum)
                        {
                            activeIndex = i;
                            break;
                        }
                    }
                }
            }

            // Actually update the chapter progress
            if (activeIndex != ChapterIndex)
            {
                // activeIndex == -1 when Value > total duration of all chapters
                if (activeIndex == -1 || ChapterIndex == -1)
                {
                    foreach (ChapterViewModel item in ProgressItems)
                    {
                        item.Value = Value;
                    }
                }
                else
                {
                    int from = Math.Min(activeIndex, ChapterIndex);
                    int to = Math.Max(activeIndex, ChapterIndex);
                    for (int i = from; i <= to; i++)
                    {
                        ProgressItems[i].Value = Value;
                    }
                }

                ChapterIndex = activeIndex;
            }
            else if (activeIndex >= 0)
            {
                ProgressItems[activeIndex].Value = Value;
            }
        }

        private void PopulateProgressItems()
        {
            ChapterIndex = -1;
            ProgressItems.Clear();
            if (Chapters?.Count > 0)
            {
                foreach (ChapterCue cue in Chapters)
                {
                    ChapterViewModel progressItem = new()
                    {
                        Minimum = cue.StartTime.TotalMilliseconds,
                        Maximum = (cue.Duration + cue.StartTime).TotalMilliseconds
                    };

                    // This assumes Maximum is updated before this function is called
                    progressItem.Width = GetItemWidth(progressItem);
                    ProgressItems.Add(progressItem);
                }
            }
            else
            {
                ChapterIndex = 0;
                ProgressItems.Add(new ChapterViewModel
                {
                    Maximum = Maximum,
                    Width = ActualWidth
                });
            }
        }

        private double GetItemWidth(ChapterViewModel item)
        {
            double availableWidth = ActualWidth - Spacing * (Chapters?.Count ?? 0);
            return Maximum > 0 ? (item.Maximum - item.Minimum) / Maximum * availableWidth : 0;
        }
    }
}
