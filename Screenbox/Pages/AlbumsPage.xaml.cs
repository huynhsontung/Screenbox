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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AlbumsPage : Page
{
    internal AlbumsPageViewModel ViewModel => (AlbumsPageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    private readonly DispatcherQueue _dispatcherQueue;

    private double _contentVerticalOffset;

    public AlbumsPage()
    {
        this.InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DataContext = Ioc.Default.GetRequiredService<AlbumsPageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AlbumsPageViewModel.SortBy))
        {
            UpdateSortVisualState(ViewModel.SortBy);
            SavePageState(0);
        }
        else if (e.PropertyName == nameof(AlbumsPageViewModel.Genres))
        {
            UpdateGenreFlyoutItems();
        }
        else if (e.PropertyName == nameof(AlbumsPageViewModel.SelectedGenre))
        {
            UpdateGenreFlyoutSelection();
            SavePageState(0);
        }
    }

    private void UpdateSortVisualState(AlbumSortOrder sortBy)
    {
        var state = sortBy switch
        {
            AlbumSortOrder.Artist => "SortByArtist",
            _ => "SortByTitle"
        };
        VisualStateManager.GoToState(this, state, true);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.NavigationMode == NavigationMode.Back
            && Common.TryGetPageState(nameof(AlbumsPage), Frame.BackStackDepth, out var state))
        {
            if (state is PageState pageState)
            {
                ViewModel.SortBy = pageState.SortBy;
                ViewModel.SelectedGenre = pageState.SelectedGenre;
                _contentVerticalOffset = pageState.VerticalOffset;
            }
            else if (state is KeyValuePair<AlbumSortOrder, double> pair)
            {
                ViewModel.SortBy = pair.Key;
                _contentVerticalOffset = pair.Value;
            }
        }

        UpdateSortVisualState(ViewModel.SortBy);

        if (!_dispatcherQueue.TryEnqueue(ViewModel.FetchAlbums))
            ViewModel.FetchAlbums();

        UpdateGenreFlyoutItems();
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    private void UpdateGenreFlyoutItems()
    {
        GenreFlyout.Items.Clear();

        var allGenresItem = new RadioMenuFlyoutItem
        {
            Text = Strings.Resources.AllGenres,
            GroupName = "GenreFilter",
            IsChecked = ViewModel.SelectedGenre is null,
            Command = ViewModel.SetGenreCommand,
            CommandParameter = null
        };
        GenreFlyout.Items.Add(allGenresItem);

        var unknownGenreItem = new RadioMenuFlyoutItem
        {
            Text = Strings.Resources.UnknownGenre,
            GroupName = "GenreFilter",
            IsChecked = ViewModel.SelectedGenre == string.Empty,
            Command = ViewModel.SetGenreCommand,
            CommandParameter = string.Empty
        };
        GenreFlyout.Items.Add(unknownGenreItem);

        foreach (string genre in ViewModel.Genres)
        {
            var genreItem = new RadioMenuFlyoutItem
            {
                Text = genre,
                GroupName = "GenreFilter",
                IsChecked = ViewModel.SelectedGenre == genre,
                Command = ViewModel.SetGenreCommand,
                CommandParameter = genre
            };
            GenreFlyout.Items.Add(genreItem);
        }
    }

    private void UpdateGenreFlyoutSelection()
    {
        foreach (var item in GenreFlyout.Items.OfType<RadioMenuFlyoutItem>())
        {
            item.IsChecked = item.CommandParameter switch
            {
                null => ViewModel.SelectedGenre is null,
                string param => param == ViewModel.SelectedGenre,
                _ => false
            };
        }
    }

    private void AlbumGridView_OnLoaded(object sender, RoutedEventArgs e)
    {
        ScrollViewer? scrollViewer = AlbumGridView.FindDescendant<ScrollViewer>();
        if (scrollViewer == null) return;
        scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
        if (_contentVerticalOffset > 0)
        {
            scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
        }
    }

    private void ScrollViewerOnViewChanging(object? sender, ScrollViewerViewChangingEventArgs e)
    {
        SavePageState(e.NextView.VerticalOffset);
    }

    private record PageState(AlbumSortOrder SortBy, string? SelectedGenre, double VerticalOffset);

    private void SavePageState(double verticalOffset)
    {
        Common.SavePageState(new PageState(ViewModel.SortBy, ViewModel.SelectedGenre, verticalOffset), nameof(AlbumsPage),
            Frame.BackStackDepth);
    }

    private bool IsSortBy(AlbumSortOrder current, AlbumSortOrder target) => current == target;

    private string GetSortByText(AlbumSortOrder sortBy)
    {
        return sortBy switch
        {
            AlbumSortOrder.Artist => Strings.Resources.Artist,
            AlbumSortOrder.Year => Strings.Resources.ReleasedYear,
            AlbumSortOrder.DateAdded => Strings.Resources.DateAdded,
            _ => Strings.Resources.PropertyTitle
        };
    }

    private string GetSortByButtonAutomationName(AlbumSortOrder value)
    {
        var optionText = GetSortByText(value);
        return Strings.Resources.SortByAutomationName(optionText);
    }

    private string GetGenreText(string? genre)
    {
        return genre switch
        {
            null => Strings.Resources.AllGenres,
            "" => Strings.Resources.UnknownGenre,
            _ => genre
        };
    }

    private string GetGenreButtonAutomationName(string? genre)
    {
        var optionText = GetGenreText(genre);
        return Strings.Resources.FilterByGenreAutomationName(optionText);
    }
}
