using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebAPI
{
    public class SsoClient
    {
        private readonly HttpClient _httpClient;

        public SsoClient(String ssoUri)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(ssoUri) };
        }

        public async Task<ApiResponse> GetPublicKeyAsync(Guid keyId)
        {
            var requestPayload = new
            {
                jsonrpc = "2.0",
                method = "getKey",
                @params = new
                {
                    keyId = keyId.ToString()
                }
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("/internal", requestContent);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Request error: " + e.Message);
                throw new ApplicationException("Error while making request to SSO service", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("General error: " + e.Message);
                throw;
            }

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var responseObject = JsonConvert.DeserializeObject<ApiResponse>(responseString);

                if (responseObject == null || responseObject.result == null)
                {
                    Console.WriteLine("Failed to parse response.");
                }
                return responseObject;
            }
            catch (JsonException e)
            {
                Console.WriteLine("JSON parsing error: " + e.Message);
                throw new ApplicationException("Error while parsing response from SSO service", e);
            }
         }
    }

    public class Result
    {
        public string PublicKey { get; set; }
    }

    public class ApiResponse
    {
        public Result result { get; set; }
        public string jsonrpc { get; set; }
    }
   
}