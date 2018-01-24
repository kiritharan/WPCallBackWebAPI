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
        public List<Attachment> attachments { get; set; }
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
      //  public List<Attachment> attachments { get; set; }

       

        //public class Postsubattachments
        //{
        //    public List<Attachment> data { get; set; }
        //}

    }

    //public PostAttachment attachments { get; set; }

    public class PostAttachment
    {
        public List<Attachment> data { get; set; }
    }

    public class Attachment
    {      
        public Attachment(string src, string type, string url)
        {
           
            this.type = type;
            this.url = url;
            this.src = src;
        }

        public string description { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string src { get; set; }
        public string title { get; set; }

        // Following for attachments in message
        public string preview { get; set; }
        public string size { get; set; }
        public string id { get; set; }
        public string mime_type { get; set; }
        public string name { get; set; }
        public ImageData image_data { get; set; }

      
    }
    public class ImageData
    {
        public string url { get; set; }
        public string preview { get; set; }
        public string raw_gif_image { get; set; }
        public string preview_url { get; set; }
        public string animated_gif_preview_url { get; set; }
        public string animated_gif_url { get; set; }

    }
    public class MessageAttachment
    {
        public List<Attachment> data { get; set; }
    }
    public class Message
    {
        public User from { get; set; }
        public Recipient to { get; set; }
        public string created_time { get; set; }
        public string message { get; set; }
        public string id { get; set; }
        public MessageAttachment attachments { get; set; }
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
