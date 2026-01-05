using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using LiteDB;

namespace Cameyo.RdpMon
{
    //
    // Program
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 1 && args[0].Equals("-dbgnow", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }
            else if (args.Length >= 1 && args[0].Equals("-shadownoconsent", StringComparison.InvariantCultureIgnoreCase))
            {
                // ShadowNoConsent
                Log("ShadowNoConsent: in");
                var ret = Utils.RegWriteDword(Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "Shadow", 4);
                Log("ShadowNoConsent: out, ret=" + ret);
                return;
            }
            else if (args.Length >= 1 && args[0].Equals("-inst", StringComparison.InvariantCultureIgnoreCase))
            {
                // Installer
                Log("Inst: in");
                try
                {
                    // Pre-install uninst
                    int exitCode = -1;
                    Utils.ExecProg(Utils.MyExe(), "-uninst", ref exitCode, 45 * 1000, false);
                    System.Configuration.Install.ManagedInstallerClass.InstallHelper(new[] {Utils.MyExe()});
                    Utils.StartService("RdpMon");
                }
                catch (Exception ex)
                {
                    Log("* Exception: " + ex.GetType().Name + ", " + ex.InnerException.GetType().Name);
                }
                Log("Inst: out");
            }
            else if (args.Length >= 1 && args[0].Equals("-uninst", StringComparison.InvariantCultureIgnoreCase))
            {
                // Uninstaller
                Log("Uninst: in");
                try
                {
                    Utils.StopService("RdpMon");
                    Thread.Sleep(1000);
                    System.Configuration.Install.ManagedInstallerClass.InstallHelper(new[] {"/u", Utils.MyExe()});
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log("* Exception: " + ex.GetType().Name + ", " + ex.InnerException.GetType().Name);
                }
                Log("Uninst: out");
            }
            else if (args.Length >= 1 && args[0].Equals("-collect", StringComparison.InvariantCultureIgnoreCase))
            {
                // Explicit aggregation, called by GUI in portable mode
                var aggregator = new ConnectMon(true, true);
                var _addrs = aggregator.Aggregate(DateTime.MinValue);
            }
            else
            {
                // Service engine
                var isSystem = Utils.IsSystemUser();
                Globals.IsSystem = isSystem;
                Log("Main: user=" + Environment.UserName + (isSystem ? " [SYSTEM]" : ""));
                
                if (isSystem)
                {
                    ServiceBase[] services = { new RdpMon.Service() };
                    ServiceBase.Run(services);
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // Check if already installed as a service
                    var installed = (IsSvcInstalled("RdpMon", Utils.MyExe(), out var started));
                    if (!installed)
                    {
                        var resp = MessageBox.Show(null,
                            "此应用将成为一个服务並开始收集数据\n" +
                            "可以使用以下命令卸载此服务：'rdpmon.exe -uninst'",
                            Utils.MyExe(), MessageBoxButtons.OKCancel);
                        if (resp == DialogResult.Cancel)
                        {
                            Application.Exit();
                            return;
                        }
                        var elevate = !Utils.IsElevated();
                        int exitCode = -1;
                        var ok = Utils.ExecProg(Utils.MyExe(), "-inst", ref exitCode, 45 * 1000, elevate);
                        if (ok && exitCode == 0)
                        {
                            exitCode = -1;
                            Utils.ExecProg(Utils.MyExe(), null, ref exitCode, 45 * 1000, false);
                        }
                        else
                            MessageBox.Show("安装服务失败。请重新尝试。");
                        Application.Exit();
                        return;
                    }
                    
                    // Check if service is started
                    if (!started)
                    {
                        Log("Service not started, attempting to start...");
                        var startResult = Utils.StartService("RdpMon", new TimeSpan(0, 0, 10));
                        if (startResult != null)
                        {
                            MessageBox.Show("警告：无法启动 'RdpMon' 服务。" + Environment.NewLine + startResult);
                        }
                        else
                        {
                            MessageBox.Show("'RdpMon' 服务已启动。");
                        }
                    }

                    // Start GUI
                    var mutexName = "RdpMon.GUI";
                    var mutex = new Mutex(true, mutexName);
                    if (!mutex.WaitOne(0))
                    {
                        MessageBox.Show("应用程序已在运行，退出。");
                        return;
                    }
                    try
                    {
                        var evt = EventWaitHandle.OpenExisting(@"Global\RdpMonRefresh");
                        evt.Set();
                    }
                    catch (Exception ex)
                    {
                        Log("Could not open global event (benign): " + ex.Message);
                    }
                    Application.Run(new MainForm());
                }
            }
        }
        
        static public bool IsSvcInstalled(string svcName, string exeFile, out bool started)
        {
            started = false;
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
                var collection = searcher.Get().Cast<ManagementBaseObject>()
                    .Where(mbo => mbo.GetPropertyValue("StartMode") != null)
                    .Select(mbo => Tuple.Create((string)mbo.GetPropertyValue("Name"), mbo));
                var query = from n in collection
                    where string.Equals(n.Item1, svcName, StringComparison.InvariantCultureIgnoreCase)
                    select n;
                if (query.Count() == 1)
                {
                    var mbo = query.First().Item2; 
                    var pathName = (string)mbo.GetPropertyValue("PathName");
                    started = (bool)mbo.GetPropertyValue("Started");
                    var svcExePath = pathName.Trim('\"', ' ');
                    if (svcExePath.Equals(exeFile, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log("Could not access the service controller: " + ex.ToString());
                return false;
            }
        }

        static void Log(string msg) { Utils.Log(msg); }
    }
}
