using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Xml;
using WixToolset.Dtf.WindowsInstaller;

namespace VerifyLicenseKey
{
    public class CustomActions
    {
        public static string BASE_URL = @"https://api.app.secopsolution.com/secops/v1.0/";
        [CustomAction]
        public static ActionResult ActionVerifyLicenseKey(Session session)
        {
            string tempPath = Path.GetTempPath();
            string filename = "secops_installer_logs.txt";
            string filepath = Path.Combine(tempPath, filename);
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        //writer.WriteLine("Started Custom Action Logging.");
                        writer.WriteLine("(M) Doing Custom Action Verify License Key\n");
                        writer.WriteLine($"The License Key provided is {session["LICENSEKEY"]}");
                    }
                }
                session["VALIDLICENSE"] = "999";
                string licenseKey = session["LICENSEKEY"];
                string baseURL = session["BASEURL"];
                session.Log("started Verifying LicenseKey");
                if (!string.IsNullOrEmpty(baseURL))
                {
                    BASE_URL = baseURL;
                }
                if (!BASE_URL.EndsWith("/"))
                {
                    BASE_URL += "/";
                }
                if (!BASE_URL.Contains("secops/v1.0/"))
                {
                    BASE_URL += "secops/v1.0/";
                }
                session.Log("URL is valid");
                
                string url = BASE_URL + "agent/validate_license_key";
                session.Log($"url is:{url}");
                session.Log($"License Key is:{licenseKey}");

                string fullURL = $@"{url}?key={licenseKey}";

                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"Full URL is : {fullURL}");
                    }
                }

                string script = $@"
try {{
    $response = Invoke-WebRequest -Uri {fullURL} -Method GET -ErrorAction Stop

    if ($response.StatusCode -eq 200) {{
        echo $response
        exit 0

    }} else {{
        echo $response
        exit 1

    }}
}} catch {{
    echo 'An exception occurred while Invoking WebRequest to the URL'
    exit 1

}}
";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    process.WaitForExit();

                    string errors = process.StandardError.ReadToEnd();
                    string output = process.StandardOutput.ReadToEnd();

                    using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.WriteLine($"output : {output}");
                        }
                    }

                    if (process.ExitCode != 0)
                    {
                        session.Log($"Error: {errors}");
                        session["VALIDLICENSE"] = "1";
                        session["PHASE"] = "1";
                        return ActionResult.Failure;
                    }
                    else
                    {
                        session["VALIDLICENSE"] = "0";
                        session["PHASE"] = "3";
                        using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                        {
                            using (StreamWriter writer = new StreamWriter(fs))
                            {
                                writer.WriteLine($"Completed Action Verify License Key Successfully!");
                                writer.WriteLine("\n");
                            }
                        }
                        return ActionResult.Success;
                    }
                }
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
                session["VALIDLICENSE"] = "1";
                session["PHASE"] = "1";
                return ActionResult.Failure;
            }
        }
    }
}
