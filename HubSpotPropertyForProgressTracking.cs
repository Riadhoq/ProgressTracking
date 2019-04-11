using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FEE.Domain.Networking;
using Newtonsoft.Json;
using RestSharp;
using Twilio.Http;

namespace FEE.Domain.ProgressTracking
{
    class HubSpotPropertyForProgressTracking
    {
        public string property { get; set; }
        public string value { get; set; }
    }

    public class HubSpotContactForProgressTracking
    {
        private static readonly string apiKey = ConfigurationManager.AppSettings["HubSpotApiKey"];

        private readonly Dictionary<string, string> _dictionary;

        public HubSpotContactForProgressTracking()
        {
            _dictionary = new Dictionary<string, string>();
        }

        public string vid { get; set; }

        public bool isContact { get; set; }

        public string this[string key]
        {
            get
            {
                string retkey;
                _dictionary.TryGetValue(key, out retkey);
                return retkey;
            }
            set => _dictionary[key] = value;
        }

        public static HubSpotContactForProgressTracking GetVidAndPropertyValueByToken(string utk, string property)
        {
            var client = new RestClient(baseUrl: "https://api.hubapi.com");
            if (string.IsNullOrEmpty(utk))
            {
                utk = HttpContext.Current?.Request?.Cookies?.Get("hubspotutk") != null
                    ? HttpContext.Current.Request.Cookies["hubspotutk"].Value
                    : string.Empty;
            }

            var request = new RestRequest("http://api.hubapi.com/contacts/v1/contact/utk/{utk}/profile?hapikey={hapikey}&property={property}");
            request.AddUrlSegment("utk", utk);
            request.AddUrlSegment("hapikey", apiKey);
            request.AddUrlSegment("property", property);
            request.AddHeader("Content-Type", "application/json");

            var result = client.Get(request);

            if (result.ResponseStatus != ResponseStatus.Error)
            {
                dynamic o = JsonConvert.DeserializeObject(result.Content);
                return GetHubSpotContactForProgressTracking(o, property);
            }

            return null;
        }

        private static HubSpotContactForProgressTracking GetHubSpotContactForProgressTracking(dynamic response, string property)
        {
            var hubSpotContact = new HubSpotContactForProgressTracking();

            if (response?.vid != null)
                hubSpotContact.vid = response.vid.ToString();

            if (response?["is-contact"] != null)
                hubSpotContact.isContact = response["is-contact"];
            if (hubSpotContact.isContact)
            {
                if (response?.properties[property]?.value != null)
                    hubSpotContact[property] = response.properties[property].value.ToString();
            }

            return hubSpotContact;
        }
    }

}
