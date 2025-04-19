#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.WinUI;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media.Core;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

        private readonly DispatcherQueueTimer _chaptersUpdateTimer;

        public ChapterProgressBar()
        {
            _chaptersUpdateTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            ProgressItems = new ObservableCollection<ChapterViewModel>();
            this.InitializeComponent();
            SizeChanged += OnSizeChanged;
        }

        private static void OnChaptersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterProgressBar view = (ChapterProgressBar)d;
            if (e.OldValue is INotifyCollectionChanged oldObservable)
            {
                oldObservable.CollectionChanged -= view.ChaptersOnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newObservable)
            {
                newObservable.CollectionChanged += view.ChaptersOnCollectionChanged;
            }

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
            if (ProgressItems.Count == 1)
            {
                ProgressItems[0].Width = ActualWidth;
            }
            else
            {
                foreach (ChapterViewModel item in ProgressItems)
                {
                    item.Width = GetItemWidth(item.Maximum - item.Minimum);
                }
            }
        }

        private void ChaptersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _chaptersUpdateTimer.Debounce(PopulateProgressItems, TimeSpan.FromMilliseconds(50));
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
            ProgressItems.Clear();
            if (Chapters?.Count > 0)
            {
                ChapterIndex = -1;
                var lastChapterEndTime = TimeSpan.Zero;
                foreach (ChapterCue cue in Chapters)
                {
                    var gap = cue.StartTime - lastChapterEndTime;
                    if (gap > TimeSpan.FromMilliseconds(500))
                    {
                        // If there is a gap between chapters, we need to add a dummy chapter
                        ChapterViewModel gapChapter = new()
                        {
                            Minimum = lastChapterEndTime.TotalMilliseconds,
                            Maximum = cue.StartTime.TotalMilliseconds,
                            Width = GetItemWidth(gap.TotalMilliseconds)
                        };

                        ProgressItems.Add(gapChapter);
                    }

                    lastChapterEndTime = cue.StartTime + cue.Duration;
                    var startTime = cue.StartTime.TotalMilliseconds;
                    var endTime = (cue.Duration + cue.StartTime).TotalMilliseconds;
                    ChapterViewModel chapter = new()
                    {
                        Minimum = startTime,
                        Maximum = endTime,
                        Width = GetItemWidth(endTime - startTime)
                    };

                    ProgressItems.Add(chapter);
                }

                // Check if the last chapter end time matches the media length
                if (Maximum - lastChapterEndTime.TotalMilliseconds > 500)
                {
                    // If not, we need to add a dummy chapter to fill the gap
                    ChapterViewModel gapChapter = new()
                    {
                        Minimum = lastChapterEndTime.TotalMilliseconds,
                        Maximum = Maximum,
                        Width = GetItemWidth(Maximum - lastChapterEndTime.TotalMilliseconds)
                    };

                    ProgressItems.Add(gapChapter);
                    LogService.Log("Chapters duration does not match with media length.");
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

        private double GetItemWidth(double durationMs)
        {
            double availableWidth = ActualWidth - Spacing * (Chapters?.Count ?? 0);
            return Maximum > 0 ? durationMs / Maximum * availableWidth : 0;
        }
    }
}
