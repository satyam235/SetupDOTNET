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
        public static string REGISTRATION_ENDPOINT = "agent/register_agent_fingerprint";
        public static string BINARY_DIRECTORY = "C:\\Program Files (x86)\\Secops Solution CLI";
        public static string CONFIG_FILE_PATH = "C:\\Program Files (x86)\\Secops Solution CLI\\secops_config.txt";
        public static string LOGFILE_PATH = Path.Combine(Path.GetTempPath(), "secops_registration_module_logs.log");

        public static string tempPath = Path.GetTempPath();
        public static string filename = "secops_installer_logs.txt";
        public static string filepath = Path.Combine(tempPath, filename);
        [CustomAction]
        public static ActionResult RegisterAgentAction(Session session)
        {
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        //writer.WriteLine("Started Custom Action Logging.");
                        writer.WriteLine("(M) Doing Custom Action Register License Key\n");
                    }
                }
                string licenseKey = session["LICENSEKEY"];
                string baseUrl = session["BASEURL"];
                session.Log("started verification");
                string agentId = generateAgentId();
                string machineOS = getMachineOS();
                string machineName = getMachineName();
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    BASE_URL = baseUrl;
                }
                if (!BASE_URL.EndsWith("/"))
                {
                    BASE_URL += "/";
                }
                if (!BASE_URL.Contains("secops/v1.0/"))
                {
                    BASE_URL += "secops/v1.0/";
                }
                if (string.IsNullOrEmpty(BASE_URL))
                {
                    session.Log("SecOps Backend Domain not configured property.");
                    return ActionResult.Failure;
                }

                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"License Key is: {session["LICENSEKEY"]}");
                        writer.WriteLine($"Agent ID is : {agentId}");
                        writer.WriteLine($"Operating System is : {machineOS}");
                        writer.WriteLine($"Machine Name is : {machineName}");
                    }
                }

                bool registrationSuccess = registerFingerprint(agentId, machineName, machineOS, licenseKey);
                if (registrationSuccess)
                {
                    session["SECOPS_CONFIG"] = $@"A_ID={agentId}
BASE_URL={BASE_URL}
";
                    session["PHASE"] = "4";

                    using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.WriteLine($"Completed Action Register License Key Successfully!");
                            writer.WriteLine("\n");
                        }
                    }
                    return ActionResult.Success;
                }
                else
                {
                    return ActionResult.Failure;
                }
            }
            catch
            {
                if (File.Exists(CONFIG_FILE_PATH))
                {
                    File.Delete(CONFIG_FILE_PATH);
                }
                return ActionResult.Failure;
            }
        }
        public static string generateAgentId()
        {
            return Guid.NewGuid().ToString();
        }
        public static string getMachineName()
        {
            return Dns.GetHostName();
        }
        public static string getMachineOS()
        {
            try
            {
                string machineOSVersion = string.Empty;
                string scriptContent = @"
$osInfo = Get-WmiObject -Class Win32_OperatingSystem
$osVersion = $osInfo.Caption + ' ' + $osInfo.Version
$osVersion
";
                ProcessStartInfo process = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{scriptContent}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (Process process1 = new Process())
                {
                    process1.StartInfo = process;
                    process1.Start();

                    machineOSVersion = process1.StandardOutput.ReadToEnd();

                    process1.WaitForExit();
                }
                return machineOSVersion;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public class Payload
        {
            public string license_key { get; set; }
            public string agent_id { get; set; }
            public string machine_name { get; set; }
            public string machine_os { get; set; }
        }

        public static bool registerFingerprint(string agentID, string machineName, string machineOS, string licenseKey)
        {
            string url = BASE_URL + REGISTRATION_ENDPOINT;
            using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine($"Target URL is: {url}");
                }
            }
            try
            {
                string powershellscript = @"
param (
    [string]$url,
    [string]$license_key,
    [string]$agent_id,
    [string]$machine_name,
    [string]$machine_os
)
$machine_os = $machine_os -replace '-', ' '
$data = @{
    license_key = $license_key
    agent_id = $agent_id
    machine_name = $machine_name
    machine_os = $machine_os
} | ConvertTo-Json
try {
    $response = Invoke-WebRequest -Uri $url -Method Post -ContentType 'application/json' -Body $data -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        echo $response
        exit 0
    } else {
        echo $response
        exit 1
    }
} catch {
    echo 'An exception occured'
    exit 1
}
";
                string tempScriptPath = Path.Combine(Path.GetTempPath(), "tempscript.ps1");
                File.WriteAllText(tempScriptPath, powershellscript);
                string powershellCommand = $"-ExecutionPolicy Bypass -File {tempScriptPath} -url {url} -license_key {licenseKey} -agent_id {agentID} -machine_name {machineName} -machine_os {machineOS}";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = powershellCommand,
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
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"An Exception occured While sening paylad: {ex}");
                    }
                }
                return false;
            }
        }
    }
}
