using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using MessengerBot.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.Tracing;

using System.Xml;
using System.IO;

namespace MessengerBot.Controllers
{
    public class WebhookController : ApiController
    {
        string app_ID = ConfigurationManager.AppSettings["app_ID"];
        string appSecret = ConfigurationManager.AppSettings["appSecret"];
        string pageToken = ConfigurationManager.AppSettings["pageToken"];


        string wpPostsFilesName = "WorkplacePosts_";
        string wpPostsFilteredFileName = "WorkplacePostsFiltered_";
        string wpPostsAttachmentFileName = "WorkplaceFiles_";

        string wpChatFileName = "WorkChatConversations_";
        string wpChatFilteredFileName = "WorkChatConversationsFiltered_";
        string wpChatAttachmentFileName = "WorkChatFiles_";



        public HttpResponseMessage Get()
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            if (querystrings["hub.verify_token"] == "hello")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            var signature = Request.Headers.GetValues("X-Hub-Signature").FirstOrDefault().Replace("sha1=", "");
            var body = await Request.Content.ReadAsStringAsync();
            if (!VerifySignature(signature, body))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            AddingNodeToXmlFile(body);
        

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private bool VerifySignature(string signature, string body)
        {
            var hashString = new StringBuilder();
            using (var crypto = new HMACSHA1(Encoding.UTF8.GetBytes(appSecret)))
            {
                var hash = crypto.ComputeHash(Encoding.UTF8.GetBytes(body));
                foreach (var item in hash)
                    hashString.Append(item.ToString("X2"));
            }

            return hashString.ToString().ToLower() == signature.ToLower();
        }

        /// <summary>
        /// get text message template
        /// </summary>
        /// <param name="text">text</param>
        /// <param name="sender">sender id</param>
        /// <returns>json</returns>
        private JObject GetMessageTemplate(string text, string sender)
        {
            return JObject.FromObject(new
            {
                recipient = new { id = sender },
                message = new { text = text }
            });
        }

        /// <summary>
        /// send message
        /// </summary>
        /// <param name="json">json</param>
        private async Task SendMessage(JObject json)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage res = await client.PostAsync($"https://graph.facebook.com/v2.6/me/messages?access_token={pageToken}", new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
            }
        }

        #region "Private functions"

        private void AddingNodeToXmlFile(string body)
        {
            try
            {
                Trace.TraceInformation("** Transaction start **" );

                var value = JsonConvert.DeserializeObject<WebhookModel>(body);
                WebhookModel model = JsonConvert.DeserializeObject<WebhookModel>(body);

                model.entry[0].id = value.entry[0].id;
                model.entry[0].uid = value.entry[0].uid;
                model.entry[0].time = value.entry[0].time;

                var json = (JToken.Parse(body))["entry"][0]["changes"][0];
                model.entry[0].field = json["field"].ToString();


                // Thsi is from Workplace group
                if (model._object == "group")
                {
                    Trace.TraceInformation("Group Post and " + model.entry[0].field + " type");

                    if ((model.entry[0].field == "posts") || (model.entry[0].field == "comments"))
                    {
                        Post post = JsonConvert.DeserializeObject<Post>(json["value"].ToString());
                        model.entry[0].Post = post;

                        if (post.verb == "delete")
                            goto Exit;

                        List<Attachment> col = new List<Attachment>();

                        if (json["value"]["attachments"] != null)
                        {
                            if (json["value"]["attachments"]["data"].Count() > 0)
                            {
                                string val = json["value"]["attachments"]["data"][0]["type"].ToString();

                                // Single photo within this attachment
                                if (json["value"]["attachments"]["data"][0]["subattachments"] == null && val == "photo")
                                {

                                    Attachment file = new Attachment();
                                    file.description = json["value"]["attachments"]["data"][0]["description"].ToString();
                                    file.src = json["value"]["attachments"]["data"][0]["media"]["image"]["src"].ToString();
                                    file.type = json["value"]["attachments"]["data"][0]["type"].ToString();
                                    file.url = json["value"]["attachments"]["data"][0]["url"].ToString();
                                    col.Add(file);


                                }
                                // single photo uploaded as attachment
                                else if (val == "file_upload")
                                {
                                    Attachment file = new Attachment();
                                    file.src = json["value"]["attachments"]["data"][0]["media"]["image"]["src"].ToString();
                                    file.type = json["value"]["attachments"]["data"][0]["type"].ToString();
                                    file.url = json["value"]["attachments"]["data"][0]["url"].ToString();
                                    file.title = json["value"]["attachments"]["data"][0]["title"].ToString();
                                    col.Add(file);
                                }
                                // multiple photo's within this attachment
                                else if (val == "album") 
                                {

                                    int counter = counter = json["value"]["attachments"]["data"][0]["subattachments"]["data"].Count();
                                    for (int i = 0; i < counter; i++)
                                    {
                                        Attachment file = new Attachment();
                                        file.src = json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["media"]["image"]["src"].ToString();
                                        file.type = json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["type"].ToString();
                                        file.url = json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["url"].ToString();
                                        col.Add(file);
                                    }

                                }
                                // AttachmentColl attachments = JsonConvert.DeserializeObject<AttachmentColl>(json["value"]["attachments"].ToString());
                                // model.entry[0].attachments = attachments;
                            }
                        }

                        if (col.Count > 0)
                            model.entry[0].attachments = col;

                        //  Trace.TraceInformation("Attachments : " + model.entry[0].Post.attachments.data.Count().ToString());

                        // Add to node if it is add operation (posting, commenting)
                        if (post.verb == "add")
                            CreateXMLNode(model);
                    }
                }

                // This is from Workplace chat 
                else if (model._object == "user")
                {
                    Trace.TraceInformation("Group Post: ");

                    if (model.entry[0].field == "message_sends")
                    {
                        Trace.TraceInformation("Group Post-field : " + model.entry[0].field);


                        Message msg = JsonConvert.DeserializeObject<Message>(json["value"].ToString());
                        model.entry[0].Message = msg;
                         
                        try
                        {
                            model.entry[0].Message = msg;
                            CreateXMLNode(model);
                        }
                        catch (Exception ex) { Trace.TraceError(ex.ToString()); }

                    }
                }               

                Exit:
                    Trace.TraceInformation("** Transaction end **");
            }
            catch (Exception ex)
            {

            }
        }
      
