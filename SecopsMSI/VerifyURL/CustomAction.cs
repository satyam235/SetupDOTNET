using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace VerifyURL
{
    public class CustomActions
    {   
        [CustomAction]
        public static ActionResult VerifyOnPremURL(Session session)
        {
            string tempPath = Path.GetTempPath();
            string filename = "secops_installer_logs.txt";
            string filepath = Path.Combine(tempPath, filename);
            try
            {
                string baseUrl = session["BASEURL"];

                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"The Base URL provided is {session["BASEURL"]}\n");
                    }
                }

                string scriptContent = $@"
try {{
    $target_url = '{baseUrl}'
    if ($target_url -match '^https?://([\w\d.-]+)(?::(\d+))?') {{
        $domain = $matches[1]
        $port = if ($matches[2]) {{ $matches[2] }} else {{ 80 }}
        $result = Test-NetConnection -ComputerName $domain -Port $port
        if($result.TcpTestSucceeded) {{
            echo $result
            exit 0
        }}
        else
        {{
            echo $result
            exit 1
        }}
    }}
    else {{
        
    echo 'Regular Expression not passed'
        exit 1
    }}
}} catch {{
    echo 'Unknown Exception in try block'
    exit 1
}}
";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{scriptContent}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.WriteLine($"output : {output}");
                        }
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0 )
                    {
                        session["RETURNCODE"] = "1";
                    }
                    else
                    {
                        session["RETURNCODE"] = "0";
                    }
                }
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"Completed Action Verify URL Successfully!");
                        writer.WriteLine("\n");
                    }
                }
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"An Exception Occured : {ex}");
                    }
                }
                session["RETURNCODE"] = "1";            
                return ActionResult.Failure;
            }
        }
    }
}
