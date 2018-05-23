using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Setting;

namespace Aurender.Core.Player
{
    public struct AurenderPassCodeInfo 
    {
        public readonly bool IsRegistered;
        public readonly String Passcode;

        internal AurenderPassCodeInfo(bool registered, string passcode)
        {
            IsRegistered = registered;
            Passcode = passcode;
        }
    }

    public enum AurenderConnectionResult
    {
        Failed,
        RequireRegister,
        Connected
    }
    public delegate void AurenderEventHandler(IAurender aurender);
    public delegate void AurenderEventHandler<T>(IAurender aurender, T arg);
    public delegate void AurenderEventHandler<T, U>(IAurender aurender, T arg1, U arg2);

    public interface IAurender
    {
        String Name { get; }
        String MAC { get; }
        IAurenderStatus Status { get; }
        IPlayerController Controller { get; }
        IVolumeController VolumeController { get; }

        //void Connect();
        //void Disconnect();

        bool IsConnected();

		event EventHandler OnConnect;
		event EventHandler AfterDisconnect;

        event EventHandler<Tuple<byte, int>> RatingUpdated;

        //event Action<IAurender> OnDeviceInfoUpdated;

        event EventHandler<bool> OnDatabaseOpened;
        event EventHandler<String> OnDatabaseUpdated;
        event EventHandler<String> OnFindNewSystemSW;

        event EventHandler<Dictionary<String, String>> OnNewMessage;

        event EventHandler<Tuple<String, long, long>> OnDBDownloadProgress;
        event EventHandler<String> OnDBDownloadStarted;

        event EventHandler<IVolumeController> OnControllerableAmpDetected;

        IWindowedDataWatingDelegate waitingPopupDelegate { get; set; }

        Task<AurenderPassCodeInfo> GetPasscodeInfo();

        /// <summary>
        /// Please use whether the controller is registered or not using <![CDATA[GetPassCodeInfo()]]>
        /// If it is not registered, it will failed to connect.
        /// </summary>
        /// <returns></returns>
        Task<Boolean> ConnectAsync();
        Task          DisconnectAsync();

        
        IAurenderEndPoint ConnectionInfo { get; }

        IDB Database { get; }

        String DBVersion { get; }
        String RateVersion { get; }

        void DownloadDBAgain();
        
        IVersionChecker SystemSoftwareChecker { get; }

        Int32 IntegerSystemSoftwareVersion { get; }
        String SystemSoftwareVersion { get; }

		/// <summary>
		/// Will be called with new version information
		/// </summary>
		event EventHandler<String> AfterDatabaseUpdated;


        ConfigForDevice Settings { get; }


        Task<String> ShowPasscoderOnAurenderAsync();
        Task HidePasscoder();

        /// <summary>
        /// If returned string is null, it means it i
        /// </summary>
        /// <param name="orgPasscode"></param>
        /// <param name="userPassCode"></param>
        /// <returns>If failed to register, new passcode will be returned.</returns>
        Task<AurenderPassCodeInfo> RegisterRemoteToAurenderAsync(String orgPasscode, String userPassCode);

        IDataManager<IDatabaseItem>[] Managers { get; }

        IAlbumManager AlbumManager { get; }
        IArtistManager ArtistManager { get; }
        IConductorManager ConductorManager { get; }
        IComposerManager ComposerManager { get; }
        IGenreManager GenreManager { get; }
        ISongManager SongManager { get; }

        IFileBrowser FileBrowser { get; }
        IFileBrowser CreateFileBrowser();
    }

}