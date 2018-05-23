using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Utility
{
    public static class TimeZoneUtility
    {
        public static readonly string[] TimeZones =
        {
            "Pacific/Midway",
            "US/Samoa",
            "US/Hawaii",
            "US/Alaska",
            "US/Pacific",
            "America/Tijuana",
            "US/Arizona",
            "US/Mountain",
            "America/Chihuahua",
            "America/Mazatlan",
            "America/Mexico_City",
            "America/Monterrey",
            "Canada/Saskatchewan",
            "US/Central",
            "US/Eastern",
            "US/East-Indiana",
            "America/Bogota",
            "America/Lima",
            "America/Caracas",
            "Canada/Atlantic",
            "America/La_Paz",
            "America/Santiago",
            "Canada/Newfoundland",
            "America/Buenos_Aires",
            "Greenland",
            "Atlantic/Stanley",
            "Atlantic/Azores",
            "Atlantic/Cape_Verde",
            "Africa/Casablanca",
            "Europe/Dublin",
            "Europe/Lisbon",
            "Europe/London",
            "Africa/Monrovia",
            "Europe/Amsterdam",
            "Europe/Belgrade",
            "Europe/Berlin",
            "Europe/Bratislava",
            "Europe/Brussels",
            "Europe/Budapest",
            "Europe/Copenhagen",
            "Europe/Ljubljana",
            "Europe/Madrid",
            "Europe/Paris",
            "Europe/Prague",
            "Europe/Rome",
            "Europe/Sarajevo",
            "Europe/Skopje",
            "Europe/Stockholm",
            "Europe/Vienna",
            "Europe/Warsaw",
            "Europe/Zagreb",
            "Europe/Athens",
            "Europe/Bucharest",
            "Africa/Cairo",
            "Africa/Harare",
            "Europe/Helsinki",
            "Europe/Istanbul",
            "Asia/Jerusalem",
            "Europe/Kiev",
            "Europe/Minsk",
            "Europe/Riga",
            "Europe/Sofia",
            "Europe/Tallinn",
            "Europe/Vilnius",
            "Asia/Baghdad",
            "Asia/Kuwait",
            "Africa/Nairobi",
            "Asia/Riyadh",
            "Europe/Moscow",
            "Asia/Tehran",
            "Asia/Baku",
            "Europe/Volgograd",
            "Asia/Muscat",
            "Asia/Tbilisi",
            "Asia/Yerevan",
            "Asia/Kabul",
            "Asia/Karachi",
            "Asia/Tashkent",
            "Asia/Kolkata",
            "Asia/Kathmandu",
            "Asia/Yekaterinburg",
            "Asia/Almaty",
            "Asia/Dhaka",
            "Asia/Novosibirsk",
            "Asia/Bangkok",
            "Asia/Jakarta",
            "Asia/Krasnoyarsk",
            "Asia/Chongqing",
            "Asia/Hong_Kong",
            "Asia/Kuala_Lumpur",
            "Australia/Perth",
            "Asia/Singapore",
            "Asia/Taipei",
            "Asia/Ulaanbaatar",
            "Asia/Urumqi",
            "Asia/Irkutsk",
            "Asia/Seoul",
            "Asia/Tokyo",
            "Australia/Adelaide",
            "Australia/Darwin",
            "Asia/Yakutsk",
            "Australia/Brisbane",
            "Australia/Canberra",
            "Pacific/Guam",
            "Australia/Hobart",
            "Australia/Melbourne",
            "Pacific/Port_Moresby",
            "Australia/Sydney",
            "Asia/Vladivostok",
            "Asia/Magadan",
            "Pacific/Auckland",
            "Pacific/Fiji",
        };

        static readonly IList<string> qobuzCountries = new[]
        {
            "France", "Germany", "Netherland", "Belgium",
            "Spain", "Switzerland", "Portugal", "Italy", "Austria",
            "Luxembourg", "United Kingdom", "Ireland", "North Korea"
        };

        static readonly IList<string> internetRadioCountries = new[]
        {
            "Korea", "South Korea", "United States", "Ireland", "Canada",
            "France", "Germany", "Greece", "Switzerland", "Czech",
            "Netherland", "Belgium", "United Kingdom", "Norway",
            "Portugal", "Russia", "Spain", "Sweden", "Luxembourg", "North Korea"
        };

        static JArray timeZones;
        static TimeZoneUtility()
        {
            string json;

            var assembly = typeof(TimeZoneUtility).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Aurender.Core.timeZones.json"))
            using (var reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }

            timeZones = JArray.Parse(json);
        }

        public static bool IsSupportQobuz(TimeZoneInfo timeZone = null)
        {
            var name = GetCurrentCountryName(timeZone);

            return qobuzCountries.Contains(name);
        }

        public static bool IsSupportInternetRadio(TimeZoneInfo timeZone = null)
        {
            var name = GetCurrentCountryName(timeZone);

            return internetRadioCountries.Contains(name);
        }

        public static bool IsKoreanTimeZone(TimeZoneInfo timeZone = null)
        {
            var name = GetCurrentCountryName(timeZone);

            return name.Contains("Korea");
        }

        public static string GetInternetRadioUrl(TimeZoneInfo timeZone = null)
        {
            var name = GetCurrentCountryName(timeZone);

            switch (name)
            {
                case "South Korea":
                    name = "korea";
                    break;
                case "United States":
                    name = "usa";
                    break;
                case "United Kingdom":
                case "Ireland":
                    name = "uk";
                    break;
                case "Switzerland":
                    name = "swiss";
                    break;
                case "North Korea":
                    name = "test";
                    break;
                default:
                    name = name.ToLower();
                    break;
            }

            var url = $"http://files.aurender.com/radio/{name}.html";
            return url;
        }

        public static string GetPHPCountryName(string id)
        {
            var timeZone = timeZones.FirstOrDefault(x => x["type"].Value<string>().Contains(id));
            return timeZone["territory"].Value<string>();
        }

        static string GetCurrentCountryName(TimeZoneInfo timeZone = null)
        {
            timeZone = timeZone ?? TimeZoneInfo.Local;

            var id = timeZone.Id;
            var code = GetPHPCountryName(id);
            var region = new RegionInfo(code);
            var name = region.EnglishName;

            IARLogStatic.Info("TimeZoneUtility", $"Code: {code}, Name: {name}");

            return name;
        }
    }
}
