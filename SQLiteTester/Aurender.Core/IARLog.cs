using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Aurender.Core
{
    public interface IARLog
    {
        bool IsARLogEnabled
        {
            get;
            set;
        }
    }

    public static class IARLogStatic
    {
        #region AppCenter

        public static Action<string, Exception, IDictionary<string, string>> TrackErrorAction { get; set; }

        static void LogHandledException(Exception exception, string prefix, string log, IDictionary<string, string> logs)
        {
            prefix = $"[Error] {prefix ?? "Unnamed error"}";

            if (exception != null)
            {
                if (exception is AggregateException aggregateException)
                {
                    foreach (var ex in aggregateException.InnerExceptions)
                    {
                        LogHandledException(ex, prefix, log, logs);
                    }
                }
                else
                {
                    if (exception.InnerException != null)
                    {
                        LogHandledException(exception, prefix, log, logs);
                    }
                }
            }

            var properties = new Dictionary<string, string>
            {
                { "Message", log }
            };
            if (logs != null)
            {
                foreach (var item in logs)
                {
                    properties.Add(item.Key, item.Value);
                }
            }

            TrackErrorAction?.Invoke(prefix, exception, properties);
        }

        #endregion

        [Conditional("DEBUG")]
        public static void Log(String prefix, String log)
        {
            String pre = prefix != null ? $"Log/{prefix}" : "Log";

            Debug.WriteLine($"{pre}: {log}");
        }

        public static void Info(string prefix, String log)
        {
            String pre = prefix != null ? $"Info/{prefix}" : "Info";

            Debug.WriteLine($"{pre}: {log}");
        }

        public static void Error(String prefix, String log, Exception ex = null, IDictionary<string, string> logs = null)
        {
            String pre = prefix != null ? $"Error/{prefix}" : "Error";

            Debug.WriteLine("================ Error =================");
            Debug.WriteLine($"{pre}: {log}");

            if (logs != null)
            {
                foreach (var item in logs)
                {
                    Debug.WriteLine($"     {item.Key} : {item.Value}");
                }
            }
            if (ex != null)
            {
                if (ex is AggregateException aggr)
                {
                    foreach (var e in aggr.InnerExceptions)
                    {
                        Debug.WriteLine($"     Exception : {e.Message}");
                        Debug.WriteLine($"     Source : {e.StackTrace}");
                    }
                }
                else
                {
                    Debug.WriteLine($"     Exception : {ex.Message}");
                    Debug.WriteLine($"     Source : {ex.StackTrace}");
                }
            }

            LogHandledException(ex, prefix, log, logs);

            Debug.WriteLine("========================================");
        }

        [Conditional("DEBUG")]
        public static void EnableLog(this IARLog source)
        {
            source.IsARLogEnabled = true;
        }

        [Conditional("DEBUG")]
        public static void L(this IARLog source, String log, [CallerFilePathAttribute] String callerFile = "", [CallerMemberName] String callerName = "")
        {
            if (source.IsARLogEnabled)
            {
                //String pf = $"[{callerFile}:{callerName}]";
                String pf = callerName;
                Log(pf, log);
            }
        }

        [Conditional("DEBUG")]
        public static void LP(this IARLog source, String prefix, String log, [CallerFilePathAttribute] String callerFile = "", [CallerMemberName] String callerName = "")
        {
            if (source.IsARLogEnabled)
            {
                //String pf = $"[{prefix}][{callerFile}:{callerName}]";
                String pf = $"{prefix}-{callerName}";
                Log(pf, log);
            }
        }

        public static void Info(this IARLog source, String prefix, String log)
        {
            Info(prefix, log);
        }

        public static void E(this IARLog source, String log, Exception ex = null, IDictionary<string, string> logs = null, [CallerFilePath] String callerFile = "", [CallerMemberName] String callerName = "", [CallerLineNumber] int callerLine = 0)
        {
            var fileName = callerFile.Substring(callerFile.LastIndexOf("\\") + 1);
            Error("Error", log, ex, new Dictionary<string, string>(logs ?? new Dictionary<string, string>())
            {
                { "Caller file name", fileName },
                { "Caller member name", callerName },
                { "Caller line number", callerLine.ToString() }
            });
        }

        public static void EP(this IARLog source, String prefix, String log, Exception ex = null, IDictionary<string, string> logs = null, [CallerFilePath] String callerFile = "", [CallerMemberName] String callerName = "", [CallerLineNumber] int callerLine = 0)
        {
            var fileName = callerFile.Substring(callerFile.LastIndexOf("\\") + 1);
            Error(prefix, log, ex, new Dictionary<string, string>(logs ?? new Dictionary<string, string>())
            {
                { "Caller file name", fileName },
                { "Caller member name", callerName },
                { "Caller line number", callerLine.ToString() }
            });
        }
    }
}
