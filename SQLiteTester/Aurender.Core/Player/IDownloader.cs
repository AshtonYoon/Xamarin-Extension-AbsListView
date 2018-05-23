using System;
using System.Linq;
using System.Collections.Generic;
using Aurender.Core;
using System.Threading;
using System.Threading.Tasks;
using Aurender.Core.Contents;

namespace Aurender.Core.Player
{

    public interface IDownloader
    {
        void DowloadFrom(String url, String to);
        Boolean IsDownloading();

        Int64 TargetFileSize { get; }
        Int64 DownloadedFileSize { get; }
       

        event Action<String> AfterDownload;
    }

}