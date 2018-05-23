using System;

namespace Aurender.Core.Utility
{
    public static class PlatformUtility
    {
        public static Action<Action> BeginInvokeOnMainThread { get; set; }
    }
}
