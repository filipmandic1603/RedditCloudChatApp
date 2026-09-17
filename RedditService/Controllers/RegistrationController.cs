using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RedditService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RedditService.Controllers
{
    public class RegistrationController : Controller
    {
        // GET: Registration
        UserDataRepository UserDataRepository = new UserDataRepository();
        TopicDataRepository TopicDataRepository = new TopicDataRepository();
        public ActionResult Index()
        {
         
            return View();
        }
        public ActionResult Login()
        {
            return View("LoginPage");
        }

        [HttpPost]
        public ActionResult RegisterUser(User u, HttpPostedFileBase file)
        {

            List<User> allUsers = UserDataRepository.RetrieveAllUsers().ToList();
            try
            {
                string uniqueBlobName = u.Name + "Photo";
                var storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference("photo");
                CloudBlockBlob blob = container.GetBlockBlobReference(uniqueBlobName);
                blob.Properties.ContentType = file.ContentType;

                blob.UploadFromStream(file.InputStream);

                if (!UserDataRepository.Exists(u.Email))
                {
                    User newUser = new User()
                    {
                        RowKey = u.Email,
                        Name = u.Name,
                        Lastname = u.Lastname,
                        Address = u.Address,
                        City = u.City,
                        Country = u.Country,
                        PhoneNumber = u.PhoneNumber,
                        Email = u.Email,
                        Password = u.Password,
                        PhotoUrl = blob.Uri.ToString(),
                        ThumbnailUrl = blob.Uri.ToString(),
                        Topics = new List<Topic>()
                      
                    };
                    ViewBag.ErrorEmail = "";
                    UserDataRepository.AddUser(newUser);
                    ViewBag.ErrorEmail = "";
                    return View("LoginPage");
                }
                else
                {
                    ViewBag.ErrorEmail = "This email already exist!";
                    return View("Index");
                }
                             
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
           

        }
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            List<User> allUsers = UserDataRepository.RetrieveAllUsers().ToList();

            try
            {
                User user = allUsers.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    ViewBag.ErrorEmail = "Invalid email";
                    ViewBag.ErrorPass = "";
                }
                else if (user.Password != password)
                {
                    ViewBag.ErrorEmail = "";
                    ViewBag.ErrorPass = "Invalid password";
                }
                else
                {
                    Session["LoggedInUser"] = user;
                    TempData["SuccessMessage"] = "You logged in successfully";
                    return RedirectToAction("Index", "Home");
                }

                return View("LoginPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
           
        }
        public ActionResult LogOut()
        {
            Session.Remove("LoggedInUser");
            Session.Clear();

            return RedirectToAction("Index", "Home");
        }
        public ActionResult EditUser()
        {
            User LoggedInUser = Session["LoggedInUser"] as User;
           
            return View("EditUser", LoggedInUser);
        }
        [HttpPost]
        public ActionResult EditUser(User u, HttpPostedFileBase file)
        {
            
            try
            {
                List<User> allUsers = UserDataRepository.RetrieveAllUsers().ToList();
                User loggedIn = Session["LoggedInUser"] as User;   
                if (loggedIn == null)
                    return RedirectToAction("Login", "Registration");
                string uniqueBlobName = string.Format("image_{0}", loggedIn.Email);
                var storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference("photo");
                CloudBlockBlob blob = container.GetBlockBlobReference(uniqueBlobName);
                blob.Properties.ContentType = file.ContentType;

                blob.UploadFromStream(file.InputStream);
                User updatedUser = UserDataRepository.GetUser(loggedIn.Email);
                updatedUser.Name = u.Name;
                updatedUser.Lastname = u.Lastname;
                updatedUser.Address = u.Address;
                updatedUser.Country = u.Country;
                updatedUser.City = u.City;
                updatedUser.Password = u.Password;
                updatedUser.PhoneNumber = u.PhoneNumber;
                updatedUser.PhotoUrl = blob.Uri.ToString();
                updatedUser.ThumbnailUrl = blob.Uri.ToString();
                UserDataRepository.UpdateUser(updatedUser);
                Session["LoggedInUser"] = updatedUser;   
                return View("EditUser", updatedUser);
            
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                
            }
            return null;
        }

        [HttpPost]
        public ActionResult DeleteTopic(int topicId)
        {
            User loggedInUser = Session["LoggedInUser"] as User;
            if (loggedInUser == null)
                return RedirectToAction("Login", "Registration");

            Topic ownTopic = loggedInUser.Topics?.FirstOrDefault(x => x.Id == topicId);
            if (ownTopic != null)
            {
                loggedInUser.Topics.Remove(ownTopic);
                UserDataRepository.UpdateUser(loggedInUser);
            }

            TopicDataRepository.RemoveTopic(topicId.ToString());

            return View("EditUser", loggedInUser);
        }
        [HttpPost]
        public ActionResult DeleteComment(int commentId, int topicId)
        {
            User loggedInUser = Session["LoggedInUser"] as User;
            if (loggedInUser == null)
                return RedirectToAction("Login", "Registration");

            Topic topic = TopicDataRepository.GetTopic(topicId.ToString());
            if (topic == null)
                return View("EditUser", loggedInUser);

            if (topic.Comments != null)
            {
                topic.Comments.RemoveAll(c => c.Id == commentId);
                TopicDataRepository.UpdateTopic(topic);
            }

            Topic ownTopic = loggedInUser.Topics?.FirstOrDefault(x => x.Id == topicId);
            if (ownTopic != null && ownTopic.Comments != null)
            {
                ownTopic.Comments.RemoveAll(c => c.Id == commentId);
                UserDataRepository.UpdateUser(loggedInUser);
            }

            return View("EditUser", loggedInUser);
        }
    }
}