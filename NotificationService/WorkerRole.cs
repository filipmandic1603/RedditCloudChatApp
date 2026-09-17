using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using RedditService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            CloudQueue queue = AzureQueueService.GetQueueReference("notification");
            Trace.TraceInformation("NotificationService is running");

            while (true)
            {
                try
                {
                    CloudQueueMessage message = queue.GetMessage();
                    if (message != null)
                    {
                        string[] MessageParts = message.AsString.Split('|');
                        int commentId = int.Parse(MessageParts[1]);
                        int topicId = int.Parse(MessageParts[0]);
                        ProcessNotification(topicId, commentId);
                        queue.DeleteMessage(message);
                    }
                   

                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in WorkerRole.RunAsync: " + ex.Message);
                }
            }
         /*   try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }*/
        }

        public override bool OnStart()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("NotificationService has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("NotificationService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("NotificationService has stopped");
        }
        private void ProcessNotification(int topicId, int commentId)
        {
            TopicDataRepository topicRepo = new TopicDataRepository();

            Topic topic = topicRepo.GetTopic(topicId.ToString());
            Comment comment = GetCommentById(topicId, commentId);
            List<string> subscribers = GetSubscribersByTopicId(topicId);
            int emailCount = 0;
            if (comment != null)
            {
                foreach (var subscriber in subscribers)
                {
                    SendEmail(subscriber, topic, comment.Text);
                    emailCount++;
                }
            }
          

             LogNotification(commentId, emailCount);
        }
        private List<string> GetSubscribersByTopicId(int topicId)
        {
            TopicDataRepository topicDataRepository = new TopicDataRepository();
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
           

            List<string> subscribers = topic.Subscribes;
            
            return subscribers; 
        }
        private async Task SendEmail(string toEmail, Topic topic, string CommentText)
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
            message.Subject = $"New comment on Topic : {topic.Title}";
            message.Body = CommentText;

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
        private Comment GetCommentById(int topicId, int commentId)
        {
            // Implement the method to get comment by ID
            TopicDataRepository topicDataRepository = new TopicDataRepository();
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
          
            Comment comment = topic.Comments.FirstOrDefault(x => x.Id == commentId);
            return comment; // Placeholder implementation
        }
         private void LogNotification(int commentId, int emailCount)
         {
            string fileName = "C:\\Users\\filip\\Desktop\\Projects\\Fakultet\\RedditCloud-master/EmailLogFile.txt";
           
            string message = $"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} | Comment ID: {commentId} | Emails Sent: {emailCount}\n";
            File.AppendAllText(fileName, message);
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
