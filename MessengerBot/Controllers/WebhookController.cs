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

namespace MessengerBot.Controllers
{
	public class WebhookController : ApiController
	{
		//string pageToken = "DQVJ2SzVFMkpvNmM1QkpNZAUxrX1BMdUZAySW5uaWQ1UnZAkRGFsLU15aW1wZAkpwenNPTjNQSENIT2RBRGtNZAlNfbjZAkQ21vRmk4aTVpdEpGMlVoWl94ZAW4tNWpaMDgySXVqOTlaNnlyLWpfS3dvd3g5OEdnQUlKcnBLTzJxRFJ1dGZAkOVJreUI0b1o3U2NtS2FzUFprME5NcHl2YzNnTEo5a2JxRm9NRnV0MllGSWxkZATZA3a3BMUmZAXWk1BRUxqNjJmcTBMSlhkd2czOVRHTVdISAZDZD";
		//string appSecret = "29e07098746b8ab7ec1b28210dbfe5ac";
        // CIBC Real Time Monitoring App at cibc.facebook.com
        string pageToken = "DQVJ2MjZAZAZA2tiMmhlNTB5S200SGRxbDdZASXZA4VHNuMVhuV3VnTTc5bTQtOE83QXFSSmRQN0g2WHRYV1R2eHJVbkd4MDNlbVNzVl93dm81YVhpaDFBNlN2YUN3SkRmdHpycGF4aWNtaXNPOWlLTWM3NGVCU05ISk5ZAdFFjSFl0S2RkS0pBanJVM0pfeDVvSnI5M1VWdld6SzJGbzgtc0NWSjFBYlJ3Rlh3QUVHNldFQTh0LVdFdDFNN2VVd1VaUm5DS3JLN3YxX2EwN295emtrNgZDZD";
        string appSecret = "016aedf91eb245872fa87648da4b7b14";
        string appI = "309396526132314";

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

			var value = JsonConvert.DeserializeObject<WebhookModel>(body);

            

            if (value._object == "group")
            {
                var json = (JToken.Parse(body))["entry"][0]["changes"][0];

                if (json["field"].ToString() == "posts")
                {
                    string from = json["value"]["from"].ToString();
                    string type = json["value"]["type"].ToString();
                    string target_type = json["value"]["target_type"].ToString();
                    string community = json["value"]["community"].ToString();
                    string post_id = json["value"]["post_id"].ToString();
                    string verb = json["value"]["verb"].ToString();
                    string created_time = json["value"]["created_time"].ToString();
                    string message = json["value"]["message"].ToString();
                    string permalink_url = json["value"]["permalink_url"].ToString();
                }
                else if (json["field"].ToString() == "comments")
                {
                    string from = json["value"]["from"].ToString();
                    string type = json["value"]["type"].ToString();
                    string target_type = json["value"]["target_type"].ToString();
                    string community = json["value"]["community"].ToString();
                    string post_id = json["value"]["post_id"].ToString();
                    string verb = json["value"]["verb"].ToString();
                    string created_time = json["value"]["created_time"].ToString();
                    string message = json["value"]["message"].ToString();
                    string permalink_url = json["value"]["permalink_url"].ToString();
                }
            }
            else if (value._object == "user")
            {
                if (value._object == "message_sends")
                {

                }

            }

            if (value._object != "page")
            {
                //return new HttpResponseMessage(HttpStatusCode.OK);
            }
           
           

            foreach (var item in value.entry[0].messaging)
			{
				if (item.message == null && item.postback == null)
					continue;
				else
					await SendMessage(GetMessageTemplate(item.message.text, item.sender.id));
			}

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
	}
}

