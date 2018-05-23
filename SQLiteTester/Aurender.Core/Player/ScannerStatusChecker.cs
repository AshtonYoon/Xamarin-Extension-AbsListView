using System;

namespace Aurender.Core.Player
{

    public interface ScannerStatusChecker
    {
        void StartScan(ScannerScanningMode mode);
        void PauseScanning();
        void ResumeScanning();

        ScannerStatus CurrentStatus { get; }

        String TargetPath { get; }

        void CheckStatus();

		event Action<ScannerStatus> GotStatus;
    }

}