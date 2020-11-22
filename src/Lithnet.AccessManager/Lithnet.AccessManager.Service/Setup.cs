using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Lithnet.AccessManager.Server;
using MartinCostello.SqlLocalDb;
using Vanara.PInvoke;
using Vanara.Security.AccessControl;

namespace Lithnet.AccessManager.Service
{
    public static class Setup
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static void SetupNLog()
        {
            var configuration = new NLog.Config.LoggingConfiguration();

            var serviceLog = new NLog.Targets.FileTarget("access-manager-installer")
            {
                FileName = Path.Combine(Path.GetTempPath(), "access-manager-installer.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = 7,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            var debugLog = new NLog.Targets.OutputDebugStringTarget();

            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, serviceLog);
            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, debugLog);

            NLog.LogManager.Configuration = configuration;
        }


        public static void Install(string path, string username, string password)
        {
            try
            {
                logger.Info($"Opening service control manager");
                var serviceManager = AdvApi32.OpenSCManager(null, null, AdvApi32.ScManagerAccessTypes.SC_MANAGER_ALL_ACCESS);
                string[] dependencies = new[] { "http" };
                logger.Info($"Opened service control manager");
                AdvApi32.SafeSC_HANDLE serviceHandle;

                if (!path.StartsWith("\""))
                {
                    path = "\"" + path;
                }

                if (!path.EndsWith("\""))
                {
                    path = path + "\"";
                }
                
                try
                {
                    logger.Info($"Checking for existing {Constants.ServiceName} service");
                    serviceHandle = AdvApi32.OpenService(serviceManager, Constants.ServiceName, AdvApi32.ServiceAccessTypes.SERVICE_ALL_ACCESS);

                    if (serviceHandle.IsNull)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    logger.Info($"Found existing {Constants.ServiceName} service");
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode == 1060)
                    {
                        logger.Info($"Existing {Constants.ServiceName} service not found");
                        logger.Info($"Attempting to create server {Constants.ServiceName} for user {username} at {path}");

                        serviceHandle = AdvApi32.CreateService(serviceManager, Constants.ServiceName, Constants.ServiceDisplayName, (uint)AdvApi32.ServiceAccessTypes.SERVICE_ALL_ACCESS, AdvApi32.ServiceTypes.SERVICE_WIN32_OWN_PROCESS, AdvApi32.ServiceStartType.SERVICE_DEMAND_START, AdvApi32.ServiceErrorControlType.SERVICE_ERROR_NORMAL, path, null, IntPtr.Zero, dependencies, username, password);

                        if (serviceHandle.IsNull)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }

                        logger.Info($"Created {Constants.ServiceName} service");
                    }
                    else
                    {
                        throw;
                    }
                }

                var description = new AdvApi32.SERVICE_DESCRIPTION()
                {
                    lpDescription = Constants.ServiceDescription
                };

                logger.Info($"Updating service description");
                if (!AdvApi32.ChangeServiceConfig2(serviceHandle, AdvApi32.ServiceConfigOption.SERVICE_CONFIG_DESCRIPTION, description))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var sidConfig = new AdvApi32.SERVICE_SID_INFO()
                {
                    dwServiceSidType = 0x1
                };

                logger.Info($"Updating service SID configuration");
                if (!AdvApi32.ChangeServiceConfig2(serviceHandle, AdvApi32.ServiceConfigOption.SERVICE_CONFIG_SERVICE_SID_INFO, sidConfig))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                logger.Info($"Updated existing {Constants.ServiceName} service parameters");

                TryGrantLogonAsAService(username);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to install service");
                throw;
            }
        }

        private static void TryGrantLogonAsAService(string username)
        {
            try
            {
                logger.Info($"Checking if {username} has logon as a service right");
                SystemSecurity d = new SystemSecurity();
                var privs = d.UserPrivileges(username);

                if (!privs[SystemPrivilege.ServiceLogon])
                {
                    logger.Info($"Granting logon as a service right to account {username}");
                    privs[SystemPrivilege.ServiceLogon] = true;
                }
                else
                {
                    logger.Info($"{username} already had logon as a service right");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "The service account could not be granted 'logon as a service right'");
            }
        }

        public static void Uninstall()
        {
            try
            {
                logger.Info($"Opening service control manager");
                var serviceManager = AdvApi32.OpenSCManager(null, null, AdvApi32.ScManagerAccessTypes.SC_MANAGER_ALL_ACCESS);
                string[] dependencies = new[] { "http" };
                logger.Info($"Opened service control manager");

                try
                {
                    logger.Info($"Checking for existing {Constants.ServiceName} service");
                    var serviceHandle = AdvApi32.OpenService(serviceManager, Constants.ServiceName, AdvApi32.ServiceAccessTypes.SERVICE_ALL_ACCESS);

                    if (serviceHandle.IsNull)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    logger.Info($"Found existing {Constants.ServiceName} service");

                    if (!AdvApi32.DeleteService(serviceHandle))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    logger.Info($"Deleted existing {Constants.ServiceName} service");
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode == 1060)
                    {
                        logger.Info($"Existing {Constants.ServiceName} service not found");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to uninstall service");
            }
        }

        public static void Process(string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            if (args[0] != "setup")
            {
                return;
            }

            SetupNLog();

            if (args[1] == "install-service")
            {
                // 0     1               2        3        
                // setup install-service username password 

                if (args.Length < 3)
                {
                    throw new ArgumentException("Invalid number of arguments");
                }

                string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                string username = args[2];
                string password = null;

                if (args.Length >= 4)
                {
                    password = args[3];

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        password = null;
                    }
                }

                Install(path, username, password);
                return;
            }

            if (args[1] == "uninstall-service")
            {
                Uninstall();
                return;
            }
        }
    }
}
