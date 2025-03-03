using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebAPI.config;
using WebAPI.integration.dto;

namespace WebAPI.integration.command;

public class PbxSearchContactsByPhoneCommand : ICommand<List<PbxContactDto>>
{
    private readonly AppSettings _appSettings;
    private readonly string _number;
    private readonly string _agent;
    private readonly HttpClient _httpClient;
    private readonly List<CompanyUrlInfo> _uniqueUrls;

    public PbxSearchContactsByPhoneCommand(AppSettings appSettings, HttpClient httpClient, List<CompanyUrlInfo> uniqueUrls, string number, string agent)
    {
        _appSettings = appSettings;
        _httpClient = httpClient;
        _uniqueUrls = uniqueUrls;
        _number = number;
        _agent = agent;
    }

    public async Task<List<PbxContactDto>> ExecuteAsync()
    {
        var results = new List<PbxContactDto>();

        var tasks = _uniqueUrls.Select(async group =>
        {
            var uriBuilder = new UriBuilder(new Uri(new Uri(group.Url), _appSettings.EndpointSearchContact))
            {
                Query = $"number={_number}&agent={_agent}&aspIds={string.Join(",", group.Ids)}"
            };

            var response = await _httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var contacts = JsonConvert.DeserializeObject<List<PbxContactDto>>(content);
            
            if (_appSettings.OnlyUsersContacts)
            {
                contacts = contacts.Where(contact => contact.Contact.IsAvailableForUser).ToList();
            }

            results.AddRange(contacts);
        });

        await Task.WhenAll(tasks);

        return results;
    }
}