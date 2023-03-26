using System;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Strings;

namespace Screenbox.Services
{
    public sealed class ResourceService : IResourceService
    {
        public string GetString(ResourceName name, params object[] parameters)
        {
            switch (name)
            {
                case ResourceName.RunTime:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.RunTime((string)parameters[0]);
                case ResourceName.PropertyTitle:
                    return Resources.PropertyTitle;
                case ResourceName.PropertySubtitle:
                    return Resources.PropertySubtitle;
                case ResourceName.PropertyYear:
                    return Resources.PropertyYear;
                case ResourceName.PropertyProducers:
                    return Resources.PropertyProducers;
                case ResourceName.PropertyWriters:
                    return Resources.PropertyWriters;
                case ResourceName.PropertyLength:
                    return Resources.PropertyLength;
                case ResourceName.PropertyResolution:
                    return Resources.PropertyResolution;
                case ResourceName.PropertyBitRate:
                    return Resources.PropertyBitRate;
                case ResourceName.PropertyContributingArtists:
                    return Resources.PropertyContributingArtists;
                case ResourceName.PropertyAlbum:
                    return Resources.PropertyAlbum;
                case ResourceName.PropertyAlbumArtist:
                    return Resources.PropertyAlbumArtist;
                case ResourceName.PropertyGenre:
                    return Resources.PropertyGenre;
                case ResourceName.PropertyTrack:
                    return Resources.PropertyTrack;
                case ResourceName.PropertyFileType:
                    return Resources.PropertyFileType;
                case ResourceName.PropertyContentType:
                    return Resources.PropertyContentType;
                case ResourceName.PropertySize:
                    return Resources.PropertySize;
                case ResourceName.PropertyLastModified:
                    return Resources.PropertyLastModified;
                case ResourceName.UnknownArtist:
                    return Resources.UnknownArtist;
                case ResourceName.UnknownAlbum:
                    return Resources.UnknownAlbum;
                case ResourceName.UnknownGenre:
                    return Resources.UnknownGenre;
                case ResourceName.Network:
                    return Resources.Network;
                case ResourceName.Disable:
                    return Resources.Disable;
                case ResourceName.FailedToLoadSubtitleNotificationTitle:
                    return Resources.FailedToLoadSubtitleNotificationTitle;
                case ResourceName.VolumeChangeStatusMessage:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.VolumeChangeStatusMessage((double)parameters[0]);
                case ResourceName.AccessDeniedMusicLibraryTitle:
                    return Resources.AccessDeniedMusicLibraryTitle;
                case ResourceName.AccessDeniedVideosLibraryTitle:
                    return Resources.AccessDeniedVideosLibraryTitle;
                case ResourceName.AccessDeniedPicturesLibraryTitle:
                    return Resources.AccessDeniedPicturesLibraryTitle;
                case ResourceName.OpenPrivacySettingsButtonText:
                    return Resources.OpenPrivacySettingsButtonText;
                case ResourceName.AccessDeniedMessage:
                    return Resources.AccessDeniedMessage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, null);
            }
        }

        public string GetString(PluralResourceName name, double count, params object[] parameters)
        {
            switch (name)
            {
                case PluralResourceName.ItemsCount:
                    return Resources.ItemsCount(count);
                case PluralResourceName.SongsCount:
                    return Resources.SongsCount((int)count);
                case PluralResourceName.AlbumsCount:
                    return Resources.AlbumsCount((int)count);
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, null);
            }
        }
    }
}
