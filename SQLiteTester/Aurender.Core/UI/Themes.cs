using System;
using System.Collections.Generic;
using System.Text;

namespace Aurender.Core.UI
{
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Themes
    {
        Dark,
        Light,
        Brown
    }
}
