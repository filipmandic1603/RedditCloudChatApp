using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Web;

namespace RedditService.Models
{
    public class Topic : TableEntity
    {
        public int Id { get; set; } = 1;
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public int NumberUpVote { get; set; }
        public int NumberDownVote { get; set; }
        [JsonIgnore]
        public List<string> ListUpVoteUser { get; set; }
        [JsonIgnore]
        public List<string> ListDownVoteUser { get; set; }
        [JsonIgnore]
        public List<Comment> Comments { get; set; }
        [JsonIgnore]
        public List<string> Subscribes { get; set; }

        public Topic()
        {
            PartitionKey = "Topic";
            RowKey = "";
        }
        public string SerializedListUpVoteUser
        {
            get => JsonConvert.SerializeObject(ListUpVoteUser);
            set => ListUpVoteUser = JsonConvert.DeserializeObject<List<string>>(value);
        }

        public string SerializedListDownVoteUser
        {
            get => JsonConvert.SerializeObject(ListDownVoteUser);
            set => ListDownVoteUser = JsonConvert.DeserializeObject<List<string>>(value);
        }

        public string SerializedComments
        {
            get => JsonConvert.SerializeObject(Comments);
            set => Comments = JsonConvert.DeserializeObject<List<Comment>>(value);
        }

        public string SerializedSubscribes
        {
            get => JsonConvert.SerializeObject(Subscribes);
            set => Subscribes = JsonConvert.DeserializeObject<List<string>>(value);
        }
    }
}