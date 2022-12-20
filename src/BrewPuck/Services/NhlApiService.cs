namespace BrewPuck.Services
{
    public class NhlApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NhlApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
    }
}
