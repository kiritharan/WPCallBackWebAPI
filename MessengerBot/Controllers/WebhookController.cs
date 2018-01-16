using System;
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

namespace MessengerBot.Controllers
{
	public class WebhookController : ApiController
	{
      
        string app_ID = "150764529042457";
        string appSecret = "c237e4905e1785299e4f22cdedcab84f";
        string pageToken = "DQVJ0dnJoZA2VrcGNudk5oWEY5TEtnTlNpdWF2X2dXVWhYQ25Nb1h2Rm1KcmNidzVxNXFFSXZAYM3pLX3pDNHdDSlIxNEp0ZA01CMkIyU1VVSlVlOERRUVY4X1lVNmE0azVGRVRyY3c1ZA190cHZAxRWh3cEtvUUlKbFc1QWhTcjBlSjBnZAFpHNGlJNXBJNmxaS1VFYWhGdkwxQ0ZAwY3Y2Y2wwY21meE13WHpOeXQzbktKbXJzX0VuSUZAKUlRsTVlhNlN0ckxHOTlyRVktTzkzNFgxRgZDZD";

        // CIBC Real Time Monitoring App at cibc.facebook.com
        //string pageToken = "DQVJ2MjZAZAZA2tiMmhlNTB5S200SGRxbDdZASXZA4VHNuMVhuV3VnTTc5bTQtOE83QXFSSmRQN0g2WHRYV1R2eHJVbkd4MDNlbVNzVl93dm81YVhpaDFBNlN2YUN3SkRmdHpycGF4aWNtaXNPOWlLTWM3NGVCU05ISk5ZAdFFjSFl0S2RkS0pBanJVM0pfeDVvSnI5M1VWdld6SzJGbzgtc0NWSjFBYlJ3Rlh3QUVHNldFQTh0LVdFdDFNN2VVd1VaUm5DS3JLN3YxX2EwN295emtrNgZDZD";
        //string appSecret = "016aedf91eb245872fa87648da4b7b14";
        //string appI = "309396526132314";

        string localFilePath = "";

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

           // foreach (var item in value.entry[0].messaging)
           //{

            // if (item.message == null && item.postback == null)
            //	continue;
            //else
            //	await SendMessage(GetMessageTemplate(item.message.text, item.sender.id));
            //}

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
                var value = JsonConvert.DeserializeObject<WebhookModel>(body);
                WebhookModel model = JsonConvert.DeserializeObject<WebhookModel>(body);
                
                model.entry[0].id = value.entry[0].id;
                model.entry[0].uid = value.entry[0].uid;
                model.entry[0].time = value.entry[0].time;
              
