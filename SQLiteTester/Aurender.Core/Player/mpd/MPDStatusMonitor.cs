using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace Aurender.Core.Player.mpd
{
    class MPDStatusMonitor : ObservableObject, IARLog
    {
        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        readonly MPDStatus status;
        private IAurenderEndPoint _connectionInfo;
        private MPDConnectionForStatus _connection;
        private bool _IsConnected = false;
        private CancellationTokenSource _cancelationTokenSource;
        private Task connectionTask;

        internal MPDStatusMonitor(IAurenderEndPoint endPoint)
        {
            _connectionInfo = endPoint;

            _connection = new MPDConnectionForStatus
            {
                BufferSize = 256
            };
            status = new MPDStatus();
        }

        internal void SetQueue(IPlaylist queue)
        {
            status.Queue = queue;
        }

        internal IAurenderStatus Status { get => status; }

        internal bool IsConnected
        {
            get => _IsConnected;
            set => Set<bool>(() => this.IsConnected, ref _IsConnected, value);
        }

        internal async Task<bool> ConnectAsync()
        {
            if (IsConnected)
            {
                this.LP("MPD Status", "Already connected");
                return true;
            }
            _cancelationTokenSource = new CancellationTokenSource();

            this.LP("MPD Status", $"Connecting to {_connectionInfo}");

            bool result = await _connection.InitAsync(_connectionInfo);
            if (result)
            {
                this.LP("MPD Status", $"Now connected to [[[[[{_connectionInfo.Name}]]]]]");

                _IsConnected = true;

                var token = _cancelationTokenSource.Token;
                connectionTask = Task.Factory.StartNew(() => CheckStatus(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                this.EP("MPD Status", $"Failed to connect to [[[[[{_connectionInfo.Name}]]]]]");
            }

            return result;
        }

        async void CheckStatus(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string response = await _connection.SendStatusCommandAsync();

                // connection closed
                if (response == null)
                    break;

                status.UpdateStatus(response);

                await Task.Delay(1000);
            }

            //await _connection..SendCommandAsync("close");
            this.IsConnected = false;

            this.LP("MPD Status", $"Closed from [[[[[{_connectionInfo.Name}]]]]]");
        }

        internal async Task DisconnectAsync()
        {
            if (this._cancelationTokenSource != null)
                await Task.Run(() =>
                {
                    try
                    {
                        this._cancelationTokenSource?.Cancel();
                        connectionTask?.Wait();
                    }
                    catch (AggregateException /*ex*/)
                    {

                    }
                    finally
                    {
                        this._cancelationTokenSource?.Dispose();
                        this._cancelationTokenSource = null;
                    }
                });
        }
    }
}
