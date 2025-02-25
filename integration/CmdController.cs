using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAPI.business;

namespace WebAPI.integration
{
    [ApiController]
    [Route("api/cmd")]
    public class CmdController : ControllerBase
    {
        private readonly ILogger<CmdController> _logger;
        private readonly PbxService _pbxService;

        public CmdController(ILogger<CmdController> logger, PbxService pbxService)
        {
            _logger = logger;
            _pbxService = pbxService;
        }

        [HttpPost("makecall/{from}/{to}/{callerId}")]
        public IActionResult MakeCall(string from, string to, string callerId)
        {
            _logger.LogDebug("MakeCall: from={from}, to={to}, callerId={callerId}", from, to, callerId);
            var result = _pbxService.MakeCall(from, to, callerId);
            return Ok(result);
        }

        [HttpGet("showallcalls")]
        public IActionResult ShowAllCalls()
        {
            _logger.LogDebug("ShowAllCalls");
            var result = _pbxService.ShowAllCalls();
            return Ok(result);
        }

    }

}
