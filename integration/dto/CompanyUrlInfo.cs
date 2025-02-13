using System.Collections.Generic;

namespace WebAPI.integration.dto;

public class CompanyUrlInfo
{
    public string Url { get; set; }
    public List<long> Ids { get; set; }
}