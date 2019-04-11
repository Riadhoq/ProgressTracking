using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Umbraco.Core.Models;


namespace FEE.Domain.ProgressTracking
{
    public class PlatformCenterProgress : BaseProgress<CategoryProgress>
    {
        //internal name of the property
        private static readonly string lc_articles_read = "lc_articles_read";
        private static HubSpotContactForProgressTracking _hubspotProgressContact;
        private static readonly string formGuid = "b64d497e-981e-4d11-954e-0b9d0d02510f";
        private static List<CategoryProgress> _hubspotProgress;
        private static IPublishedContent _model;
        private static string _utk;
        public static readonly string _portalId = ConfigurationManager.AppSettings["HubSpotPortalId"];

        public LearningCenterProgress(IPublishedContent model, string utk)
        {
            
            if (string.IsNullOrEmpty(utk))
            {
                _utk = HttpContext.Current?.Request?.Cookies?.Get("hubspotutk") != null
                    ? HttpContext.Current.Request.Cookies["hubspotutk"].Value
                    : string.Empty;
            }
            else
            {
                _utk = utk;
            }

            _hubspotProgressContact = HubSpotContactForProgressTracking.GetVidAndPropertyValueByToken(utk, lc_articles_read);
            _hubspotProgress = _hubspotProgressContact?[lc_articles_read] != null ? JsonConvert.DeserializeObject<List<CategoryProgress>>(_hubspotProgressContact[lc_articles_read]) : null;
            _model = model;
        }
        public List<string> GetReadArticleListFromHubSpot()
        {
           
            var readArticles = _hubspotProgress != null ? _hubspotProgress.Where(hsProgress => hsProgress.Name == _model.Parent.Parent.UrlName && hsProgress.Topics.Any(x=> x.Name == _model.Parent.UrlName))
                .SelectMany(hsProgress =>
                    hsProgress.Topics.Find(topic => topic.Name == _model.Parent.UrlName).Articles) : new List<string>();

            return readArticles.ToList();
        }

        public void PostNewHubSpotPropertyValue()
        {
            var newHubspotProgress = CreateNewProgress();
            if (_hubspotProgressContact.isContact)
            {
                PostPropertiesByVid(_hubspotProgressContact.vid, newHubspotProgress, lc_articles_read);
            }
            else
            {
                Post_To_HubSpot_FormsAPI(_portalId, formGuid, newHubspotProgress, _utk, lc_articles_read, _model.Name,
                    _model.Url, ref _utk);
            }
        }

        public List<CategoryProgress> CreateNewProgress()
        {
            string article = _model.UrlName;
            TopicProgress lcTopic = new TopicProgress {Name = _model.Parent.UrlName, Articles = new List<string> {article}};
            CategoryProgress lcCategory =
                new CategoryProgress {Name = _model.Parent.Parent.UrlName, Topics = new List<TopicProgress> {lcTopic}};
            List<CategoryProgress> lcCategoryList = new List<CategoryProgress> {lcCategory};

            if (_hubspotProgress == null)
            {
                return lcCategoryList;
            }
            else
            {   // Check if category is already in hubspot
                if (_hubspotProgress.Any(x => x.Name == lcCategory.Name))
                {
                    foreach (var categoryProgress in _hubspotProgress.Where(x => x.Name == lcCategory.Name))
                    {   // Check if topic is already in hubspot
                        if (categoryProgress.Topics.Any(x => x.Name == lcTopic.Name))
                        {
                            foreach (var progress in categoryProgress.Topics.Where(x => x.Name == lcTopic.Name))
                            {   // Check if article is already in hubspot
                                if (progress.Articles.Contains(article) == false)
                                {
                                    progress.Articles.Add(article);
                                    return _hubspotProgress;
                                }                              
                            }
                            return _hubspotProgress;
                        }
                        else
                        {
                            categoryProgress.Topics.Add(lcTopic);
                            return _hubspotProgress;
                        }

                    }
                }
                else
                {
                    _hubspotProgress.AddRange(lcCategoryList);
                    return _hubspotProgress;
                }
            }

            return new List<CategoryProgress>();

        }

    }

    public class TopicProgress
    {
        public string Name { get; set; }
        public List<string> Articles { get; set; }
    }

    public class CategoryProgress
    {
        public string Name { get; set; }
        public List<TopicProgress> Topics { get; set; }
    }

}
