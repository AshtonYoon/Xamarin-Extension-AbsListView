using System;

namespace Aurender.Core.Player
{

    public interface IVersionChecker
    {
        void StartCheckPeriodically();

        /// <summary>
        /// It will stop. To resume, call CheckVersion again.
        /// </summary>
        void StopChecking();
        void ResumeChecking();
        void ResumeCheckingWithImediatCheck();


        /// <summary>
        /// If failed to check or got error from URL, 
        /// String argument will be null
        /// </summary>
		event EventHandler<String> OnVersionChecked;

        String CurrentVersion { get; }
        bool IsWorking { get; }

        void ClearVersion();
    }

}