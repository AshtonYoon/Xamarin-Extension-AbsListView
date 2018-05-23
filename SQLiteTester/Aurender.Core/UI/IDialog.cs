using System;
namespace Aurender.Core.UI
{
    public interface IDialog
    {
        void ShowDialog(String title, String message,
                        String button1 = null, String button2 = null,
                        Boolean disableBt1 = true, Boolean diableBt2 = true,
                        Boolean showCancel = true,
                        Int32 hideAfterSec = 0,
                        Action afterBtn1 = null,
                        Action afterBtn2 = null,
                        Action afterCancel = null );
    }
}
