﻿using System;
using Windows.ApplicationModel.Resources;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Core
{
    public enum PluralResourceName
    {
        ItemsCount,
        SongsCount,
        AlbumsCount
    }

    internal static class ResourceHelper
    {
        private static readonly ResourceLoader ResourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");

        public const string RunTime = "RunTime";
        public const string PropertyTitle = "PropertyTitle";
        public const string PropertySubtitle = "PropertySubtitle";
        public const string PropertyYear = "PropertyYear";
        public const string PropertyProducers = "PropertyProducers";
        public const string PropertyWriters = "PropertyWriters";
        public const string PropertyLength = "PropertyLength";
        public const string PropertyResolution = "PropertyResolution";
        public const string PropertyBitRate = "PropertyBitRate";
        public const string PropertyContributingArtists = "PropertyContributingArtists";
        public const string PropertyAlbum = "PropertyAlbum";
        public const string PropertyAlbumArtist = "PropertyAlbumArtist";
        public const string PropertyGenre = "PropertyGenre";
        public const string PropertyTrack = "PropertyTrack";
        public const string PropertyFileType = "PropertyFileType";
        public const string PropertyContentType = "PropertyContentType";
        public const string PropertySize = "PropertySize";
        public const string PropertyLastModified = "PropertyLastModified";
        public const string UnknownArtist = "UnknownArtist";
        public const string UnknownAlbum = "UnknownAlbum";
        public const string UnknownGenre = "UnknownGenre";
        public const string Network = "Network";
        public const string Disable = "Disable";
        public const string FailedToLoadSubtitleNotificationTitle = "FailedToLoadSubtitleNotificationTitle";
        public const string VolumeChangeStatusMessage = "VolumeChangeStatusMessage";

        public static string GetString(string resourceName, params object[] parameters)
        {
            string resource = ResourceLoader.GetString(resourceName);
            Guard.IsNotNullOrEmpty(resource);
            return parameters.Length > 0 ? string.Format(resource, parameters) : resource;
        }

        public static string GetPluralString(PluralResourceName name, double count, bool hasNoneState = true)
        {
            string resourceName = GetPluralResourceName(name);
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(ResourceLoader, resourceName, count, hasNoneState), count);
        }

        private static string GetPluralResourceName(PluralResourceName name)
        {
            string resourceName;
            switch (name)
            {
                case PluralResourceName.ItemsCount:
                    resourceName = nameof(PluralResourceName.ItemsCount);
                    break;
                case PluralResourceName.SongsCount:
                    resourceName = nameof(PluralResourceName.SongsCount);
                    break;
                case PluralResourceName.AlbumsCount:
                    resourceName = nameof(PluralResourceName.SongsCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, null);
            }

            return resourceName;
        }
    }
}
