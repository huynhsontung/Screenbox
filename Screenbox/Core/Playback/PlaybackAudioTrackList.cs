#nullable enable

using LibVLCSharp.Shared;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback
{
    public class PlaybackAudioTrackList : ObservableTrackList<AudioTrack>
    {        
        public PlaybackAudioTrackList(Media media)
        {
            if (media.IsParsed)
            {
                AddVlcMediaTracks(media.Tracks);
            }
            else
            {
                media.ParsedChanged += Media_ParsedChanged;
            }
        }

        //public PlaybackAudioTrackList(MediaPlaybackItem playbackItem)
        //{
        //    playbackItem.AudioTracksChanged += PlaybackItem_AudioTracksChanged;
        //    MediaPlaybackAudioTrackList audioTracks = playbackItem.AudioTracks;
        //    foreach (Windows.Media.Core.AudioTrack track in audioTracks)
        //    {
        //        TrackList.Add(new AudioTrack(track));
        //    }
            
        //    SelectedIndex = audioTracks.SelectedIndex;
        //    audioTracks.SelectedIndexChanged += AudioTracks_SelectedIndexChanged;
        //}

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            Media media = (Media)sender;
            AddVlcMediaTracks(media.Tracks);
        }

        private void AddVlcMediaTracks(MediaTrack[] tracks)
        {
            foreach (MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Audio)
                {
                    TrackList.Add(new AudioTrack(track));
                }
            }
        }

        //private void PlaybackItem_AudioTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs args)
        //{
        //    int index = (int)args.Index;
        //    switch (args.CollectionChange)
        //    {
        //        case CollectionChange.Reset:
        //            TrackList.Clear();
        //            foreach (Windows.Media.Core.AudioTrack track in sender.AudioTracks)
        //            {
        //                TrackList.Add(new AudioTrack(track));
        //            }
                    
        //            break;
        //        case CollectionChange.ItemInserted:
        //            TrackList.Insert(index, new AudioTrack(sender.AudioTracks[index]));
        //            break;
        //        case CollectionChange.ItemRemoved:
        //            TrackList.RemoveAt(index);
        //            break;
        //        case CollectionChange.ItemChanged:
        //        default:
        //            // Not implemented
        //            break;
        //    }
        //}

        //private void AudioTracks_SelectedIndexChanged(ISingleSelectMediaTrackList sender, object args)
        //{
        //    SelectedIndex = sender.SelectedIndex;
        //}
    }
}
