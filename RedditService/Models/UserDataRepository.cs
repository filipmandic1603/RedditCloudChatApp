using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RedditService.Models
{
    public class UserDataRepository
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public UserDataRepository()
        {
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("UsersTable");
            _table.CreateIfNotExists();
           
        }
        public IQueryable<User> RetrieveAllUsers()
        {
            var results = from g in _table.CreateQuery<User>()
                          where g.PartitionKey == "User"
                          select g;
            return results;
        }

        public void AddUser(User newUser)
        {
            newUser.SerializedTopics = JsonConvert.SerializeObject(newUser.Topics);
            TableOperation insertOperation = TableOperation.Insert(newUser);      
            _table.Execute(insertOperation);
            
        }
        public void RemoveUser(string email)
        {
            User user = RetrieveAllUsers().Where(s => s.RowKey == email).FirstOrDefault();

            if (user != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(user);
                _table.Execute(deleteOperation);
            }
        }
        public bool Exists(string email)
        {
            return RetrieveAllUsers().Where(s => s.RowKey == email).FirstOrDefault() != null;
        }
        public void UpdateUser(User user)
        {
            user.SerializedTopics = JsonConvert.SerializeObject(user.Topics);
            TableOperation updateOperation = TableOperation.Replace(user);
            _table.Execute(updateOperation);
        }
        public User GetUser(string email)
        {
            User user = RetrieveAllUsers().Where(p => p.RowKey == email).FirstOrDefault();
            user.Topics = JsonConvert.DeserializeObject<List<Topic>>(user.SerializedTopics);
            return user;
        }
    }
}
