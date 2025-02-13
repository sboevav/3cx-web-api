namespace WebAPI.integration.dto;

public class PbxContactDto
{
    public PbxContactInfoDto Contact { get; set; }
}

public class PbxContactInfoDto
{
    public long Id { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Company { get; set; }
    public string Email { get; set; }
    public string BusinessPhone { get; set; }
    public string BusinessPhone2 { get; set; }
    public string MobilePhone { get; set; }
    public string MobilePhone2 { get; set; }
    public string Url { get; set; }
    public string CustomValue { get; set; }
}