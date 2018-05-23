using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Utility;

namespace Aurender.Core.Player.DeviceControl
{
    public class MountedNAS
    {
        public readonly String Name;
        public readonly String IPAddress;
        public readonly String FoldeToConnect;
        public readonly String MountedPath;
        public readonly String UserName;

        public bool IsConnected;

        internal MountedNAS(String lineFromServer)
        {
            String[] fields = lineFromServer.Split(';');

            if (fields.Count() == 7)
            {
                this.Name = fields[1];
                this.IPAddress = fields[2];
                this.FoldeToConnect = fields[3];
                this.UserName = fields[4];
                this.IsConnected = fields[5].Equals("Mounted");
                this.MountedPath = $"/{fields[6]}";
            }
            else
            {
                this.Name = "Failed";
                this.IPAddress = "Wong format";
            }
        }

        public async Task<bool> Mount(IAurenderEndPoint aurender)
        {
            String url = $"/php/smbMnt?auto=1&path={FoldeToConnect.URLEncodedString()}&sn={Name.URLEncodedString()}&user={UserName.URLEncodedString()}";

            var result = await DeviceControlUtility.GetResponse(aurender, url).ConfigureAwait(false);

            return result.isSucess;
        }

        public override string ToString()
        {
            return $"MountedNAS : {Name}({IPAddress}/{UserName})[{FoldeToConnect}] => [{MountedPath}]";
        }
    }

    public class NASDevice  
    {
        public readonly String Name;
        public readonly String IPAddress;

        public String UserID;
        public String Password;
        public List<String> Shares;

        public String ErrorMessage;

        internal NASDevice(String dataLiine)
        {
            var fields = dataLiine.Split(';');

            if (fields.Count() == 3)
            {
                this.Name = fields[0];
                this.IPAddress = fields[1];
                this.Shares = new List<string>();
                this.UserID = "";
            }
            else
            {
                this.Name = "N/A";
                this.IPAddress = "No IP Info";
                this.UserID = "";
            }
            Password = String.Empty;
        }

        public async Task<bool> LoadShares(IAurenderEndPoint endPoint)
        {
            String url;
            if (Password == null)
            {
                Password = String.Empty;
            }
            if (UserID != null && UserID.Length > 0)
                url = $"/php/smbShare?ip={IPAddress}&type=SMB&name={UserID.URLEncodedString()}&pwd={Password.URLEncodedString()}";
            else
                url = $"/php/smbShare?ip={IPAddress}&type=SMB";

            List<String> lists = new List<string>();
            var result = await DeviceControlUtility.GetResponse(endPoint, url).ConfigureAwait(false);
            if (result.isSucess)
            {
                var regex = new Regex(@"<pre class=share>([\s\S]*?)</pre>");
                var match = regex.Match(result.responseString);
                if (match.Success && match.Groups.Count > 1)
                {
                    var line = match.Groups[1].Value;

                    var lines = line.Split('\n');
                    lists.AddRange(lines);
                }
            }

            this.Shares = lists;

            return result.isSucess;
        }


        public async Task<bool> TryMount(IAurenderEndPoint aurender, String folderToConnect, String userName, String password)
        {
            return await AddNASServerToAurender(aurender, this.Name, folderToConnect, userName, password);
        }

        public static async Task<bool> AddNASServerToAurender(IAurenderEndPoint aurender, String name, String folder, String user, String password)
        {
            String url;

            if (password != null && password.Length > 0)
                url = $"/php/smbMnt?auto=1&path={folder.URLEncodedString()}&sn={name}&user={user.URLEncodedString()}&pwd={password.URLEncodedString()}";
            else
                url = $"/php/smbMnt?auto=1&path={folder.URLEncodedString()}&sn={name}&user={user.URLEncodedString()}";

            var result = await DeviceControlUtility.GetResponse(aurender, url).ConfigureAwait(false);
            if (result.isSucess)
                return result.responseString.Contains("<pre><br>Mounted</pre>");

            return false;
        }

        public override string ToString()
        {
            return $"MountedNAS : {Name}({IPAddress})";
        }
    }


    public class AurenderConnectedNASServerSetting : DeviceControlBase
    {
        public IList<MountedNAS> MountedNASs;
        
        public AurenderConnectedNASServerSetting(IAurender aurender) : base(aurender) { }

        public override async Task<bool> LoadInformation()
        {
            var status = await GetResponse("/php/smbAutoMntList").ConfigureAwait(false);

            IList<MountedNAS> list = new List<MountedNAS>();
            if (status.isSucess)
            {
                var regex = new Regex(@"<pre class=smbList>([\s\S]*?)</pre>");

                var match = regex.Match(status.responseString);
                if (match.Success)
                {
                    var mountedNASs = Regex.Split(match.Groups[1].Value, @"\n");
                    foreach(var line in mountedNASs)
                    {
                        MountedNAS device = new MountedNAS(line);

                        list.Add(device);
                    }                    
                }
            }

            this.MountedNASs = list;

            return status.isSucess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nasName"></param>
        /// <param name="folderToMount"></param>
        /// <param name="userName"></param>
        /// <param name="passwd"></param>
        /// <returns></returns>
        public async Task<bool> AddNAS(String nasName, String folderToMount, String userName, String passwd)
        {
            return await NASDevice.AddNASServerToAurender(aurender, nasName, folderToMount, userName, passwd);
        }

        public async Task<bool> UpdateNAS(String nasName, String folderToMount, String userName, String passwd)
        {
            return await AddNAS(nasName, folderToMount, userName, passwd).ConfigureAwait(false);
        }
        
        public async Task<bool> Remove(String nasName, String folderToMount)
        {
            String url = $"/php/smbListEdit?op=rm&sn={nasName}&path={folderToMount.URLEncodedString()}";

            var status = await GetResponse(url).ConfigureAwait(false);

            return status.isSucess;
        }
        
        public async Task<List<NASDevice>> BrowseNASDevices()
        {
            List<NASDevice> list = new List<NASDevice>();
            String url = "/php/smbList";
            var status = await GetResponse(url).ConfigureAwait(false);

            if (status.isSucess)
            {
                var regexList = new Regex(@"<pre class=smbList>([\s\S]*?)</pre>");

                var match = regexList.Match(status.responseString);

                if (match.Success && match.Groups.Count > 1)
                {
                    var nasLines = match.Groups[1].Value.Split('\n');

                    foreach(var line in nasLines)
                    {
                        if (line.Length == 0)
                            continue;

                        NASDevice device = new NASDevice(line);

                        list.Add(device);
                    }
                }
            }

            return list;
        }
    }

}