using Microsoft.AspNet.SignalR;
using RedditService.Hubs;
using RedditService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace RedditService.Controllers
{
    public class ChatController : Controller
    {
        private readonly MessageDataRepository _messageRepository = new MessageDataRepository();
        private readonly UserDataRepository _userRepository = new UserDataRepository();

        private User CurrentUser => Session["LoggedInUser"] as User;

        private static object ToDto(Message m)
        {
            return new
            {
                rowKey = m.RowKey,
                conversationId = m.PartitionKey,
                sender = m.Sender,
                receiver = m.Receiver,
                content = m.Content,
                sentAt = m.SentAt,         
                isEdited = m.IsEdited
            };
        }

        private static IHubContext ChatContext =>
            GlobalHost.ConnectionManager.GetHubContext<ChatHub>();

        public ActionResult Index()
        {
            User me = CurrentUser;
            if (me == null)
                return RedirectToAction("Login", "Registration");

            // Identitet trenutnog korisnika (email je primaran, RowKey je fallback jer je i on == email)
            string myEmail = (me.Email ?? me.RowKey ?? string.Empty).Trim();

            List<User> others = _userRepository.RetrieveAllUsers()
                .ToList()
                .Where(u =>
                {
                    string uEmail = (u.Email ?? u.RowKey ?? string.Empty).Trim();
                    // Izbaci prazne i samog sebe (poređenje i po Email i po RowKey)
                    if (string.IsNullOrEmpty(uEmail))
                        return false;
                    if (string.Equals(uEmail, myEmail, StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (string.Equals((u.RowKey ?? "").Trim(), (me.RowKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                        return false;
                    return true;
                })
                .ToList();

            ViewBag.CurrentEmail = me.Email;
            ViewBag.CurrentName = me.Name;
            return View(others);
        }

        [HttpGet]
        public ActionResult GetConversation(string otherEmail)
        {
            User me = CurrentUser;
            if (me == null)
                return new HttpStatusCodeResult(401);
            if (string.IsNullOrWhiteSpace(otherEmail))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var messages = _messageRepository.GetConversation(me.Email, otherEmail)
                .Select(ToDto)
                .ToList();
            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SendMessage(string receiver, string content)
        {
            User me = CurrentUser;
            if (me == null)
                return new HttpStatusCodeResult(401);
            if (string.IsNullOrWhiteSpace(receiver) || string.IsNullOrWhiteSpace(content))
                return new HttpStatusCodeResult(400, "Receiver i content su obavezni.");

            var message = new Message(me.Email, receiver, content.Trim());
            _messageRepository.Add(message);
            var dto = ToDto(message);
            ChatContext.Clients.Group(receiver.Trim().ToLowerInvariant()).messageReceived(dto);
            ChatContext.Clients.Group(me.Email.Trim().ToLowerInvariant()).messageReceived(dto);

            return Json(dto);
        }

        [HttpPost]
        public ActionResult EditMessage(string rowKey, string otherEmail, string content)
        {
            User me = CurrentUser;
            if (me == null)
                return new HttpStatusCodeResult(401);
            if (string.IsNullOrWhiteSpace(rowKey) || string.IsNullOrWhiteSpace(content))
                return new HttpStatusCodeResult(400);

            string conversationId = Message.BuildConversationId(me.Email, otherEmail);
            Message message = _messageRepository.Get(conversationId, rowKey);
            if (message == null)
                return new HttpStatusCodeResult(404);

            if (!string.Equals(message.Sender, me.Email, StringComparison.OrdinalIgnoreCase))
                return new HttpStatusCodeResult(403);

            message.Content = content.Trim();
            message.IsEdited = true;
            _messageRepository.Update(message);

            var dto = ToDto(message);
            ChatContext.Clients.Group(message.Receiver.Trim().ToLowerInvariant()).messageEdited(dto);
            ChatContext.Clients.Group(message.Sender.Trim().ToLowerInvariant()).messageEdited(dto);

            return Json(dto);
        }

        [HttpPost]
        public ActionResult DeleteMessage(string rowKey, string otherEmail)
        {
            User me = CurrentUser;
            if (me == null)
                return new HttpStatusCodeResult(401);
            if (string.IsNullOrWhiteSpace(rowKey))
                return new HttpStatusCodeResult(400);

            string conversationId = Message.BuildConversationId(me.Email, otherEmail);
            Message message = _messageRepository.Get(conversationId, rowKey);
            if (message == null)
                return Json(new { rowKey, conversationId }); 

            if (!string.Equals(message.Sender, me.Email, StringComparison.OrdinalIgnoreCase))
                return new HttpStatusCodeResult(403);

            string receiver = message.Receiver;
            string sender = message.Sender;
            _messageRepository.Delete(message); 

            var payload = new { rowKey, conversationId };
            ChatContext.Clients.Group(receiver.Trim().ToLowerInvariant()).messageDeleted(payload);
            ChatContext.Clients.Group(sender.Trim().ToLowerInvariant()).messageDeleted(payload);

            return Json(payload);
        }
    }
}