/*
 {"entry": 
[{"changes": 
[{"field": "posts", 
"value": {"from": {"id": "100016022019645", "name": "Kiritharan Kanesapillai"}, 
"type": "status", "target_type": "group", 
"community": {"id": "1206397592809948"}, 
"post_id": "483499012017824_545236315844093", 
"verb": "add", 
"created_time": "2018-01-16T02:59:58+0000", 
"message": "iyiiyuiuiu", 
"permalink_url": "https://cibc.facebook.com/groups/483499012017824/permalink/545236315844093/"}}], 
"id": "483499012017824", 
"time": 1516071600}], 
"object": "group"}


{"entry": 
[{"changes": 
[{"field": "comments", 
"value": {"from": {"id": "100016022019645", "name": "Kiritharan Kanesapillai"}, 
"comment_id": "545236832510708", 
"community": {"id": "1206397592809948"}, 
"post_id": "483499012017824_545236315844093", 
"verb": "add", 
"created_time": "2018-01-16T03:03:13+0000",
 "message": "this is comment", 
"permalink_url": "https://cibc.facebook.com/groups/483499012017824/permalink/545236315844093/?comment_id=545236832510708"}}], 
"id": "483499012017824", 
"time": 1516071794}],
 "object": "group"}


{"entry": 
[{"time": 1516071939, 
"changes": [{"field": "message_sends", 
"value": {"created_time": "2018-01-16T03:05:39+0000", 
"to": {"data": [{"id": "100015950115290", "name": "Lahiru Pinnaduwage", 
"community": {"id": "1206397592809948"}, 
"email": "lahiru.pinnaduwage@cibc.com"}]}, 
"message": "fdfdsffs", 
"from": {"id": "100016022019645", "name": "Kiritharan Kanesapillai", 
"community": {"id": "1206397592809948"}, 
"email": "kiritharan.kanesapillai@cibc.com"}, 
"id": "m_mid.$cAAAAAAxbb-dnLjM2Mlg_O1EUcVB7"}}], 
"id": "100016022019645", 
"uid": "100016022019645"}], 
"object": "user"}




{"entry": 
[{"changes": [{"field": "posts", 
"value": {"from": {"id": "100016022019645", "name": "Kiritharan Kanesapillai"}, 
"attachments": {"data": [{"target": {"url": "https://lookaside.fbsbx.com/file/Microsoft.AspNet.WebApi.5.2.3.nupkg?token=AWxyec5Jh3XK8wUZQAXSH5zjQGNiuk5P02GMrqi776yC3SjJsZxXHD3AowJcWt8fXNsWVor_OUX1SbJtVqh5PMIN_Foti-Ot0BtkDwbYCK9PAk-aThIQyD8PLNfahR5jOTnHnfYuS6LsQZRFhf5n9vH0luX2Qdm3k5mY7Z-jmPRcnrRdNtuXwv8Etn4bLC7tyE_t0ZipCuybIv5WKcnFUy2ltVkZBeQOwPFgPq6jb9ue9g"}, "title": "Microsoft.AspNet.WebApi.5.2.3.nupkg \u00b7 version 1", 
"url": "https://lookaside.fbsbx.com/file/Microsoft.AspNet.WebApi.5.2.3.nupkg?token=AWxyec5Jh3XK8wUZQAXSH5zjQGNiuk5P02GMrqi776yC3SjJsZxXHD3AowJcWt8fXNsWVor_OUX1SbJtVqh5PMIN_Foti-Ot0BtkDwbYCK9PAk-aThIQyD8PLNfahR5jOTnHnfYuS6LsQZRFhf5n9vH0luX2Qdm3k5mY7Z-jmPRcnrRdNtuXwv8Etn4bLC7tyE_t0ZipCuybIv5WKcnFUy2ltVkZBeQOwPFgPq6jb9ue9g", 
"media": {"image": {"src": "https://static.xx.fbcdn.net/rsrc.php/v3/yk/r/VCodXOH-RMH.png", "width": 72, "height": 72}}, 
"subattachments": {"data": [{"url": "https://cibc.facebook.com/groups/483499012017824/545237825843942/", 
"type": "album", "target": {"url": "https://cibc.facebook.com/groups/483499012017824/545237825843942/"}, 
"title": "Photos from Kiritharan Kanesapillai's post in Testing Workplace"}]}, "type": "file_upload"}]}, 
"type": "status", "target_type": "group", "community": {"id": "1206397592809948"}, 
"post_id": "483499012017824_545237825843942", 
"verb": "add", 
"created_time": "2018-01-16T03:07:45+0000",
 "permalink_url": "https://cibc.facebook.com/groups/483499012017824/545237825843942/"}}], 
"id": "483499012017824", 
"time": 1516072067}], 
"object": "group"}
     */
