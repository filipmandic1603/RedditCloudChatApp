using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;

namespace RedditService.Models
{
    /// <summary>
    /// Repozitorijum za privatne poruke (Azure Table Storage, tabela "MessagesTable").
    /// Prati isti obrazac kao TopicDataRepository / UserDataRepository.
    /// </summary>
    public class MessageDataRepository
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _table;

        public MessageDataRepository()
        {
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudTableClient tableClient = new CloudTableClient(new System.Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("MessagesTable");
            _table.CreateIfNotExists();
        }

        /// <summary>Sve poruke jedne konverzacije, hronološki poređane.</summary>
        public List<Message> GetConversation(string emailA, string emailB)
        {
            string conversationId = Message.BuildConversationId(emailA, emailB);
            var query = from m in _table.CreateQuery<Message>()
                        where m.PartitionKey == conversationId
                        select m;
            return query.ToList().OrderBy(m => m.SentAt).ToList();
        }

        public Message Get(string conversationId, string rowKey)
        {
            var query = from m in _table.CreateQuery<Message>()
                        where m.PartitionKey == conversationId && m.RowKey == rowKey
                        select m;
            return query.FirstOrDefault();
        }

        public void Add(Message message)
        {
            TableOperation insert = TableOperation.Insert(message);
            _table.Execute(insert);
        }

        public void Update(Message message)
        {
            TableOperation replace = TableOperation.Replace(message);
            _table.Execute(replace);
        }

        /// <summary>HARD delete – poruka fizički nestaje za oba korisnika.</summary>
        public void Delete(Message message)
        {
            TableOperation delete = TableOperation.Delete(message);
            _table.Execute(delete);
        }
    }
}
