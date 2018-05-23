using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aurender.Core.Player.mpd
{
    [DebuggerDisplay("MPDResponse Cmd:{CMD}\n\t Result:{IsOk}\n\t Error:{ErrorMessage}\n\t Command:{ErrorCommand}\n\n Lines: {ResponseLines}")]
    public class MPDResponse
    {
        const string SUCCESS = "OK";
        const string FAIL = "ACK";

        private static readonly Regex EndPattern = new Regex("^(OK)|(ACK \\[(?<error>[^@]+)@(?<command_listNum>[^\\]]+)] {(?<command>[^}]*)} (?<message>.*))");

        public bool IsOk { get; private set; }

        private List<string> Lines = new List<string>();
        public List<string> ResponseLines { get => Lines; }

        public string ErrorMessage { get; protected set; }
        public string ErrorCommand { get; protected set; }

        public bool AddLine(string line)
        {
            line = line ?? string.Empty;
            Match match = EndPattern.Match(line);
            Lines.Add(line);

            if (match.Success)
            {
                ProcessResultMatches(match);
                return this.IsOk;
            }
            else
            {
                return false;
            }
        }

        private void ProcessResultMatches(Match match)
        {
            if (match.Captures[0].Value.Equals(SUCCESS))
            {
                this.IsOk = true;
            }
            else
            {
                FillErrorInfo(match);

                this.IsOk = false;
            }
        }

        [Conditional("DEBUG")]
        private void FillErrorInfo(Match match)
        {
            this.ErrorMessage = match.Groups["message"]?.Value;
            this.ErrorCommand = match.Groups["command"]?.Value;
        }
    }
}
