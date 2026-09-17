using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using RedditService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RedditService.Controllers
{
    public class HomeController : Controller
    {
        TopicDataRepository topicDataRepository = new TopicDataRepository();
        UserDataRepository userDataRepository = new UserDataRepository();
       
        List<Topic> allTopics { get; set; } = new List<Topic>();
      
        public ActionResult Index()
        {
            allTopics = topicDataRepository.RetrieveAllTopics().ToList();
            List<User> alluser = userDataRepository.RetrieveAllUsers().ToList();
           
            return View(allTopics);
        }
        public ActionResult CreateTopic()
        {
            return View("CreateTopic");
        }
        [HttpPost]
        public ActionResult CreateTopic(Topic t, HttpPostedFileBase image)
        {
            try
            {
                allTopics = topicDataRepository.RetrieveAllTopics().ToList();
                User loggedInUser = Session["LoggedInUser"] as User;
                string uniqueBlobName = t.Title + "Photo";
                var storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference("photo");
                CloudBlockBlob blob = container.GetBlockBlobReference(uniqueBlobName);
                blob.Properties.ContentType = image.ContentType;
                
                

                blob.UploadFromStream(image.InputStream);
                int RowKeyValue = allTopics.Count == 0 ? 1 : allTopics.Max(x => x.Id) + 1;
                Topic newTopic = new Topic()
                {
                    RowKey = RowKeyValue.ToString(),
                    Id = RowKeyValue,
                    Author = loggedInUser.Email,
                    Title = t.Title,
                    Description = t.Description,
                    Comments = new List<Comment>(),
                    Subscribes = new List<string>(),
                    NumberUpVote = 0,
                    NumberDownVote = 0,
                    ListUpVoteUser = new List<string>(),
                    ListDownVoteUser = new List<string>(),
                    ImageUrl = blob.Uri.ToString(),
                    ThumbnailUrl = blob.Uri.ToString()
                };
              
                topicDataRepository.AddTopic(newTopic);
                
                loggedInUser.Topics.Add(newTopic);
                userDataRepository.UpdateUser(loggedInUser);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            
        }
        [HttpPost]
        public ActionResult UpVote(int topicId)
        {
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
            User LoggedInUser = Session["LoggedInUser"] as User;
            if (topic == null || LoggedInUser == null)
                return RedirectToAction("Index", "Home");

            string email = LoggedInUser.Email;

            if (topic.ListUpVoteUser.Contains(email))
            {
                topic.ListUpVoteUser.Remove(email);
                topic.NumberUpVote--;
            }
            else
            {
                if (topic.ListDownVoteUser.Contains(email))
                {
                    topic.ListDownVoteUser.Remove(email);
                    topic.NumberDownVote--;
                }
                topic.ListUpVoteUser.Add(email);
                topic.NumberUpVote++;
            }

            topicDataRepository.UpdateTopic(topic);
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public ActionResult DownVote(int topicId)
        {
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
            User LoggedInUser = Session["LoggedInUser"] as User;
            if (topic == null || LoggedInUser == null)
                return RedirectToAction("Index", "Home");

            string email = LoggedInUser.Email;

            if (topic.ListDownVoteUser.Contains(email))
            {
                topic.ListDownVoteUser.Remove(email);
                topic.NumberDownVote--;
            }
            else
            {
                if (topic.ListUpVoteUser.Contains(email))
                {
                    topic.ListUpVoteUser.Remove(email);
                    topic.NumberUpVote--;
                }
                topic.ListDownVoteUser.Add(email);
                topic.NumberDownVote++;
            }

            topicDataRepository.UpdateTopic(topic);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Subscribe(int topicId)
        {
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
            User user = Session["LoggedInUser"] as User;
            if (!topic.Subscribes.Contains(user.Email))
            {
                topic.Subscribes.Add(user.Email);
            }
            
            topicDataRepository.UpdateTopic(topic);
            return RedirectToAction("Index", "Home");

        }
        [HttpPost]
        public ActionResult Comments(int topicId, Comment comment)
        {
            Topic topic = topicDataRepository.GetTopic(topicId.ToString());
            User loggedInuser = Session["LoggedInUser"] as User;

            if (loggedInuser == null || topic == null)
                return RedirectToAction("Index", "Home");

            if (comment == null || string.IsNullOrWhiteSpace(comment.Text))
                return RedirectToAction("Index", "Home");

            comment.Author = loggedInuser.Name;
            comment.PostId = topicId;
            if (topic.Comments == null)
                topic.Comments = new List<Comment>();

            comment.Id = topic.Comments.Count == 0 ? 1 : topic.Comments.Max(c => c.Id) + 1;

            topic.Comments.Add(comment);
            topicDataRepository.UpdateTopic(topic);

            Topic ownTopic = loggedInuser.Topics?.FirstOrDefault(x => x.Id == topicId);
            if (ownTopic != null)
            {
                if (ownTopic.Comments == null)
                    ownTopic.Comments = new List<Comment>();
                ownTopic.Comments.Add(comment);
                userDataRepository.UpdateUser(loggedInuser);
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("notification");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(topicId.ToString() + "|" + comment.Id.ToString());
            queue.AddMessage(message);

            return RedirectToAction("Index", "Home");
        }
        
        public List<Comment> GetComments(int id)
        {
            List<Topic> allTopics = topicDataRepository.RetrieveAllTopics().ToList();

            Topic topic = allTopics.FirstOrDefault(x => x.Id == id);
            return topic.Comments;
        }

        public ActionResult SearchTopic(string searchTerm)
        {

            try
            {
                List<Topic> allTopic = topicDataRepository.RetrieveAllTopics().ToList();

                List<Topic> foundedTopic = string.IsNullOrWhiteSpace(searchTerm)
                    ? allTopic
                    : allTopic.Where(x => x.Title != null &&
                            x.Title.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                return View("Index", foundedTopic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
          
        }
        public ActionResult Sort(string sortOrder)
        {
            try
            {
                List<Topic> allTopic = topicDataRepository.RetrieveAllTopics().ToList();
                List<Topic> sortList = new List<Topic>();
                switch (sortOrder)
                {
                    case "latest":
                        sortList = allTopic.OrderByDescending(x => x.Id).ToList();
                        return View("Index", sortList);
                    case "upvotes":
                        sortList = allTopic.OrderByDescending(x => x.NumberUpVote).ToList();
                        return View("Index", sortList);
                    case "downvotes":
                        sortList = allTopic.OrderByDescending(x => x.NumberDownVote).ToList();
                        return View("Index", sortList);
                  /*  case "comments":
                        sortList = allTopic.OrderByDescending(x => x.).ToList();
                        return View("Index", sortList);*/
                    default:
                        break;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}