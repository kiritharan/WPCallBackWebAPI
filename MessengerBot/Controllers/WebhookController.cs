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
using System.Web;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.Caching;

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

        string wpFilesFilesName = "WorkplaceFiles_";

        private const string cacheFormatKey = "CacheKey_RegexFormat";
        private const string cacheCardInfoKey = "CacheKey_RegexCardInfo";
        private const string cacheBadWordsKey = "CacheKey_RegexBadWords";

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

        /// <summary>
        /// Caution: Post created as an album but without a caption is not captured by this code.
        /// </summary>
        /// <param name="body"></param>
        private void AddingNodeToXmlFile(string body)
        {
            try
            {
                Trace.TraceInformation("Transaction start **");// + body);

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
                    if ((model.entry[0].field == "posts") || (model.entry[0].field == "comments"))
                    {                       
                        Post post = JsonConvert.DeserializeObject<Post>(json["value"].ToString());
                        post.type = model.entry[0].field;
                        model.entry[0].Post = post;
                     
                        if (post.verb == "delete")
                            goto Exit;
                        
                        List<Attachment> col = new List<Attachment>();
                        if (json["value"]["attachments"] != null)
                        {
                            if (json["value"]["attachments"]["data"].Count() > 0)
                            {
                                string val = json["value"]["attachments"]["data"][0]["type"].ToString();

                                if (val != "album")
                                {
                                    Attachment file = new Attachment
                                        (
                                            json["value"]["attachments"]["data"][0]["media"]["image"]["src"].ToString(),
                                            json["value"]["attachments"]["data"][0]["type"].ToString(),
                                            json["value"]["attachments"]["data"][0]["url"].ToString()
                                        );                                    
                                   
                                    // Single photo within this attachment OR Attachment created as a photo album
                                    if (json["value"]["attachments"]["data"][0]["subattachments"] == null && val == "photo")                                  
                                        file.description = json["value"]["attachments"]["data"][0]["description"].ToString();                                  
                                    else
                                        // single photo uploaded as attachment
                                        file.title = json["value"]["attachments"]["data"][0]["title"].ToString();

                                    col.Add(file);
                                }
                                else
                                {
                                    // multiple photo's within this attachment
                                    int counter = counter = json["value"]["attachments"]["data"][0]["subattachments"]["data"].Count();

                                    for (int i = 0; i < counter; i++)
                                    {
                                        Attachment file = new Attachment
                                        (
                                            json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["media"]["image"]["src"].ToString(),
                                            json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["type"].ToString(),
                                            json["value"]["attachments"]["data"][0]["subattachments"]["data"][i]["url"].ToString()
                                        );

                                        col.Add(file);
                                    }
                                }
                                
                            }
                        }


                        if (col.Count > 0)
                        {
                            model.entry[0].attachments = col;
                            //model.entry[0].Post.attachments = col; 
                        }
                       
                        // Add to node if it is add operation (posting, commenting)
                        if (post.verb == "add" || post.verb == "edit")
                            CreateXMLNode(model);
                    }
                }

                // This is from Workplace chat 
                else if (model._object == "user")
                {
                    Trace.TraceInformation("Object: " + model._object);

                    if (model.entry[0].field == "message_sends")
                    {

                        Message msg = JsonConvert.DeserializeObject<Message>(json["value"].ToString());
                        model.entry[0].Message = msg;

                     
                           
                            CreateXMLNode(model);
                      
                    }
                    else if (model.entry[0].field == "status")
                    {
                        Post post = JsonConvert.DeserializeObject<Post>(json["value"].ToString());
                        post.type = model.entry[0].field;
                        model.entry[0].Post = post;
                    }

                    CreateXMLNode(model);
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
                string WPFILES = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPFILES"], DATE);

                if (!Directory.Exists(WPGROUPPOSTS))
                {
                    Trace.TraceInformation("Creating directory to store group posts " + WPGROUPPOSTS);
                    Directory.CreateDirectory(WPGROUPPOSTS);
                }

                if (!Directory.Exists(WPCHAT))
                {
                    Trace.TraceInformation("Creating directory to store chat messages " + WPCHAT);
                    Directory.CreateDirectory(WPCHAT);
                }

                if (!Directory.Exists(WPFILES))
                {
                    Trace.TraceInformation("Creating directory to store file informations " + WPFILES);
                    Directory.CreateDirectory(WPFILES);
                }

            }
            catch(Exception ex) { }
        }

        private Validation GetResourceValues()
        {
            try
            {   

                string REGEXFORMATFILENAME = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,ConfigurationManager.AppSettings["REGEXFORMATFILENAME"]);
                string PARAMETERSFILENAME = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["PARAMETERSFILENAME"]);
                string BADWORDKEYFILENAME = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["BADWORDSFILENAME"]);

                //Trace.TraceInformation("REGEXFORMATFILENAME : " + REGEXFORMATFILENAME);
                //Trace.TraceInformation("PARAMETERSFILENAME : " + PARAMETERSFILENAME);
                //Trace.TraceInformation("BADWORDKEYFILENAME : " + BADWORDKEYFILENAME);

                
                string REGEXSTRING = string.Empty;
                ArrayList REGEXARRAY = new ArrayList();
                string BADWORDKEYSTRING = string.Empty;
                List<CardTypeInfo> CardTypeInfo = null;

                // Store data in the cache
                ObjectCache cache = MemoryCache.Default;              
                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                cacheItemPolicy.AbsoluteExpiration = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);
                foreach (var item in MemoryCache.Default) 
                {
                    Trace.TraceInformation("ObjectCache: " + item.Key + " val: " + item.Value);
                }
                

                // Reading regex formats
                if (!cache.Contains(cacheFormatKey))
                {
                    using (var txt = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), REGEXFORMATFILENAME)))
                    {
                        while (txt.Peek() != -1)
                        {
                            string str = txt.ReadLine();
                            if (REGEXSTRING != "")
                                REGEXSTRING += "|";
                            REGEXSTRING += str.Split(';')[1].Trim();
                            REGEXARRAY.Add(new Regex(str.Split(';')[1].Trim()));
                        }

                        REGEXSTRING = REGEXSTRING.Substring(0, REGEXSTRING.Length - 1);
                        Trace.TraceInformation("REGEXSTRING 1: " + REGEXSTRING);
                        cache.Add(cacheFormatKey, REGEXSTRING, cacheItemPolicy);

                    }
                }
                else
                {
                    REGEXSTRING = cache[cacheFormatKey].ToString();
                    Trace.TraceInformation("REGEXSTRING 2: " + REGEXSTRING);
                }

                // Reading card type informaion
                if (!cache.Contains(cacheCardInfoKey))
                {

                    CardTypeInfo = new List<CardTypeInfo>();

                    using (var txt = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), PARAMETERSFILENAME)))
                    {
                        while (txt.Peek() != -1)
                        {
                            CardTypeInfo.Add(new MessengerBot.Controllers.CardTypeInfo(txt.ReadLine().Split(';')[1].Trim()));
                        }
                        Trace.TraceInformation("REGEXARRAY 1: " + CardTypeInfo[0].RegEx);
                        cache.Add(cacheCardInfoKey, CardTypeInfo, cacheItemPolicy);
                    }
                }
                else
                {
                    REGEXARRAY = (ArrayList)cache[cacheCardInfoKey];
                    Trace.TraceInformation("REGEXARRAY 2: " + CardTypeInfo[0].RegEx);
                }

                // Reading bad words 
                if (!cache.Contains(cacheBadWordsKey))
                {
                    using (var txt = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), BADWORDKEYFILENAME)))
                    {
                        while (txt.Peek() != -1)
                        {
                            string str = txt.ReadLine();
                            if (BADWORDKEYSTRING != "")
                                BADWORDKEYSTRING += "|";
                            BADWORDKEYSTRING += str.Trim().ToLower();
                        }

                        cache.Add(cacheBadWordsKey, BADWORDKEYSTRING, cacheItemPolicy);
                        Trace.TraceInformation("BADWORDKEYSTRING 1: " + BADWORDKEYSTRING);
                    }
                }
                else
                {
                    BADWORDKEYSTRING = cache[cacheBadWordsKey].ToString();
                    Trace.TraceInformation("BADWORDKEYSTRING 2: " + BADWORDKEYSTRING);
                }


                return new Validation(REGEXSTRING, REGEXARRAY, CardTypeInfo);
            }
            catch(Exception ex)
            { }

            return null;
        }

        private string FindingBad(string message)
        {
            string matchedWords = "";
            try
            {
                Validation validation = GetResourceValues();
                matchedWords = validation.Detect(message);

                // No credit card info found
                if (matchedWords == "")
                {
                    ObjectCache cache = MemoryCache.Default;
                    string BADWORDKEYSTRING = cache[cacheBadWordsKey].ToString();
                    Regex r = new Regex(BADWORDKEYSTRING);

                    if (r.IsMatch(message.ToLower()))
                    {
                        MatchCollection matchesCollection = r.Matches(message.ToLower());
                        var matches = from Match m in matchesCollection where m.Value != "" select m;

                        for (int y = 0; y < matches.Count(); y++)
                        {
                            if (matchedWords != string.Empty)
                                matchedWords += ",";
                            matchedWords += matches.ElementAt(y);
                        }
                    }
                }

            }catch (Exception ex) { }

            return matchedWords;
        }
        private void CreateXMLNode(Models.WebhookModel model)
        {
            

            Trace.TraceInformation("Creating and adding XML node to text file...");

            string xmlPostNode = "<Posts GroupName='{0}' GroupID='{1}' PostID='{2}' Type='{7}' PostedTime='{3}' PostedBy='{4}' Message='{5}' Prev_Url='{8}'>{6}</Posts>";
            string xmlPostAttachmentNode = "<Attachment Url='{0}' src='{1}' />";
            string xmlPostAttachmentCollNode = "<Attachments>{0}</Attachments>";
            string xmlFilteredPostNode = "<Posts GroupName='{0}' GroupID='{1}' PostID='{2}' PostedTime='{3}' PostedBy='{4}' Message='{5}' MatchedWords='{6}' />";

            string xmlMesssageAttachmentNode = "<Message MessageID='{0}' CreatedTime='{1}' Message='{2}' ><Participants Participants='{3}' />{4}</ Message >";
            string xmlMessageAttachmentHolderNode = "<Attachment Attachment_ID='{0}' Attachment_mime_type='{1}' Attachment_Name='{2}' size='{3}' src='{4}' />";
            string xmlMessageNode = "<Message MessageID='{0}' CreatedTime='{1}' Message='{2}' ><Participants Participants='{3}' ></ Message>";                    
            string xmlFilteredMessageNode = "<Message MessageID='{0}' CreatedTime='{1}' Message='{2}' MatchedWords='{3}'><Participants Participants='{4}' /></Message>";

            string xmlFileNode = "<File Url='{0}' Src='{1}' />";

            string filePath = string.Empty;
            string filteredFilePath = string.Empty;
            string attachFfilePath = string.Empty;
            string xmlLog = string.Empty;
            string xmlFilesLog = string.Empty;
            string message = string.Empty;
            string matchedWords = string.Empty;

            CreatingDirectories();

            string DATE = DateTime.Today.ToString("MMddyyyy");
            string postDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPGROUPPOSTS"], DATE);
            string chatDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPCHAT"], DATE);
            string filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["WPFILES"],DATE);
            

            // Archiving
            try
            {
                
                switch (model.entry[0].field)
                {
                    // Post to walls
                    case "status":

                        break;

                    case "posts":
                    case "comments":                  

                        filePath = Path.Combine(postDirectory, wpPostsFilesName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        filteredFilePath = Path.Combine(postDirectory, wpPostsFilteredFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        attachFfilePath = Path.Combine(postDirectory, wpPostsAttachmentFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");

                        string groupID = model.entry[0].Post.post_id.Split("_".ToCharArray())[0];
                        string postID = model.entry[0].Post.post_id.Split("_".ToCharArray())[1];
                        string postedDate = model.entry[0].Post.created_time;
                        string postedBy = model.entry[0].Post.from.name + "_" + model.entry[0].Post.from.id;
                        message = model.entry[0].Post.message;
                        string type = model.entry[0].Post.type;
                        string prev_url = model.entry[0].Post.permalink_url;
                        

                        if (model.entry[0].attachments != null)
                        {                           
                            string attachments = string.Empty;

                            for (int i=0; i< model.entry[0].attachments.Count();i++)
                            {

                                string url = model.entry[0].attachments[i].url;
                                string src = model.entry[0].attachments[i].src;

                                attachments += string.Format(xmlPostAttachmentNode, url, src);
                                xmlFilesLog += string.Format(xmlFileNode, url, src);

                            }

                            xmlPostAttachmentCollNode = string.Format(xmlPostAttachmentCollNode, attachments);
                            xmlLog = string.Format(xmlPostNode, string.Empty, groupID, postID, postedDate, postedBy, message, xmlPostAttachmentCollNode, type, prev_url);
                            
                        }
                        else
                            xmlLog = string.Format(xmlPostNode, string.Empty, groupID, postID, postedDate, postedBy, message,"", type, prev_url);


                        matchedWords = FindingBad(message);
                        Trace.TraceInformation("matchedWords 1: " + matchedWords);
                        if (matchedWords !="")
                            xmlFilteredPostNode = string.Format(xmlFilteredPostNode, "", groupID, postID, postedDate, postedBy, message, matchedWords);

                        break;

                    case "message_sends":

                        filePath = Path.Combine(chatDirectory, wpChatFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        filteredFilePath = Path.Combine(chatDirectory, wpChatFilteredFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");
                        attachFfilePath = Path.Combine(chatDirectory, wpChatAttachmentFileName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");

                         
                        string user = model.entry[0].Message.from.name + "(" + model.entry[0].Message.from.id + ")";
                        message = model.entry[0].Message.message;
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

                                    xmlFilesLog += string.Format(xmlFileNode, model.entry[0].Message.attachments.data[i].image_data.preview_url, string.Empty);
                                    
                                }
                                
                                xmlLog = string.Format(xmlMesssageAttachmentNode, 
                                    id, 
                                    createdTme,
                                    message,
                                    participants, 
                                    xmlMessageAttachmentHolderNode);       
                               
                            }
                        }
                        else
                            xmlLog = string.Format(xmlMessageNode, id, createdTme, message, participants);

                        matchedWords = FindingBad(message);
                        Trace.TraceInformation("matchedWords 2!: " + matchedWords);
                        if (matchedWords != "")
                            xmlFilteredMessageNode = string.Format(xmlFilteredMessageNode, id, createdTme, message, matchedWords, participants);

                        break;
                }


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

                if(xmlFilesLog != string.Empty)
                {   
                    wpFilesFilesName = Path.Combine(filesDirectory, wpFilesFilesName + "_" + DateTime.Today.ToString("MMddyyyy") + ".txt");

                    if (!File.Exists(wpFilesFilesName))
                    {
                        Trace.TraceInformation("File being created and add");
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(wpFilesFilesName))
                        {
                            file.WriteLine(xmlFilesLog);
                        }
                    }
                    // Append to an existing file
                    else
                    {
                        Trace.TraceInformation("Add to an existign file");
                        using (StreamWriter sw = File.AppendText(wpFilesFilesName))
                        {
                            sw.WriteLine(xmlFilesLog);
                        }
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

