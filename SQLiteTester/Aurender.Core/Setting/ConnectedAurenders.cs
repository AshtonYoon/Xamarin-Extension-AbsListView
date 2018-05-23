namespace Aurender.Core.Setting
{
    public class ConnectedAurender
    {
        private string sSID;
        private string aurenderName;
        private string iP;

        public string SSID { get => sSID; set => sSID = value; }
        public string AurenderName { get => aurenderName; set => aurenderName = value; }
        public string IP { get => iP; set => iP = value; }

        public ConnectedAurender()
        {

        }

        public ConnectedAurender(string ssid, string name, string ip)
        {
            SSID = ssid;
            AurenderName = name;
            IP = ip;
        }
    }
}
