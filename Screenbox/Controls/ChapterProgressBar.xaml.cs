using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LibVLCSharp.Shared.Structures;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class ChapterProgressBar : UserControl
    {
        public static readonly DependencyProperty ChaptersProperty = DependencyProperty.Register(
            nameof(Chapters),
            typeof(ICollection<ChapterDescription>),
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

        public ICollection<ChapterDescription> Chapters
        {
            get => (ICollection<ChapterDescription>)GetValue(ChaptersProperty);
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
            var view = (ChapterProgressBar)d;
            view.PopulateProgressItems();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ChapterProgressBar)d;
            view.UpdateProgress();
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ChapterProgressBar)d;
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
            foreach (var item in ProgressItems)
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

            var activeIndex = ChapterIndex;

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
                    for (var i = 0; i < ProgressItems.Count; i++)
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
                    foreach (var item in ProgressItems)
                    {
                        item.Value = Value;
                    }
                }
                else
                {
                    var from = Math.Min(activeIndex, ChapterIndex);
                    var to = Math.Max(activeIndex, ChapterIndex);
                    for (var i = from; i <= to; i++)
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
                foreach (var chapterDescription in Chapters)
                {
                    var progressItem = new ChapterViewModel
                    {
                        Minimum = chapterDescription.TimeOffset,
                        Maximum = chapterDescription.Duration + chapterDescription.TimeOffset
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
            var availableWidth = ActualWidth - Spacing * (Chapters?.Count ?? 0);
            return Maximum > 0 ? (item.Maximum - item.Minimum) / Maximum * availableWidth : 0;
        }
    }
}
