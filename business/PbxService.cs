using System;
using Microsoft.Extensions.Logging;
using TCX.Configuration;
using WebAPI.config;

namespace WebAPI.business;

public class PbxService
{
    private readonly ILogger<PbxService> _logger;
    private readonly ConfigurationService _configurationService;

    public PbxService(ILogger<PbxService> logger, ConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
        InitializePbx();
    }

    private void InitializePbx()
    {
        _logger.LogInformation("Initializing PBX...");

        PhoneSystem.CfgServerHost = "127.0.0.1";
        PhoneSystem.CfgServerPort = int.Parse(_configurationService.ConfPort);
        PhoneSystem.CfgServerUser = _configurationService.ConfUser;
        PhoneSystem.CfgServerPassword = _configurationService.ConfPass;
        var ps = PhoneSystem.Reset(
            PhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(),
            "127.0.0.1",
            int.Parse(_configurationService.ConfPort),
            _configurationService.ConfUser,
            _configurationService.ConfPass);
        ps.WaitForConnect(TimeSpan.FromSeconds(3));

        if (ps.Connected) _logger.LogInformation("Initialized successfully");
        else _logger.LogError("Initialization failed");
    }

    public string MakeCall(string from, string to, string callerId)
    {
        return MakeDirectCall.Dial(from, to, callerId);
    }

    public string ShowAllCalls()
    {
        return Getcall.showallcall();
    }

}