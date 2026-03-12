#nullable enable

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Playback;

namespace Screenbox.Core.ViewModels;

public sealed partial class EqualizerViewModel : ObservableRecipient
{
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private double _preamp;
    [ObservableProperty] private int _selectedPresetIndex;

    public ObservableCollection<EqualizerBandViewModel> Bands { get; }

    public ObservableCollection<string> Presets { get; }

    private Equalizer? _equalizer;
    private readonly PlayerContext _playerContext;

    private VlcMediaPlayer? VlcMediaPlayer =>
        _playerContext.MediaPlayer as VlcMediaPlayer;

    public EqualizerViewModel(PlayerContext playerContext)
    {
        _playerContext = playerContext;

        Bands = new ObservableCollection<EqualizerBandViewModel>();
        Presets = new ObservableCollection<string>();

        _selectedPresetIndex = 0;
        _equalizer = new Equalizer(0);
        Preamp = _equalizer.Preamp;
        RefreshBands();
        LoadPresets();

        IsActive = true;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        UpdatePlayerEqualizer();
    }

    partial void OnPreampChanged(double value)
    {
        if (_equalizer is null) return;

        _equalizer.SetPreamp((float)value);
        UpdatePlayerEqualizer();
    }

    partial void OnSelectedPresetIndexChanged(int value)
    {
        if (value < 0 || value == Presets.Count - 1 || _equalizer is null) return;

        _equalizer = new Equalizer((uint)value);
        Preamp = _equalizer.Preamp;
        RefreshBands();
        UpdatePlayerEqualizer();
    }

    private void LoadPresets()
    {
        if (_equalizer is null) return;

        uint presetCount = _equalizer.PresetCount;
        for (uint i = 0; i < presetCount; i++)
        {
            string? name = _equalizer.PresetName(i);
            if (name != null)
            {
                Presets.Add(name);
            }
        }
    }

    private void RefreshBands()
    {
        if (_equalizer is null) return;

        foreach (EqualizerBandViewModel bandVm in Bands)
        {
            bandVm.PropertyChanged -= BandViewModel_OnPropertyChanged;
        }

        Bands.Clear();

        uint bandCount = _equalizer.BandCount;
        for (uint i = 0; i < bandCount; i++)
        {
            float frequency = _equalizer.BandFrequency(i);
            double amp = (double)_equalizer.Amp(i);
            var bandVm = new EqualizerBandViewModel(i, frequency, amp);
            bandVm.PropertyChanged += BandViewModel_OnPropertyChanged;
            Bands.Add(bandVm);
        }
    }

    private void UpdatePlayerEqualizer()
    {
        var player = VlcMediaPlayer;
        if (player is null) return;

        if (IsEnabled && _equalizer is not null)
        {
            player.VlcPlayer.SetEqualizer(_equalizer);
        }
        else
        {
            player.VlcPlayer.UnsetEqualizer();
        }
    }

    private void BandViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EqualizerBandViewModel.Gain) &&
            sender is EqualizerBandViewModel band &&
            _equalizer is not null)
        {
            _equalizer.SetAmp((float)band.Gain, band.Index);
            SelectedPresetIndex = Presets.Count - 1;
            UpdatePlayerEqualizer();
        }
    }

    [RelayCommand]
    private void ResetEqualizer()
    {
        if (_equalizer is null) return;

        _equalizer = new Equalizer(0);
        Preamp = _equalizer.Preamp;
        RefreshBands();
        SelectedPresetIndex = 0;
        UpdatePlayerEqualizer();
    }
}