        private void CreatingDirectories()
        {
            try
            {
                string DATE = DateTime.Today.ToString("MMddyyyy");
                string WPGROUPPOSTS = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPGROUPPOSTS"], DATE);
                string WPCHAT = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPCHAT"], DATE);
                
                if (!Directory.Exists(WPGROUPPOSTS))
                {
                    Trace.TraceInformation("Creating directory " + WPGROUPPOSTS);
                    Directory.CreateDirectory(WPGROUPPOSTS);
                }

                if (!Directory.Exists(WPCHAT))
                {
                    Trace.TraceInformation("Creating directory " + WPCHAT);
                    Directory.CreateDirectory(WPCHAT);
                }

            }
            catch(Exception ex) { }
        }

        private void CreateXMLNode(Models.WebhookModel model)
        {
            

            Trace.TraceInformation("Creating and adding XML node to text file...");

            string xmlPostNode = "<Posts GroupName='{0}' GroupID='{1}' PostID='{2}' PostedTime='{3}' PostedBy='{4}' Message='{5}' Link='{6}'/>";
            string xmlMesssageAttachmentNode = "<Message MessageID='{0}' CreatedTime='{1}' Message='{2}' ><Participants Participants='{3}' />{4}</ Message >";
            string xmlMessageAttachmentHolderNode = "<Attachment Attachment_ID='{0}' Attachment_mime_type='{1}' Attachment_Name='{2}' size='{3}' src='{4}' />";
            string xmlMessageNode = "<Message MessageID='{0}' CreatedTime='{1}' Message='{2}' ><Participants Participants='{3}' ></ Message>";
            
            string filePath = string.Empty;
            string filteredFilePath = string.Empty;
            string attachFfilePath = string.Empty;
            string xmlLog = string.Empty;


            CreatingDirectories();

            string DATE = DateTime.Today.ToString("MMddyyyy");
            string postDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPGROUPPOSTS"], DATE);
            string chatDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPCHAT"], DATE);



            try
            {

                switch (model.entry[0].field)
                {
                    case "posts":
                    case "comments":

                        filePath = Path.Combine(postDirectory, wpPostsFilesName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        filteredFilePath = Path.Combine(postDirectory, wpPostsFilteredFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        attachFfilePath = Path.Combine(postDirectory, wpPostsAttachmentFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");

                        string groupID = model.entry[0].Post.post_id.Split("_".ToCharArray())[0];
                        string postID = model.entry[0].Post.post_id.Split("_".ToCharArray())[1];
                        string postedDate = model.entry[0].Post.created_time;
                        string postedBy = model.entry[0].Post.from.name + "_" + model.entry[0].Post.from.id;
                        string message = model.entry[0].Post.message;

                        xmlLog = string.Format(xmlPostNode, string.Empty, groupID, postID, postedDate, postedBy, message, string.Empty);

                        break;

                    case "message_sends":

                        filePath = Path.Combine(chatDirectory, wpChatFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        filteredFilePath = Path.Combine(chatDirectory, wpChatFilteredFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        attachFfilePath = Path.Combine(chatDirectory, wpChatAttachmentFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");

                        //filePath = AppDomain.CurrentDomain.BaseDirectory + "/" + ConfigurationManager.AppSettings["WORKPLACECHATFILE"] + "/" + wpChatFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt";
                        //filteredfilePath = AppDomain.CurrentDomain.BaseDirectory + "/" + ConfigurationManager.AppSettings["WORKPLACECHATFILE"] + "/" + wpFilteredChatFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt";
                        
                        string user = model.entry[0].Message.from.name + "(" + model.entry[0].Message.from.id + ")";
                        string msg = model.entry[0].Message.message;
                        string id = model.entry[0].Message.id;
                        string createdTme = model.entry[0].Message.created_time;
                        string participants = model.entry[0].Message.from.name + "(" + model.entry[0].Message.from.id + ");";


                        for (int i = 0; i < model.entry[0].Message.to.data.Count; i++)
                            participants += model.entry[0].Message.to.data[i].name + "(" + model.entry[0].Message.to.data[i].id + ");";

                        // Chat message with attachment
                        if(model.entry[0].Message.attachments != null)
                        {
                            if (model.entry[0].Message.attachments.data.Count > 0)
                            {
                                for (int i = 0; i < model.entry[0].Message.attachments.data.Count; i++)
                                {                                    
                                    xmlMessageAttachmentHolderNode = string.Format(xmlMessageAttachmentHolderNode,
                                        model.entry[0].Message.attachments.data[i].id, 
                                        model.entry[0].Message.attachments.data[i].mime_type, 
                                        model.entry[0].Message.attachments.data[i].name,
                                        model.entry[0].Message.attachments.data[i].size, 
                                        model.entry[0].Message.attachments.data[i].image_data.preview_url);
                                    Trace.TraceInformation("xmlMessageAttachmentHolderNode : " + xmlMessageAttachmentHolderNode);
                                }
                                
                                xmlLog = string.Format(xmlMesssageAttachmentNode, 
                                    id, 
                                    createdTme,
                                    msg,
                                    participants, 
                                    xmlMessageAttachmentHolderNode);
                                Trace.TraceInformation("xmlLog : " + xmlLog);
                            }
                        }
                        else
                            xmlLog = string.Format(xmlMessageNode, id, createdTme, msg, participants);

                        break;

                }


                //string wpPostsFilesName = "WorkplacePosts_";
                //string wpPostsFilteredFileName = "WorkplacePostsFiltered_";
                //string wpPostsAttachmentFileName = "WorkplaceFiles_";

                //string wpChatFileName = "WorkChatConversations_";
                //string wpChatFilteredFileName = "WorkChatConversationsFiltered_";
                //string wpChatAttachmentFileName = "WorkChatFiles_";

                // Create a file if its not exist
                if (!File.Exists(filePath))
                {
                    Trace.TraceInformation("File being created and add");
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
                    {
                        file.WriteLine(xmlLog);
                    }
                }
                // Append to an existing file
                else
                {
                    Trace.TraceInformation("Add to an existign file");
                    using (StreamWriter sw = File.AppendText(filePath))
                    {
                        sw.WriteLine(xmlLog);
                    }
                }

                //// Read file and write to Trace
                //if (!File.Exists(filePath))
                //{
                //    Trace.TraceInformation("Reading");
                //    using (StreamReader sr = File.OpenText(filePath))
                //    {
                //        string s = "";
                //        while ((s = sr.ReadLine()) != null)
                //        {
                //            Trace.TraceInformation(s);
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.ToString());

            }
        }

        #endregion
    }
}

