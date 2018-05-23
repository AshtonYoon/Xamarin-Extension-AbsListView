using System;
using System.Threading.Tasks;

namespace Aurender.Core.Player.DeviceControl
{

    public static class AurenderManualBackup
    {
        public static async Task<bool> StartBackupPlaylistAndRateToMusic1Backup(this IAurender aurender)
        {
            String url = "/wapi/contents/backupPlaylistAndRate";

            var result = await DeviceControlUtility.GetResponse(aurender.ConnectionInfo, url).ConfigureAwait(false);

            return result.isSucess;
        }

        public static async Task<bool> StartRestorePlaylistAndRateToMusic1Backup(this IAurender aurender)
        {

            String url = "/wapi/contents/backupPlaylistAndRate?restore=1";
            var result = await DeviceControlUtility.GetResponse(aurender.ConnectionInfo, url).ConfigureAwait(false);

            return result.isSucess;
        }

        public static async Task<Boolean> FormatEmptyInternalHDDs(this IAurender aurender)
        {
            String url = "/php/system?format=format";
            var result = await DeviceControlUtility.GetResponse(aurender.ConnectionInfo, url).ConfigureAwait(false);

            return result.isSucess;
        }
    }
}