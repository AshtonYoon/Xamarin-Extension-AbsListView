using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurender.Core
{
    public interface IFolderUIItem
    {
        bool IsFolder { get; }

        bool IsSong { get; }

        String Name { get; }

    }


    public interface IFileBrowser
    {
        /// <summary>
        ///  This is used to highlight the folder name when user press go up
        ///  If it is the root folder, it will return empty String.
        /// </summary>
        string GetLastSelectedFolder();
        String GetCurrentFolderFullPath();
        String GetCurrentFolderName();
        String GetParentFolderName();
        string GetFullPath(IFolderUIItem item);

        Task<bool> CreateFolder(String name);

        bool IsRootFolder();
        bool HasFile();
        bool IsWorking();

        List<IFolderUIItem> Folders { get; }
        List<IFolderUIItem> Files { get; }


        /// <summary>
        /// When you request, it will create a new List of <IFolderUIItem>IFolderUIItem</IFolderUIItem>
        /// </summary>
        /// <returns></returns>
        IList<IFolderUIItem> GetAllItems();

        int Count { get; }
        int FolderCount { get; }
        int FileCount { get; }

        bool IsCurrentFolderUSB();
        bool IsCurrentFolderNAS();

        Task<bool> MoveToRoot();

        Task<bool> EnterTo(String folderName);
        /// <summary>
        /// Back to up folder
        /// </summary>
        Task<bool> GoUp();

        Task<bool> MoveTo(String fullPath);
        Task<bool> MoveToContainerFolder(String filePath);

        event EventHandler<String> ContentsUpdated;



    }
}