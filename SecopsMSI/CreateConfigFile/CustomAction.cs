using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace CreateConfigFile
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {
            string tempPath = Path.GetTempPath();
            string filename = "secops_installer_logs.txt";
            string filepath = Path.Combine(tempPath, filename);
            try
            {
                    string filePath = "C:\\Program Files (x86)\\Secops Solution CLI\\secops_config.txt";
                    if (File.Exists(filePath))
                    {
                    using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.WriteLine("Created Secops_config file successfully!");
                        }
                    }
                    using (StreamWriter sw = new StreamWriter(filePath))
                        {
                            sw.WriteLine($"{session.CustomActionData["SECOPS_CONFIG"]}");
                        }
                        return ActionResult.Success;
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                        {
                            using (StreamWriter writer = new StreamWriter(fs))
                            {
                                writer.WriteLine("The Secop_Config file could not be found.");
                            }
                        }
                    return ActionResult.Failure;
                    }
            }
            catch (Exception ex)
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"Exception occured: {ex}");
                    }
                }
                return ActionResult.Failure;
            }
        }
    }
}
