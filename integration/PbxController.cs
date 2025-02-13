using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebAPI.config;
using WebAPI.integration.command;
using WebAPI.integration.dto;


namespace WebAPI.integration
{
    [ApiController]
    [Route("api/pbx/3cx")]
    public class PbxController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpClient;

        public PbxController(IOptions<AppSettings> appSettings, HttpClient httpClient)
        {
            _appSettings = appSettings.Value;
            _httpClient = httpClient;
        }

        [HttpGet("v20/contact/search")]
        public async Task<IActionResult> SearchContactByPhone(
            [FromQuery] long aspId,
            [FromQuery] string number,
            [FromQuery] string email,
            [FromQuery] string callDirection)
        {
            List<PbxContactDto> result;

            if (!string.IsNullOrEmpty(number))
            {
                result = await RestCommandExecutor.Execute(new PbxSearchContactsByPhoneCommand(_appSettings, _httpClient, GetUniqueUrls(), number));
            }
            else if (!string.IsNullOrEmpty(email))
            {
                result = await RestCommandExecutor.Execute(new PbxSearchContactsByEmailCommand());
            }
            else
            {
                result = new List<PbxContactDto>();
            }

            return Ok(result);
        }

        [HttpPost("v20/call/create")]
        public async Task<IActionResult> CallEvent([FromBody] CallInfo callInfo)
        {
            var result = await RestCommandExecutor.Execute(new ProcessCallEventCommand(_appSettings, _httpClient, GetUniqueUrls(), callInfo));
            return Ok(result);
        }

        private List<CompanyUrlInfo> GetUniqueUrls()
        {
            return _appSettings.Companies
                .GroupBy(c => c.Url)
                .Select(group => new CompanyUrlInfo
                {
                    Url = group.Key,
                    Ids = group.Select(c => c.Id).ToList()
                }).ToList();
        }

    }
}
