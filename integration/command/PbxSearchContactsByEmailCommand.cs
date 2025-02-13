using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPI.integration.dto;

namespace WebAPI.integration.command;

public class PbxSearchContactsByEmailCommand : ICommand<List<PbxContactDto>>
{
    public async Task<List<PbxContactDto>> ExecuteAsync()
    {
        return await Task.FromResult(new List<PbxContactDto>());
    }
}