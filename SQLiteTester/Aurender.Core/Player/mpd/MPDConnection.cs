using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Aurender.Core.Player.mpd
{
    class MPDConnection : IDisposable, IARLog
    {
        protected Stream _networkStream;
        protected TcpClient _tcpClient;

        public Int32 BufferSize { get; set; } = 512;

        public string Version { get; private set; }

        internal async Task<Boolean> InitAsync(IAurenderEndPoint end)
        {
            return await InitAsync(end.IPV4Address, end.Port);
        }

        internal async Task<Boolean> InitAsync(string hostname, int port)
        {
            this.L("MPD", $"Connecting to {hostname}:{port}");

            _tcpClient = new TcpClient();

            try
            {
                await _tcpClient.ConnectAsync(hostname, port).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                this.EP("Connection", $"Failed to connect : ", ex);
                return false;
            }
            _networkStream = _tcpClient.GetStream();

            var mpdResponse = await ReadResponseAsync().ConfigureAwait(false);
            if (!mpdResponse.IsOk)
            {
                this.EP(prefix: "Connection", log: $"Failed to connect : {mpdResponse.ErrorMessage}");

                return false;
            }

            Version = mpdResponse.ResponseLines[0];

            this.LP("MPD", $"Connected to MPD version {Version}");

            return true;
        }

        public async Task<MPDResponse> SendCommandAsync(string command, params string[] arguments)
        {
            var commandline = new StringBuilder(command);
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    commandline.Append($" {argument}");
                }
            }
            commandline.AppendLine("");

            this.LP("MPD", $"Sending command: {commandline}");
            var buffer = Encoding.UTF8.GetBytes(commandline.ToString());

            if(_networkStream.CanWrite)
                await _networkStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return await ReadResponseAsync().ConfigureAwait(false);
        }

        protected virtual async Task<MPDResponse> ReadResponseAsync()
        {
            var result = new MPDResponse();

            using (var reader = new StreamReader(_networkStream, Encoding.UTF8, true, BufferSize, true))
            {
                string line;
                do
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }
                while (!result.AddLine(line));

                //this.LP("MPD", $"Received answer: {string.Join(Environment.NewLine, result.ResponseLines)}");
            }

            return result;
        }

        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        #region IDisposable

        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _networkStream.Dispose();
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion
    }
}