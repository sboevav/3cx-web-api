using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebAPI.config;
using WebAPI.integration.dto;

namespace WebAPI.integration.command;

public class ProcessCallEventCommand : ICommand<object>
{
    private readonly AppSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly List<CompanyUrlInfo> _uniqueUrls;
    private readonly CallInfo _callInfo;

    public ProcessCallEventCommand(AppSettings appSettings, HttpClient httpClient, List<CompanyUrlInfo> uniqueUrls, CallInfo callInfo)
    {
        _appSettings = appSettings;
        _httpClient = httpClient;
        _uniqueUrls = uniqueUrls;
        _callInfo = callInfo;
    }

    public async Task<object> ExecuteAsync()
    {
        var tasks = _uniqueUrls.Select(async group =>
        {
            var uriBuilder = new UriBuilder(new Uri(new Uri(group.Url), _appSettings.EndpointReportCall));

            _callInfo.AspIds = group.Ids;
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var jsonContent = JsonConvert.SerializeObject(_callInfo, settings);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uriBuilder.Uri, content);
            response.EnsureSuccessStatusCode();
        });

        await Task.WhenAll(tasks);

        return true;
    }
}
