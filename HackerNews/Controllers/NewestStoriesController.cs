using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HackerNews.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace HackerNews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewestStoriesController : ControllerBase
    {

        const string NewestStoriesApi = "https://hacker-news.firebaseio.com/v0/newstories.json";
        const string StoryApiTemplate = "https://hacker-news.firebaseio.com/v0/item/{0}.json";

        private static HttpClient client = new HttpClient();
        private IMemoryCache cache;


        public NewestStoriesController(IMemoryCache memoryCache)
        {
            cache = memoryCache;
        }

        [HttpGet]
        [Route("GetNewestStories")]
        public async Task<List<HackerStorySummary>> GetAsync(string searchString)
        {
            List<HackerStorySummary> stories = new List<HackerStorySummary>();

            string message = "Failed";

            var response = await client.GetAsync(NewestStoriesApi);
            if (response.IsSuccessStatusCode)
            {
                var storiesResponse = response.Content.ReadAsStringAsync().Result;
                var newestIDs = JsonConvert.DeserializeObject<List<int>>(storiesResponse);

                var tasks = newestIDs.Select(GetStoryAsync);
                stories = (await Task.WhenAll(tasks)).ToList();

                if (!String.IsNullOrEmpty(searchString))
                {
                    var search = searchString.ToLower();

                    stories = stories.Where(s =>
                                        s.Title.ToLower().IndexOf(search) > -1 || s.By.ToLower().IndexOf(search) > -1)
                                        .ToList();
                }

                message = "Success";
            }

            return stories;

        }


        private async Task<HackerStorySummary> GetStoryAsync(int storyId)
        {
            return await cache.GetOrCreateAsync<HackerStorySummary>(storyId,
                async cacheEntry => {
                    HackerStorySummary story = new HackerStorySummary();

                    var response = await client.GetAsync(string.Format(StoryApiTemplate, storyId));
                    if (response.IsSuccessStatusCode)
                    {
                        var storyResponse = response.Content.ReadAsStringAsync().Result;
                        story = JsonConvert.DeserializeObject<HackerStorySummary>(storyResponse);
                    }
                    else
                    {
                        story.Title = string.Format("***Error (Not a title): (ID {0})", storyId);
                    }

                    return story;
                });
        }

    }
}
