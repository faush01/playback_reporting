using emby_user_stats.Data;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace emby_user_stats.Api
{
    // http://localhost:8096/emby/user_usage_stats/test5/2017-01-03/Activity

    [Route("/user_usage_stats/{UserID}/{StartDate}/Activity", "GET", Summary = "Gets activity for {USER} from {StartDate} formatted as yyyy-MM-dd")]
    public class GetUserActivity : IReturn<UserAction>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "StartDate", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

        public string UserID { get; set; }
        public string StartDate { get; set; }
        public string filter { get; set; }
    }

    public class UserActivityAPI : IService
    {

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public UserActivityAPI(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public object Get(GetUserActivity activity)
        {
            List<UserAction> items = new List<UserAction>();
            UserAction test = new UserAction();
            test.Id = "Test ID";
            test.ItemId = "Test Item ID";
            test.Date = DateTime.UtcNow;
            test.UserId = activity.UserID;
            items.Add(test);
            return items;
        }

    }
}
