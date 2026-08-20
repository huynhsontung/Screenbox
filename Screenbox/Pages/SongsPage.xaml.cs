using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongsPage : Page
{
    internal SongsPageViewModel ViewModel => (SongsPageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    private double _contentVerticalOffset;

    private readonly DispatcherQueue _dispatcherQueue;

    public SongsPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SongsPageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SongsPageViewModel.SortBy))
        {
            return;
        }

        UpdateSortVisualState(ViewModel.SortBy);
        UpdateSortByFlyout();
        SavePageState(0);
    }

    private void UpdateSortVisualState(SongSortOrder sortBy)
    {
        var state = sortBy switch
        {
            SongSortOrder.Album => "SortByAlbum",
            SongSortOrder.Artist => "SortByArtist",
            _ => "SortByTitle"
        };

        VisualStateManager.GoToState(this, state, true);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.NavigationMode == NavigationMode.Back
            && Common.TryGetPageState(nameof(SongsPage), Frame.BackStackDepth, out var state)
            && state is KeyValuePair<SongSortOrder, double> pair)
        {
            ViewModel.SortBy = pair.Key;
            _contentVerticalOffset = pair.Value;
        }

        UpdateSortVisualState(ViewModel.SortBy);
        UpdateSortByFlyout();

        if (!_dispatcherQueue.TryEnqueue(ViewModel.FetchSongs))
        {
            ViewModel.FetchSongs();
        }

        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel.OnNavigatedFrom();
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    private void SongListView_OnLoaded(object sender, RoutedEventArgs e)
    {
        ScrollViewer? scrollViewer = SongListView.FindDescendant<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        scrollViewer.ViewChanging += ScrollViewerOnViewChanging;

        if (_contentVerticalOffset <= 0)
        {
            return;
        }

        scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
    }

    private void ScrollViewerOnViewChanging(object? sender, ScrollViewerViewChangingEventArgs e)
    {
        SavePageState(e.NextView.VerticalOffset);
    }

    private void SavePageState(double verticalOffset)
    {
        Common.SavePageState(new KeyValuePair<SongSortOrder, double>(ViewModel.SortBy, verticalOffset), nameof(SongsPage), Frame.BackStackDepth);
    }

    private string GetSortByText(SongSortOrder tag)
    {
        var item = SortByFlyout.Items?.FirstOrDefault(x => x.Tag is SongSortOrder order && order == tag) ?? SortByFlyout.Items?.FirstOrDefault();
        return (item as MenuFlyoutItem)?.Text ?? string.Empty;
    }

    private string GetSortByButtonAutomationName(SongSortOrder value)
    {
        var optionText = GetSortByText(value);
        return Strings.Resources.SortByAutomationName(optionText);
    }

    private void UpdateSortByFlyout()
    {
        if (SortByFlyout.Items?.FirstOrDefault(x => x.Tag is SongSortOrder order && order == ViewModel.SortBy) is RadioMenuFlyoutItem radioItem)
        {
            radioItem.IsChecked = true;
        }
    }
}
