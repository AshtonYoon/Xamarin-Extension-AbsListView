using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json;

namespace Aurender.Core.Data.Services
{
    [Flags]
    enum StreamingTrackFeatures
    {
        None,
        Free,
        Prime,
        Adult,
        BSide          
    }

    static class StreamingTrackFeaturesExt
    {
        public static bool IsFree(this StreamingTrackFeatures feature) => feature.HasFeature(StreamingTrackFeatures.Free);
        public static bool IsPrime(this StreamingTrackFeatures feature) => feature.HasFeature(StreamingTrackFeatures.Prime);
        public static bool IsForAdult(this StreamingTrackFeatures feature) => feature.HasFeature(StreamingTrackFeatures.Adult);
        public static bool IsBSide(this StreamingTrackFeatures feature) => feature.HasFeature(StreamingTrackFeatures.BSide);

        private static bool HasFeature(this StreamingTrackFeatures self, StreamingTrackFeatures feature)
        {
            return ((feature & self) == feature);
        }
    }


    [DebuggerDisplay("{ServiceType} Track {StreamingID} : [{AlbumTitle}] - [{Title}]  - [{ArtistName}]")]
    internal abstract class StreamingTrack : IStreamingTrack
    {
        public String Genre { get; set; } = String.Empty;
        public override bool Equals(object obj)
        {
            if (obj is StreamingTrack b)
            {
                return (b.ServiceType == this.ServiceType) && (b.StreamingID.Equals(this.StreamingID));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return $"{ServiceType.GetName()}:{this.StreamingID}".GetHashCode();
        }


        protected StreamingTrack(ContentType cType)
        {
            this.ServiceType = cType;
        }

        public override string ToString()
        {
            return $"{ServiceType} Track { StreamingID} : [{AlbumTitle}] - [{Title}]  - [{ArtistName}]";
        }

        [JsonIgnore]
        public ContentType ServiceType { get; private set; }
        [JsonIgnore]
        public virtual string ItemPath => $"{ServiceType.StreamingPrefix()}://{StreamingID}";


        public string StreamingID { get; set; }

        public string StreamingArtistID { get; set; }

        public string StreamingAlbumID { get; set; }

        public bool AllowStreaming { get; set; }


        [JsonIgnore]
        public bool IsFavorite => ServiceManager.Service(ServiceType).IsFavorite(this);
        public virtual bool IsPremiumOnly => false;

        [JsonIgnore]
        public abstract string ImageUrl { get; }
        [JsonIgnore]
        public abstract string ImageUrlForMediumSize { get; }

        [JsonIgnore]
        public abstract string ImageUrlForLargeSize { get; }

        public int Duration { get;  set; }

        public int DiscIndex { get; set; }

        public int TrackIndex { get; set; }

        public string Title { get; set; }

        public string ArtistName { get; set; }

        public string AlbumTitle { get; set; }

        [JsonIgnore]
        public object FrontCover => this.GetImage(ImageSize.Small);

        [JsonIgnore]
        public object BackCover { get; protected set; }


        #region Unused so static

        [JsonIgnore]
        public string ComposerName => String.Empty;

        [JsonIgnore]
        public string Conductor => String.Empty;

        [JsonIgnore]
        public ulong FileSize => 0;


        [JsonIgnore]
        public string ContainerFormat => this.ServiceType.GetName();

        [JsonIgnore]
        public byte BitWidth => 0;

        [JsonIgnore]
        public int SamplingRate => 0;

        [JsonIgnore]
        public int Bitrate => 0;

        [JsonIgnore]
        public byte Rating => 0;
        #endregion

        public IAlbum GetAlbum()
        {
            IARLogStatic.Error($"{ServiceType} Track", $"Doesn't support to get album");
            return null;
        }

        public Credits GetAlbumCredits()
        {
            IARLogStatic.Info($"{ServiceType} Track", $"Doesn't support to get album credits");
            return null;
        }

        public IArtist GetArtist()
        {
            IARLogStatic.Error($"{ServiceType} Track", $"Doesn't support to get artist");
            return null;
        }

        public IArtist GetComposer()
        {
            IARLogStatic.Error($"{ServiceType} Track", $"Doesn't support to get composer");
            return null;
        }

        public IArtist GetConductor()
        {
            IARLogStatic.Error($"{ServiceType} Track", $"Doesn't support to get conductor");
            return null;
        }

        public Credits GetSongCredits()
        {
            IARLogStatic.Info($"{ServiceType} Track", $"Doesn't support to get song credits");
            return null;
        }

        public Task<string> LoadLyrics()
        {
            IARLogStatic.Error($"{ServiceType} Track", $"Doesn't support to load lyrics");
            return null;
        }

        public StreamingItemType StreamingItemType => StreamingItemType.Track;
        public void SetFavorite(bool favorite)
        {
            var s = ServiceManager.Service(ServiceType);
            Debug.Assert(s != null, "Service must implement IStreamingServiceFavorite");
            
            Task.Run(async () => await s.UpdateFavorite(this, favorite).ConfigureAwait(false));
        }

        public Task AddToCache()
        {
            //return Task.Run( () => ServiceManager.Service(ServiceType).AddToCache(this));
            ServiceManager.it[ServiceType].AddToCache(this);
            return Task.CompletedTask;
        }

        public IList<string> GetLyrics()
        {
            throw new NotImplementedException();
        }

        public abstract object GetExtraImage();
    }
}
