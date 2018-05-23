using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace Aurender.Core.Player.mpd
{
    class MPDConnectionForStatus : MPDConnection
    {
        internal MPDConnectionForStatus()
        {
            ((IARLog)this).IsARLogEnabled = false;
        }

        private readonly byte[] _statusCommand = Encoding.UTF8.GetBytes("status\n");

        public async Task<string> SendStatusCommandAsync()
        {
            // this.LP("MPD Status", "Sending command: status");
            StringBuilder sb = new StringBuilder(256);

            try
            {
                //TODO: handling socket closed exception
                await _networkStream.WriteAsync(_statusCommand, 0, _statusCommand.Length).ConfigureAwait(false);

                using (var reader = new StreamReader(_networkStream, Encoding.UTF8, true, BufferSize, true))
                {
                    string line = string.Empty;
                    do
                    {
                        line = await reader.ReadLineAsync().ConfigureAwait(false);

                        sb.AppendLine(line);
                    } while (line != null && line != "OK");

                    //this.LP("MPD", $"Received answer: {string.Join(Environment.NewLine, sb)}");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                this.EP("MPDConnectionForStatus", $"Failed to get status: {sb}", ex);
                return null;
            }
        }
    }
}