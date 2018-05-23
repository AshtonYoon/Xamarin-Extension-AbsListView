using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurender.Core.Utility
{
    public static class TimerUtility
    {
        public static Action<TimeSpan, Func<Task<bool>>> TimerFunc { get; set; }
        public static Action<Func<Task<bool>>> TimerUnsetFunc { get; set; }

        private static IList<Func<Task<bool>>> functions = new List<Func<Task<bool>>>();
        private static IDictionary<Func<Task<bool>>, bool> scheduleResult = new Dictionary<Func<Task<bool>>, bool>();
        private static readonly Object LockObj = new object();

        public static void SetTimer(int interval, Func<Task<bool>> timerAction)
        {
            var timeSpan = TimeSpan.FromSeconds(interval);

            lock (LockObj)
            {
                if (TimerFunc == null)
                {
                    if (functions.Contains(timerAction))
                    {
                        return;
                    }

                    //IARLogStatic.Info("Scheduler", $"Add a schduledTask {interval}, T[{timerAction.Target}, f[{timerAction}]");
                    functions.Add(timerAction);
                    scheduleResult.Add(timerAction, true);

                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                var shouldContinue = scheduleResult[timerAction];
                                if (!shouldContinue)
                                {
                                    IARLogStatic.Info("Scheduler", $"Last result for T[{timerAction.Target}, f[{timerAction}] was failed, so we quit.");
                                    break;
                                }

                                //IARLogStatic.Info("Scheduler", $"Call {interval}, T[{timerAction.Target}, f[{timerAction}]");
                                var result = timerAction();

                                bool newShouldContinue = false;
                                if (result.IsFaulted)
                                {
                                    IARLogStatic.Error("Scheduler", "Failed to run schduledTask ", result.Exception);
                                }
                                else
                                {
                                    newShouldContinue = result.Result;
                                }
                                //IARLogStatic.Info("Scheduler", $"New result for T[{timerAction.Target}, f[{timerAction}] is {newShouldContinue}");
                                scheduleResult[timerAction] = newShouldContinue;
                            }
                            catch (Exception ex)
                            {
                                IARLogStatic.Error("Scheduler", $"Failed to run schduledTask", ex);
                                break;
                            }
                            await Task.Delay(timeSpan);
                        }
                    });
                }
                else
                {
                    //  throw new InvalidOperationException("Enable below line");
                    TimerFunc(timeSpan, timerAction);
                }
            }
        }

        public static void UnsetTimer(Func<Task<bool>> timerAction)
        {
            if (TimerFunc == null)
            {
                throw new ArgumentNullException("Please set TimerUtility.TimerFunc");
            }

            TimerUnsetFunc(timerAction);
        }
    }
}
