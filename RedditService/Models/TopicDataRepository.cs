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
    public class TopicDataRepository
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public TopicDataRepository()
        {
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("TopicTable");
            _table.CreateIfNotExists();

        }
        public IQueryable<Topic> RetrieveAllTopics()
        {
            var results = from g in _table.CreateQuery<Topic>()
                          where g.PartitionKey == "Topic"
                          select g;
            return results;
        }

        public void AddTopic(Topic topic)
        {
            topic.SerializedListUpVoteUser = JsonConvert.SerializeObject(topic.ListUpVoteUser);
            topic.SerializedListDownVoteUser = JsonConvert.SerializeObject(topic.ListDownVoteUser);
            topic.SerializedComments = JsonConvert.SerializeObject(topic.Comments);
            topic.SerializedSubscribes = JsonConvert.SerializeObject(topic.Subscribes);

            TableOperation insertOperation = TableOperation.Insert(topic);
            _table.Execute(insertOperation);

        }
        public void RemoveTopic(string rowkey)
        {
            Topic topic = RetrieveAllTopics().Where(s => s.RowKey == rowkey).FirstOrDefault();

            if (topic != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(topic);
                _table.Execute(deleteOperation);
            }
        }
        public bool ExistsTopic(string email)
        {
            return RetrieveAllTopics().Where(s => s.RowKey == email).FirstOrDefault() != null;
        }
        public void UpdateTopic(Topic topic)
        {
            topic.SerializedListUpVoteUser = JsonConvert.SerializeObject(topic.ListUpVoteUser);
            topic.SerializedListDownVoteUser = JsonConvert.SerializeObject(topic.ListDownVoteUser);
            topic.SerializedComments = JsonConvert.SerializeObject(topic.Comments);
            topic.SerializedSubscribes = JsonConvert.SerializeObject(topic.Subscribes);
            TableOperation updateOperation = TableOperation.Replace(topic);
            _table.Execute(updateOperation);
        }
        public Topic GetTopic(string rowKey)
        {
            Topic topic = RetrieveAllTopics().Where(p => p.RowKey == rowKey).FirstOrDefault();
            topic.ListUpVoteUser = JsonConvert.DeserializeObject<List<string>>(topic.SerializedListUpVoteUser);
            topic.ListDownVoteUser = JsonConvert.DeserializeObject<List<string>>(topic.SerializedListDownVoteUser);
            topic.Comments = JsonConvert.DeserializeObject<List<Comment>>(topic.SerializedComments);
            topic.Subscribes = JsonConvert.DeserializeObject<List<string>>(topic.SerializedSubscribes);

            return topic;
        }
    }
}