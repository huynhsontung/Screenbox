using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class ChapterViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial double Value { get; set; }

        [ObservableProperty]
        public partial double Minimum { get; set; }

        [ObservableProperty]
        public partial double Maximum { get; set; }

        [ObservableProperty]
        public partial double Width { get; set; }
    }
}