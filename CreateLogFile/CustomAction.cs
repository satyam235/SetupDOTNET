using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace CreateLogFile
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {
            string tempPath = Path.GetTempPath();
            string filename = "secops_installer_logs.txt";
            string filepath = Path.Combine(tempPath, filename);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("=== Started Custom Action Logging. ===");
                    writer.WriteLine("(M) Doing Custom Action Verify URL\n");
                }
            }
            return ActionResult.Success;
        }
    }
}
