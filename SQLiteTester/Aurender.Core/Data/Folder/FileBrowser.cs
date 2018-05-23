using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Player;
using Aurender.Core.Player.DeviceControl;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.Folder
{

    [DebuggerDisplay("FolderBrower : [{GetCurrentFolderFullPath()}]")]
    internal class FileBrowser : IFileBrowser
    {
        public FileBrowser(IAurenderEndPoint aurender)
        {
            objLock = new object();
            EndPoint = aurender;
            paths = new List<String>
            {
                "Root"
            };
            this.Folders = new List<IFolderUIItem>();
            this.Files = new List<IFolderUIItem>();
        }

        public bool IsWorking() => isWorking;

        public List<IFolderUIItem> Folders { get; private set; }

        public List<IFolderUIItem> Files  { get; private set; }

        public int Count => FolderCount + FileCount;
        public int FolderCount => Folders.Count;
        public int FileCount => Files.Count;
        public bool HasFile() => Files.Count > 0;
        public bool IsRootFolder() => paths.Count == 1;

        public event EventHandler<String> ContentsUpdated;

        public IList<IFolderUIItem> GetAllItems()
        {
            List<IFolderUIItem> items = new List<IFolderUIItem>(Folders);
            items.AddRange(Files);
            return items;
        }

        public string GetCurrentFolderFullPath()
        {
            String result = String.Join("/", this.paths.ToArray());

            if (IsCurrentFolderNAS())
            {
                Regex replace = new Regex("^Root/(NAS|SMB)/");
                result = replace.Replace(result, "/mnt/smb/");
            }
            else
            {
                Regex replace = new Regex("^Root/");
                result = replace.Replace(result, "");
            }


            return result;
        }

        public string GetFullPath(IFolderUIItem item)
        {
            String fullPath = $"{GetPathForURL()}/{item.Name}";

            return fullPath;
        }

        public async Task<bool> CreateFolder(String name)
        {
            var creationResult = false;
            

            if (Folders.Any((item) => item.Name.Equals(name))) {
                IARLogStatic.Error("SmartCopy", "There is a folder with same name");
                return creationResult;
            }

            var prohibited = new char[] { '*', '\"', ';', ':', '?', '|', '<', '>', '\\', '/'};
            var newName = name;
            foreach (var ch in prohibited) {
                newName = newName.Replace(ch, '_');
            }

            newName = newName.Replace("%", "%26");
            newName = $"/hdds/{GetCurrentFolderFullPath()}/{newName}"; 
            
            var url = this.EndPoint.WebURLFor($"wapi/contents/hdds/1/create");

            try
            {
                isWorking = true;   
                IList<KeyValuePair<String, String>> postData = new List<KeyValuePair<String, String>>()
                {
                    new KeyValuePair<string, string>("p", newName)
                };

                using (var result = await Utility.WebUtil.GetResponseByPostDataAsync(url, postData))
                {

                    if (result.IsSuccessStatusCode)
                    {
                        creationResult = true;
                    }
                    else
                    {
                        var message = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                        IARLogStatic.Error("SmartCopy", $"Failed to create folder :[{result.StatusCode}] {message}");
                    }
                }
            }
            catch (Exception er)
            {
                Debug.WriteLine(er.Message);
            }
            finally
            {
                isWorking = false;

                await Refresh().ConfigureAwait(false);
            }

            return creationResult;
        }

        private async Task<bool> Refresh()
        {
            String path = GetPathForURL();
            return await MoveToFolder(path).ConfigureAwait(false);
        }

        private String GetPathForURL()
        {
            if (paths.Count == 1)
                return ".";

                String result = String.Join("/", this.paths.ToArray());
            if (IsCurrentFolderNAS())
            {
                if (paths.Count == 2)
                    return "NAS";

                Regex replace = new Regex("^Root/(NAS|SMB)/");
                result = replace.Replace(result, "NAS/");
            }
            else if (IsCurrentFolderUSB())
            {
                if (paths.Count == 2)
                    return "USB";

                Regex replace = new Regex("^Root/USB/");
                result = replace.Replace(result, "USB/");
            }
            else
            {
                Regex replace = new Regex("^Root/");
                result = replace.Replace(result, "");
            }


            return result;
        }
 
        public string GetCurrentFolderName() => paths.Last();

        public string GetLastSelectedFolder() => lastSelectedFolder;

        public string GetParentFolderName()
        {
            if (paths.Count < 3)
            {
                return paths[0];
            }

            return paths[paths.Count - 2];
        }

        public bool IsCurrentFolderNAS()
        {
            if (paths.Count > 1)
            {
                return paths[1] == "NAS";
            }
            return false;
        }

        public bool IsCurrentFolderUSB()
        {
            if (paths.Count > 1)
            {
                return paths[1] == "USB";
            }
            return false;
         }


        public async Task<bool> MoveToRoot()
        {
            lastSelectedFolder = String.Empty;

            return await MoveToFolder("");
        }

        public async Task<bool> EnterTo(string folderName)
        {
            if (ContainsSubfoler(folderName))
            {
                String path = GetPathForURL();
                path = path + "/" + folderName;

                bool sucess = await MoveToFolder(path).ConfigureAwait(false);

                return sucess;
            }

            return false;
        }
        public async Task<bool> GoUp()
        {
            lastSelectedFolder = this.GetCurrentFolderName();
            String path = GetPathForURL();
            int position = path.LastIndexOf(lastSelectedFolder);
            if (position <= 0)
                path = ".";
            else
                path = path.Remove(position - 1);

            return await MoveToFolder(path).ConfigureAwait(false);       
        }


        private bool ContainsSubfoler(string folderName)
        {
            return Folders.Any((folder) => folder.Name.Equals(folderName));
        }

        public async Task<bool> MoveTo(string fullPath)
        {
            return await MoveToFolder(fullPath).ConfigureAwait(false);
        }

        public async Task<bool> MoveToContainerFolder(string filePath)
        {
            String fileName = Path.GetFileName(filePath);
            String folderPath = filePath.Replace(fileName, "");

            return await MoveToFolder(folderPath, fileName).ConfigureAwait(false);
        }

        private bool isWorking;
        private readonly Object objLock;
        private readonly IAurenderEndPoint EndPoint;
        private readonly List<String> paths;
        private String lastSelectedFolder = String.Empty;


        private Regex rxFolderFiles = new Regex(@"<pre class='folders'>([\s\S]*?)</pre><pre class='files'>([\s\S]*?)</pre>");

        public async Task<bool> MoveToFolder(String folderPath, String fileNameToSelect = "")
        {
            bool sucess = false;

            while (isWorking)
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
            lock (objLock)
            {
                this.paths.RemoveRange(1, paths.Count - 1);

                if (folderPath.Length == 0)
                {

                }
                else
                {
                    if (folderPath.StartsWith("/"))
                        folderPath = folderPath.Remove(0, 1);

                    string[] items = folderPath.Split('/');
                    if(string.IsNullOrEmpty(items.LastOrDefault()))
                        items = items.Take(items.Count() - 1).ToArray();
                    
                    paths.AddRange(items.Where((name) => !name.Equals(".")));
                }
                this.Folders = new List<IFolderUIItem>();
                this.Files = new List<IFolderUIItem>();
            }
            String param = string.Empty;
            String path = folderPath;
            const int PATH_OFFSET_FOR_USB_AND_NAS = 3;
            if (IsCurrentFolderNAS())
            {
                param = $"&s=on";
                if (folderPath == "./NAS")
                    folderPath = ".";
                else
                    folderPath = folderPath.Substring(PATH_OFFSET_FOR_USB_AND_NAS); 
            }
            else if (IsCurrentFolderUSB())
            {
                param = $"&s=usb";
                if (folderPath == "./USB")
                    folderPath = ".";
                else
                    folderPath = folderPath.Substring(PATH_OFFSET_FOR_USB_AND_NAS);
            }
            else
            {
                if (folderPath.Length == 0)
                {
                    folderPath = ".";
                }
            }
            if (folderPath.StartsWith("./"))
            {
                folderPath = folderPath.Substring(2);
            }

            string urlToQeury = $"php/lsHdds?p={folderPath.URLEncodedString()}{param}";

            try
            {
                isWorking = true;
                var result = await DeviceControlUtility.GetResponse(EndPoint, urlToQeury).ConfigureAwait(false);

                if (result.isSucess)
                {
                    var mataches = rxFolderFiles.Match(result.responseString);

                    if (mataches.Success)
                    {
                        var newFolders = mataches.Groups[1].Value.Split('\n')
                            .Where((name) => name.Length > 0);
                        var x = newFolders.Select<String, FolderItem>((name) => new FolderItem(name));

                        if (x.Count() > 0)
                            this.Folders.AddRange(x);

                        var newFiles = mataches.Groups[2].Value.Split('\n')
                            .Where((name) => name.Length > 0);
                        var y = newFiles.Select<String, FileItem>((name) => new FileItem(name));

                        if (y.Count() > 0)
                            this.Files.AddRange(y);

                        sucess = true;
                    }
                }
            }
            catch (Exception er)
            {
                Debug.WriteLine(er.Message);
            }
            finally
            {
                isWorking = false;
                this.ContentsUpdated?.Invoke(this, fileNameToSelect);
            }

            return sucess;
        }
    }

}