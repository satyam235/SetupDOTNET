using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using WixToolset.Dtf.WindowsInstaller;
using System.Diagnostics;

namespace ActionUninstall
{
    public class CustomActions
    {

        public static string sepChar = Path.DirectorySeparatorChar.ToString();
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {

            session.Log("Begin CustomAction1");
            string A_ID = "agentID";

            string ConfigFile = "C:\\Program Files (x86)\\Secops Solution CLI\\secops_config.txt";

            if (File.Exists(ConfigFile))
            {
                // Open the file to read from
                using (StreamReader sr = File.OpenText(ConfigFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Split the line into key and value
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0];
                            string value = parts[1];
                            if (key == "A_ID")
                            {
                                A_ID = value;
                                // Process the key-value pair
                                session.Log($"Key: {key}, Value: {value}");
                                break;
                            }
                        }
                    }
                }
            }

            string folderPath = session["INSTALLFOLDER"];
            if (!folderPath.EndsWith(sepChar))
            {
                folderPath += sepChar;
            }
            string exePath = folderPath + "secops_uninstaller.exe";
            session.Log("checking exePath");
            session.Log(exePath);
            if (File.Exists(exePath))
            {
                session.Log("exe path file exists");
            }
            else
            {
                session.Log("exe path file does not exist");
            }
            session.Log($"printing agent id{A_ID}");
                try
            {
                ProcessStartInfo process = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{A_ID}",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                session.Log("");
                using (Process process1 = new Process())
                {
                    process1.StartInfo = process;
                    process1.Start();

                    session.Log($"output: {process1.StandardOutput.ReadToEnd()}");
                    session.Log($"error: {process1.StandardError.ReadToEnd()}");
                    process1.WaitForExit();
                }
                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }
    }
}
