using System;
using System.Collections.Generic;
using System.IO;
using Aurender.Core.Setting;
using Aurender.Core.UI;

namespace Aurender.Core.Utility
{
    public static class ImageUtility
    {
        static readonly string[] imageFileNames =
        {
            // default album cover
            "album_default_dark.png",
            "album_default_light.png",
            "album_default_brown.png",

            
            "icon_aurender.png",
            "icon_tidal.png",
            "icon_qobuz.png",
            "icon_bugs.png",
            "icon_melon.png",
            "icon_shoutcast.png",
            "icon_aurender_sel.png",
            "icon_tidal_sel.png",
            "icon_qobuz_sel.png",
            "icon_bugs_sel.png",
            "icon_melon_sel.png",
            "icon_shoutcast_sel.png",

            
            "rating_tidal.png",
            "rating_qobuz.png",
            "rating_bugs.png",
            "rating_melon.png",
            "rating_tidal_sel.png",
            "rating_qobuz_sel.png",
            "rating_bugs_sel.png",
            "rating_melon_sel.png",
        };

        static readonly List<Themes> themes = new List<Themes> { Themes.Dark, Themes.Light, Themes.Brown };

        public static Func<Uri, object> GetImageSourceFromUri { get; set; }
        public static Func<string, object> GetImageSourceFromFile { get; set; }
        public static Func<Stream, object> GetImageSourceFromStream { get; set; }

        public static object GetDefaultAlbumCover()
        {
            var theme = UserSetting.Setting.App.Get(FieldsForAppConfig.CurrentTheme, Themes.Dark);

            var file = imageFileNames[themes.IndexOf(theme)];
            var source = GetImageSourceFromFile(file);
            return source;
        }

        public static object GetServiceIcon(this ContentType contentType, bool isSelected)
        {
            int index = 3;
            index += (int)contentType;
            index += isSelected ? 6 : 0;

            var file = imageFileNames[index];
            var source = GetImageSourceFromFile(file);
            return source;
        }

        public static object GetFavoriteImage(this ContentType contentType, bool isFavorite)
        {
            int index = 15;

            if(contentType == ContentType.TIDAL || contentType == ContentType.InternetRadio)
            {
                index += 0;
            }
            else if (contentType == ContentType.Qobuz)
            {
                index += 1;
            }
            else if (contentType == ContentType.Bugs)
            {
                index += 2;
            }
            else if (contentType == ContentType.Melon)
            {
                index += 3;
            }

            index += isFavorite ? 4 : 0;

            var file = imageFileNames[index];
            var source = GetImageSourceFromFile(file);
            return source;
        }
    }
}
