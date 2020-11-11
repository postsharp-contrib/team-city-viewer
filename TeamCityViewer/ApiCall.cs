using System.Net.Http;
using System.Text;

namespace TeamCityViewer
{
    public class ApiCall
    {
        public static HttpClient Client()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SavedConfig.Instance.Token);
            return httpClient;
        }
    }
}