                if ((value._object == "group") || (value._object == "user"))
                {
                    var json = (JToken.Parse(body))["entry"][0]["changes"][0];
                    model.field = json["field"].ToString();
                  
                    if ((json["field"].ToString() == "posts") || (json["field"].ToString() == "comments"))
                    {                       
                        Post post = JsonConvert.DeserializeObject<Post>(json["value"].ToString());

                        Trace.TraceInformation("model1qweq2: " + post.community.ToString());
                        Trace.TraceInformation("model1qweq3: " + post.created_time.ToString());
                        Trace.TraceInformation("model1qweq4: " + post.post_id.ToString());
                        Trace.TraceInformation("model1qweq5: " + post.verb.ToString());
                        Trace.TraceInformation("model1qweq6: " + post.message.ToString());
                        Trace.TraceInformation("model1qweq7: " + post.from.ToString());
                        Trace.TraceInformation("model1qweq8: " + post.permalink_url.ToString());
                        try
                        {  
                            model.entry[0].AddPost(post);

                      

                        Trace.TraceInformation("model1: " + model.field.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].type.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].community.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].created_time.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].post_id.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].verb.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].message.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].from.ToString());
                        Trace.TraceInformation("model1: " + model.entry[0].Post[0].permalink_url.ToString());
                        }
                        catch (Exception ex) { Trace.TraceError(ex.ToString()); }
                    }

                    else if (value._object == "user")
                    {
                        if (json["field"].ToString() == "message_sends")
                        {
                            Trace.TraceInformation("Inside message: ");
                            Message msg = JsonConvert.DeserializeObject<Message>(json["value"].ToString());
                            //Message msg = new Message();
                            //msg.created_time = json["value"]["created_time"].ToString();
                            //msg.from = JsonConvert.DeserializeObject<User>(json["value"]["from"].ToString());
                            ////Trace.TraceInformation("to count: " + json["value"]["to"]["data"].Count().ToString());
                            //List<Recipient> recipients = new List<Models.Recipient>();
                            //for (int i = 0; i < json["value"]["to"]["data"].Count(); i++)
                            //{
                            //    //Trace.TraceInformation("to: " + json["value"]["to"]["data"][i]["name"].ToString());
                            //    recipients.Add(JsonConvert.DeserializeObject<Recipient>(json["value"]["to"]["data"][i].ToString()));
                            //}

                            //msg.to = recipients;
                            //msg.message = json["value"]["message"].ToString();
                            //msg.id = json["value"]["id"].ToString();
                            try { 
                            model.entry[0].Messages.Add(msg);
                            }
                            catch (Exception ex) { Trace.TraceError(ex.ToString()); }

                           
                            Trace.TraceInformation("model1: " + model.field.ToString());
                            Trace.TraceInformation("model1: " + model.entry[0].Messages[0].created_time.ToString());
                            Trace.TraceInformation("model1: " + model.entry[0].Messages[0].from.ToString());
                            Trace.TraceInformation("model1: " + model.entry[0].Messages[0].to.ToString());
                            Trace.TraceInformation("model1: " + model.entry[0].Messages[0].message.ToString());
                            Trace.TraceInformation("model1: " + model.entry[0].Messages[0].id.ToString());
                        }
                    }
                    else if (value._object == "page")
                    {
                        //return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }

                //  XmlDocument doc = CheckForFile();

                // AddToXMLFile(doc);

                Trace.TraceInformation("** END **");
            }
            catch(Exception ex) { }
        }

        /// <summary>
        /// This method will check file existense, such as if a file is already created and exists for today ignore creating a new file otherwise create a new file.
        /// </summary>
        private XmlDocument CheckForFile()
        {
            //XML for filtered chat conversations
            XmlDocument docFiltered = new XmlDocument();
            XmlDeclaration xDeclareFiltered = docFiltered.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement documentRootFiltered = docFiltered.DocumentElement;
            docFiltered.InsertBefore(xDeclareFiltered, documentRootFiltered);
            XmlElement filteredUsersElement = (XmlElement)docFiltered.AppendChild(docFiltered.CreateElement("Users"));

            //XML for unfiltered chat conversations
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xDeclare = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement documentRoot = doc.DocumentElement;
            doc.InsertBefore(xDeclare, documentRoot);
            XmlElement usersElement = (XmlElement)doc.AppendChild(doc.CreateElement("Users"));

            return doc;
        }
        private void CreateXMLFile()
        {
                      
        }


        private void AddToXMLFile(XmlDocument doc)
        {
            try
            {
                XmlElement userElement = null;
                XmlElement conversationsElement = null;
                XmlElement filteredUserElement = null;
                XmlElement filteredConversationsElement = null;
                XmlElement exceptionElement = null;



                //userElement = (XmlElement)usersElement.AppendChild(doc.CreateElement("User"));
                //userElement.SetAttribute("WorkplaceUserID", user.id);
                //userElement.SetAttribute("UserName", user.name);
                //conversationsElement = (XmlElement)userElement.AppendChild(doc.CreateElement("Conversations"));


                //filteredUserElement = (XmlElement)filteredUsersElement.AppendChild(docFiltered.CreateElement("User"));
                //filteredUserElement.SetAttribute("WorkplaceUserID", user.id);
                //filteredUserElement.SetAttribute("UserName", user.name);
                //filteredConversationsElement = (XmlElement)filteredUserElement.AppendChild(docFiltered.CreateElement("Conversations"));


                //conversationElement = (XmlElement)conversationsElement.AppendChild(doc.CreateElement("Conversation"));
                //conversationElement.SetAttribute("WorkChatID", json.data[i]["id"].Value);

            }
            catch(Exception ex) { }
        }

        #endregion
    }
}

