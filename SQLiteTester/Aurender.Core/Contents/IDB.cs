using System;
using System.Collections.Generic;

using SQLite;

using Aurender.Core.Player;

namespace Aurender.Core.Contents
{
    public interface IWindowedDataWatingDelegate
    {
        void ShowWaitingPopupWith(String message = "");
        void HideWaitingPopup();

        EViewType ActiveViewType { get; }
    }

    public interface IDB
    {
        SQLiteConnection CreateConnection();
        SQLiteConnection CreateRatingConnection();
        //SQLiteConnection CreateFullMutelConnection(); 

        bool IsOpen();
        void Close();

        String DBVersion { get;  }
        String RateVersion { get;  }

        IList<string> FolderFilters { get; }

        IWindowedDataWatingDelegate popupDelegate { get; }

        //void CheckUpgradePeriodically();
        void StopChecking();

        IVersionChecker DBVersionChecker { get; }
        IVersionChecker RateVersionChecker { get; }

        void ResetDBVersion();
    }
}
