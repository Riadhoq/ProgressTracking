using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using FEE.Domain.Networking;
using Newtonsoft.Json;
using RestSharp;


namespace FEE.Domain.ProgressTracking
{
    public class BaseProgress<T>
    {
        private static readonly string _apiKey = ConfigurationManager.AppSettings["HubSpotApiKey"];

        public static void PostPropertiesByVid(string vid, List<T> progress, string propertyName)
        {
            try
            {
                var client = new RestClient(baseUrl: "https://api.hubapi.com");

                var request = new RestRequest("https://api.hubapi.com/contacts/v1/contact/vid/{vid}/profile?hapikey={hapikey}");
                request.AddParameter("application/json",
                    JsonConvert.SerializeObject(new
                    {
                        properties = new List<HubSpotPropertyForProgressTracking>
                        {
                            new HubSpotPropertyForProgressTracking
                            {
                                property = propertyName,
                                value = JsonConvert.SerializeObject(progress)
                            }
                        }
                    }), ParameterType.RequestBody);
                request.AddUrlSegment("vid", vid);
                request.AddUrlSegment("hapikey", _apiKey);

                client.Post(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Hubspot Update Properties By Vid Failed: {0}, {1}", ex.Source, ex.InnerException);
            }

        }

        /// 
        /// This helper function sends data to the the HubSpot Forms API
        /// 
        /// HubSpot Portal ID, or 'HUB ID'
        /// Unique ID for the form
        /// Dictionary containing all of the field names/values
        /// UserToken from the visitor's browser
        /// IP Address of the visitor
        /// Title of the page they visited
        /// URL of the page they visited
        /// 
        /// 
        public bool Post_To_HubSpot_FormsAPI(string intPortalID,
            string strFormGUID,
            List<T> progress,
            string strHubSpotUTK,
            string propertyField,
            string strPageTitle,
            string strPageURL,
            ref string strMessage)
        {
            // Build Endpoint URL
            string strEndpointURL = string.Format("https://forms.hubspot.com/uploads/form/v2/{0}/{1}", intPortalID, strFormGUID);

            // Setup HS Context Object
            Dictionary<string, string> hsContext = new Dictionary<string, string>();           
            hsContext.Add("hutk", strHubSpotUTK);
            hsContext.Add("pageUrl", strPageURL);
            hsContext.Add("ipAddress", HttpContext.Current?.Request.GetOriginalHostAddress());
            hsContext.Add("pageName", strPageTitle);
            string progressValue = HttpUtility.UrlEncode(JsonConvert.SerializeObject(progress));

            // Serialize HS Context to JSON (string)
            var strHubSpotContextJSON = JsonConvert.SerializeObject(hsContext);

            // Create string with post data
            string strPostData = "";

            // Add dictionary values

                strPostData += $"{propertyField}={progressValue}&";
            

            // Append HS Context JSON
            strPostData += "hs_context=" + HttpUtility.UrlEncode(strHubSpotContextJSON);

            // Create web request object
            System.Net.HttpWebRequest r = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(strEndpointURL);

            // Set headers for POST
            r.Method = "POST";
            r.ContentType = "application/x-www-form-urlencoded";
            r.ContentLength = strPostData.Length;
            r.KeepAlive = false;

            // POST data to endpoint
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(r.GetRequestStream()))
            {
                try
                {
                    sw.Write(strPostData);
                }
                catch (Exception ex)
                {
                    // POST Request Failed
                    strMessage = ex.Message;
                    return false;
                }
            }

            return true; //POST Succeeded

        }

    }

}
