using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Hosting;


namespace StopService
{
    public class Worker : BackgroundService
    {
        //https://dotnetcoretutorials.com/2019/12/07/creating-windows-services-in-net-core-part-3-the-net-core-worker-way/
        //https://dotnetcoretutorials.com/2019/09/19/creating-windows-services-in-net-core-part-1-the-microsoft-way/


        //Root Project =>> dotnet publish -r win-x64 -c Release
        // sc create TestService BinPath = C:\location\StopService.exe
        private string Desktop = string.Empty;

        private System.Timers.Timer Timer = null;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Timer = new System.Timers.Timer(3600000);
            Timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            Timer.Start();
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SsStop();
        }

        private void SsStop()
        {
            Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + DateTime.Now.ToLongTimeString().Replace(":", "-") + ".txt";
            using (StreamWriter sw = File.CreateText(Desktop)) ;
            Stop();
        }

        public void Stop()
        {
            try
            {
                if (!IsAdministrator())
                {
                    throw new Exception("Permission denied. Please run this as administrator.");
                }
                StopAndDisableService("wuauserv", "Windows Update");
                StopAndDisableService("wercplsupport", "Problem Reports and Solutions Control Panel Support");
                StopAndDisableService("WerSvc", "Windows Error Reporting Service");
                StopAndDisableService("DoSvc", "Delivery Optimization");
                StopAndDisableService("BITS", "Background Intelligent Transfer Service");
            }
            catch (Exception e)
            {
                LastException(e);
            }

        }


        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        void StopAndDisableService(string serviceName, string serviceDescription)
        {

            string newLine = Environment.NewLine;

            try
            {

                AppendTextFile("----------------------------------------------------");
                AppendTextFile($"Stopping \"{serviceDescription}\"");


                var ps = new ProcessStartInfo("cmd", $"/c sc stop {serviceName}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                var pr = Process.Start(ps);
                pr?.WaitForExit();
                AppendTextFile(pr?.StandardOutput.ReadToEnd());

            }
            catch (Exception e)
            {
                LastException(e);
            }

            try
            {

                AppendTextFile($"Disabling \"{serviceDescription}\"");

                var ps = new ProcessStartInfo("cmd", $"/c sc config {serviceName} start=disabled")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                var pr = Process.Start(ps);
                pr?.WaitForExit();
                AppendTextFile(pr?.StandardOutput.ReadToEnd());
            }
            catch (Exception e)
            {
                LastException(e);
            }

        }


        void LastException(Exception error)
        {
            Exception realerror = error;
            while (realerror.InnerException != null)
                realerror = realerror.InnerException;

            AppendTextFile(realerror.ToString());
        }


        public void AppendTextFile(string text)
        {
            using (StreamWriter sw = File.AppendText(Desktop))
            {
                sw.WriteLine(text);
            }
        }
    }
}
