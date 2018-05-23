using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Aurender.Core.Data.Folder
{

    [DebuggerDisplay("File[{IsSong}] : [{Name}]")]
    internal class FileItem : IFolderUIItem
    {
        static Regex rgFileExt = new Regex("\\.(mp3|flac|m4a|wav|wave|ogg|oga|aac|ape|aif|aiff|dff|dsf)$");

        internal FileItem(String fileName)
        {
            Name = fileName;

        }

        public String Name { get; protected set; }

        public bool IsFolder => false;

        public bool IsSong => rgFileExt.IsMatch(Name);
    }

}