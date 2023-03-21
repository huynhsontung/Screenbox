using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class ChapterViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _value;

        [ObservableProperty]
        private double _minimum;

        [ObservableProperty]
        private double _maximum;

        [ObservableProperty]
        private double _width;
    }
}
