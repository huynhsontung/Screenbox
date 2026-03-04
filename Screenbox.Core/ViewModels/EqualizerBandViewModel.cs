#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels;

public sealed partial class EqualizerBandViewModel : ObservableObject
{
    public uint Index { get; }

    public float Frequency { get; }

    [ObservableProperty]
    private double _gain;

    public EqualizerBandViewModel(uint index, float frequency, double gain)
    {
        Index = index;
        Frequency = frequency;
        _gain = gain;
    }
}
