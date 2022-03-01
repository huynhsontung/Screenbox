using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.Controls
{
    internal class ChapterProgressItem : ObservableObject
    {
        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public double Minimum
        {
            get => _minimum;
            set => SetProperty(ref _minimum, value);
        }

        public double Maximum
        {
            get => _maximum;
            set => SetProperty(ref _maximum, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private double _value;
        private double _minimum;
        private double _maximum;
        private double _width;
    }
}
