using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Web.org.umbraco.update;

namespace Our.Umbraco.SilentUpgrade
{
    public delegate void UpgradeEventHandler(UpgradeEventArgs e);

    public class SilentUpgrade
    {
        public static event UpgradeEventHandler UpgradeStarting;
        public static event UpgradeEventHandler UpgradeComplete;

        public static void FireUpgradeComplete(bool success, string initialVersion, string finalVersion)
        {
            UpgradeComplete?.Invoke(new UpgradeEventArgs
            {
                VersionFrom = initialVersion,
                VersionTo = finalVersion,
                Success = success
            });
        }

        public static void FireUpgradeStarting(string initialVersion, string targetVersion)
        {
            UpgradeStarting?.Invoke(new UpgradeEventArgs
            {
                VersionFrom = initialVersion,
                VersionTo = targetVersion
            });
        }
    }

    public class UpgradeEventArgs
    {
        public bool Success { get; set; }

        public string VersionFrom { get; set; }
        public string VersionTo { get; set; }

        public string Message { get; set; }

    }
}
