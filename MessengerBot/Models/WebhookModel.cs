using System.Collections.Generic;
using Newtonsoft.Json;

namespace MessengerBot.Models
{
    public class WebhookModel
    {
        [JsonProperty("object")]
        public string _object { get; set; }
        public List<Entry> entry { get; set; }       
        public string field { get; set; }
    }

    public class Entry
    {
        List<Post> p = null;
        //public Entry()
        //{
        //    if (p == null)
        //        p = new List<Post>();
        //}
        public string id { get; set; }
        public long time { get; set; }
        public string uid { get; set; }     
        // public List<Change> changes { get; set; }
        public List<Message> Messages { get; set; }
        public void AddPost(Post post)
        {
            if (p == null)
                p = new List<Post>();
            p.Add(post);
        }

        public List<Post> Post
        {
            get { return p; }
        }
    }

    public class Post
    {
        public User from { get; set; }
        public string type { get; set; }
        public Community community { get; set; }
        public string post_id { get; set; }
        public string verb { get; set; }
        public string created_time { get; set; }
        public string message { get; set; }
        public string permalink_url { get; set; }
       // public string target_type { get; set; }
       // public string community_id { get; set; }
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
