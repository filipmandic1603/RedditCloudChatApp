using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RedditService.Models
{
    public class User : TableEntity
    {
      
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhotoUrl { get; set; }
        public string ThumbnailUrl { get; set; }

        public List<Topic> Topics { get; set; }
        public User()
        {
            PartitionKey = "User";
            RowKey = "";
        }
        public string SerializedTopics
        {
            get => JsonConvert.SerializeObject(Topics);
            set => Topics = JsonConvert.DeserializeObject<List<Topic>>(value);
        }
    }
}