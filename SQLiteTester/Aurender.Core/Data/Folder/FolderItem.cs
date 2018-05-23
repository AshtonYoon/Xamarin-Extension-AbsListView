using System;
using System.Diagnostics;

namespace Aurender.Core.Data.Folder
{

    [DebuggerDisplay("Folder : [{Name}]")]
    internal class FolderItem : IFolderUIItem
    {
        internal FolderItem(String name)
        {
            Name = name;
        }

        public String Name { get; protected set; }

        public bool IsFolder => true;

        public bool IsSong => false;
    }
}