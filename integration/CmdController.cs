using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.business;

namespace WebAPI.integration
{
    [ApiController]
    [Route("api/cmd")]
    public class CmdController : ControllerBase
    {
        private readonly PbxService _pbxService;

        public CmdController(PbxService pbxService)
        {
            _pbxService = pbxService;
        }

        [HttpPost("makecall/{from}/{to}/{callerId}")]
        [Authorize]
        public IActionResult MakeCall(string from, string to, string callerId)
        {
            var result = _pbxService.MakeCall(from, to, callerId);
            return Ok(result);
        }

        [HttpGet("showallcalls")]
        [Authorize]
        public IActionResult ShowAllCalls()
        {
            var result = _pbxService.ShowAllCalls();
            return Ok(result);
        }

    }

}
