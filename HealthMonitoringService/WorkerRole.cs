using Common;
using Microsoft.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using RedditService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitoringService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public override void Run()
        {
            Trace.TraceInformation("HealthMonitoringService is running");

            /*  while (true)
              {
                  //PerformHealthCheck();
                  Thread.Sleep(5000); // Wait for 5 seconds before the next check
              }*/
            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Use TLS 1.2 for Service Bus connections
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.
            HealthCheckDataRepository healthCheckDataRepository = new HealthCheckDataRepository();
            HealthRedditServer healthRedditServer = new HealthRedditServer();
            HealthNotificationServer healthNotificationServer = new HealthNotificationServer();
            bool result = base.OnStart();

            Trace.TraceInformation("HealthMonitoringService has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("HealthMonitoringService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("HealthMonitoringService has stopped");
        }
       
      
        private void LogHealthCheck(bool RedditStatus, bool NotificationStatus)
        {
            HealthCheckDataRepository health = new HealthCheckDataRepository();
            List<HealthCheckEntity> healthList = health.RetrieveAllLogsData().ToList();
            string status = "";
            if (RedditStatus && NotificationStatus)
            {
                status = $"{DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss")}_OK";

            }
            else
            {
                status = $"{DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss")}_NOT_OK";
            }

            var healthCheck = new HealthCheckEntity(DateTime.Now)
            {
                Status = status,
                DateTime = DateTime.Now

            };

            health.LogHealtCheck(healthCheck);
        }
        private async Task SendEmail(string toEmail)
        {
            string smtpServer = "smtp.gmail.com";
            int port = 587;

         
            string username = "filipbasta02@gmail.com";
            string password = "tqsd xoav dsyz jlgi";
            string fromEmail = "filipbasta02@gmail.com";
            SmtpClient client = new SmtpClient(smtpServer, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);


            MailMessage message = new MailMessage(fromEmail, toEmail);
            message.Subject = "Service Health Check Failure";
            message.Body = "One or more services are not responding to health checks.";

            try
            {
           
                client.Send(message);
                Console.WriteLine("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }
      
        private void SendFailureNotification()
        {
            UserDataRepository userDataRepository = new UserDataRepository();
            List<User> users = userDataRepository.RetrieveAllUsers().ToList();
            foreach (var user in users)
            {

                SendEmail(user.Email);
            }
        }
        private IHealthMonitoring proxyReddit;
        private IHealthMonitoring proxyNotification;
        private bool IsRedditeServiceActive { get; set; }
        private bool IsNotificationServiceActive { get; set; }
        public void Connect()
        {
            
            var binding = new NetTcpBinding();
            ChannelFactory<IHealthMonitoring> factory = new
            ChannelFactory<IHealthMonitoring>(binding, new
            EndpointAddress("net.tcp://localhost:8080/HealthMonitoring"));
            proxyReddit = factory.CreateChannel();
            if (proxyReddit != null)
            {
                IsRedditeServiceActive = true;
            }
            else
                IsRedditeServiceActive = false;
            var bindingNotification = new NetTcpBinding();
            ChannelFactory<IHealthMonitoring> factoryNotification = new
            ChannelFactory<IHealthMonitoring>(bindingNotification, new
            EndpointAddress("net.tcp://localhost:8081/HealthMonitoring"));
            proxyNotification = factoryNotification.CreateChannel();
            if (proxyNotification != null)
            {
                IsNotificationServiceActive = true;
            }
            else
                IsNotificationServiceActive = false;
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Connect();
                    LogHealthCheck(IsRedditeServiceActive, IsNotificationServiceActive);
                  
                    Trace.TraceInformation("Services are alive.");
                }
                catch(Exception ex)
                {
                    SendFailureNotification();
                    Console.WriteLine(ex.Message);
                    Trace.TraceWarning("Service not alive anymore!");
                }

                await Task.Delay(5000);
            }
        }
    }
}
