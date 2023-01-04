using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageUpdates.WorkItems
{
    public class WorkItemService : IWorkItemService
    {
        private string _baseUri;
        private string _authorizationToken;

        public WorkItemService(string organization, string project, string authorizationToken)
        {
            _baseUri = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems";
            _authorizationToken = authorizationToken;
        }


        /// <summary>
        /// Creates a workitem
        /// </summary>
        /// <param name="requestBody"> List of json objects describing the workitem</param>
        /// <param name="workItemType">The type of the workitem to be created e.g Task, User Story</param>
        /// <returns></returns>
        public async Task<dynamic> CreateUserStoryAsync(List<object> requestBody, string workItemType="User Story")
        {
            string requestUrl = $"{_baseUri}/${workItemType}?api-version=7.0";

            string jsonRequestBody = JsonConvert.SerializeObject(requestBody);

            HttpClientHandler _httpclienthndlr = new HttpClientHandler();
            using (HttpClient client = new HttpClient(_httpclienthndlr))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authorizationToken);


                var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
                {
                    Content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json-patch+json")
                };

                HttpResponseMessage responseMessage = await client.SendAsync(request);
                responseMessage.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<dynamic>(await responseMessage.Content.ReadAsStringAsync());
            }
        }
    }
}


