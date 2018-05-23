using System;
using SQLite;

namespace Aurender.Core.Data.DB.TableMapping
{
    internal class FolderFilterRow
    {
        [Column("filter_id")]
        public int FilterID { get; set; }

        [Column("path")]
        public String FolderName { get; set; }

        [Column("filter_order")]
        public int Order { get; set; }
    }
}
