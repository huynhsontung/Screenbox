using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LibVLCSharp.Shared.Structures;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class ChapterProgressBar : UserControl
    {
        public static readonly DependencyProperty ChaptersProperty = DependencyProperty.Register(
            nameof(Chapters),
            typeof(ICollection<ChapterDescription>),
            typeof(ChapterProgressBar),
            new PropertyMetadata(0, OnChaptersChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ChapterProgressBar),
            new PropertyMetadata(0, OnValueChanged));

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

        public double Maximum { get; set; }

        private ObservableCollection<ChapterProgressItem> ProgressItems { get; }

        private const double Spacing = 1;
        private int _activeIndex = -1;

        public ChapterProgressBar()
        {
            ProgressItems = new ObservableCollection<ChapterProgressItem>();
            this.InitializeComponent();
            SizeChanged += OnSizeChanged;
        }

        private static void OnChaptersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ChapterProgressBar)d;
            view.UpdateChapters();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ChapterProgressBar)d;
            view.UpdateProgress();
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
            var activeIndex = _activeIndex;

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
            if (activeIndex != _activeIndex)
            {
                // activeIndex == -1 when Value > total duration of all chapters
                if (activeIndex == -1 || _activeIndex == -1)
                {
                    foreach (var item in ProgressItems)
                    {
                        item.Value = Value;
                    }
                }
                else
                {
                    var from = Math.Min(activeIndex, _activeIndex);
                    var to = Math.Max(activeIndex, _activeIndex);
                    for (var i = from; i <= to; i++)
                    {
                        ProgressItems[i].Value = Value;
                    }
                }

                _activeIndex = activeIndex;
            }
            else if (activeIndex >= 0)
            {
                ProgressItems[activeIndex].Value = Value;
            }
        }

        private void UpdateChapters()
        {
            _activeIndex = -1;
            ProgressItems.Clear();
            if (Chapters?.Count > 0)
            {
                foreach (var chapterDescription in Chapters)
                {
                    var progressItem = new ChapterProgressItem
                    {
                        Minimum = chapterDescription.TimeOffset,
                        Maximum = chapterDescription.Duration + chapterDescription.TimeOffset
                    };

                    // This assumes Maximum is updated before this function is called
                    progressItem.Width = GetItemWidth(progressItem);
                    ProgressItems.Add(progressItem);
                }

                if (ProgressItems.Count != 0) return;
            }

            ProgressItems.Add(new ChapterProgressItem
            {
                Maximum = Maximum,
                Width = ActualWidth
            });
        }

        private double GetItemWidth(ChapterProgressItem item)
        {
            var availableWidth = ActualWidth - Spacing * (Chapters?.Count ?? 0);
            return Maximum > 0 ? (item.Maximum - item.Minimum) / Maximum * availableWidth : 0;
        }
    }
}
