using System;
using System.Linq;
using System.Collections.Generic;

namespace Aurender.Core.UI
{

    public static class IDialogExtension
    {
        public static void ShowInfo(this IDialog dialog, String message)
        {
            String title = "Info";
            dialog.ShowDialog(title, message);
        }

        public static void ShowError(this IDialog dialog, String message)
        {
            String title = "Error";
            dialog.ShowDialog(title, message);
        }
    }

}