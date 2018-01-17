using System.Collections.Generic;
using Newtonsoft.Json;

namespace MessengerBot.Models
{
    public class WebhookModel
    {
        [JsonProperty("object")]
        public string _object { get; set; }
        public List<Entry> entry { get; set; }
        
    }

    public class Entry
    {
        public string field { get; set; }
        public string id { get; set; }
        public long time { get; set; }
        public string uid { get; set; }       
        public Message Message { get; set; }
        public Post Post { get; set; }
    }

    public class Post
    {
        public Post() { }
        public User from { get; set; }
        public string type { get; set; }
        public Community community { get; set; }
        public string post_id { get; set; }
        public string verb { get; set; }
        public string created_time { get; set; }
        public string message { get; set; }
        public string permalink_url { get; set; }      
        public AttachmentColl attachments { get; set; }
       // public string target_type { get; set; }
       // public string community_id { get; set; }
    }

    public class AttachmentColl
    {
        public List<Attachment> data { get; set; }
    }

    public class Attachment
    {
        public string description { get; set; }

        public string target { get; set; }
        public string type { get; set; }
        public string url { get; set; }

    }
    public class Message
    {
        public User from { get; set; }
        public List<Recipient> to { get; set; }
        public string created_time { get; set; }
        public string message { get; set; }
        public string messageId { get; set; }
        public string id { get; set; }
    }

    public class User
    {
        public string email { get; set; }

        public string name { get; set; }

        public Community community { get; set; }

        public string id { get; set; }

    }

    public class Recipient
    {
        public List<User> data { get; set; }
    }
    public class Community
    {
        public string id { get; set; }
    }

}
