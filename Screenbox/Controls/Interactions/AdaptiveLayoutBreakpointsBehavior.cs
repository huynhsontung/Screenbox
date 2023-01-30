using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace Screenbox.Controls.Interactions
{
    internal class AdaptiveLayoutBreakpointsBehavior : Behavior<Control>
    {
        public static readonly DependencyProperty BreakpointsProperty = DependencyProperty.Register(
            nameof(Breakpoints),
            typeof(string),
            typeof(AdaptiveLayoutBreakpointsBehavior),
            new PropertyMetadata(string.Empty, OnBreakpointsChanged));

        public static readonly DependencyProperty OverrideProperty = DependencyProperty.Register(
            nameof(Override),
            typeof(int),
            typeof(AdaptiveLayoutBreakpointsBehavior),
            new PropertyMetadata(-1, OnOverrideChanged));

        public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register(
            nameof(Current),
            typeof(int),
            typeof(AdaptiveLayoutBreakpointsBehavior),
            new PropertyMetadata(0));

        public int Current
        {
            get => (int)GetValue(CurrentProperty);
            private set => SetValue(CurrentProperty, value);
        }

        public int Override
        {
            get => (int)GetValue(OverrideProperty);
            set => SetValue(OverrideProperty, value);
        }

        public string Breakpoints
        {
            get => (string)GetValue(BreakpointsProperty);
            set => SetValue(BreakpointsProperty, value);
        }

        private double[] _breakpoints;

        public AdaptiveLayoutBreakpointsBehavior()
        {
            _breakpoints = Array.Empty<double>();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        private static void OnBreakpointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not string str) return;
            AdaptiveLayoutBreakpointsBehavior instance = (AdaptiveLayoutBreakpointsBehavior)d;
            string[] values = str.Split(",;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            instance._breakpoints = values.Select(s => double.TryParse(s, out double result) ? result : 0).ToArray();
        }

        private static void OnOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not int target || target < 0) return;
            AdaptiveLayoutBreakpointsBehavior instance = (AdaptiveLayoutBreakpointsBehavior)d;
            if (instance.AssociatedObject == null) return;
            if (VisualStateManager.GoToState(instance.AssociatedObject, $"Level{target}", true))
            {
                instance.Current = target;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Override >= 0) return;
            Control control = (Control)sender;
            double width = e.NewSize.Width;
            int target = 0;
            for (int i = 0; i < _breakpoints.Length; i++)
            {
                double currentBreak = _breakpoints[i];
                if (width < currentBreak)
                {
                    target = i;
                    break;
                }

                if (i == _breakpoints.Length - 1)
                {
                    target = _breakpoints.Length;
                }
            }

            if (VisualStateManager.GoToState(control, $"Level{target}", true))
            {
                Current = target;
            }
        }
    }
}
