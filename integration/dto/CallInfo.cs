using System.Collections.Generic;

namespace WebAPI.integration.dto;

public class CallInfo
{
    public List<long> AspIds { get; set; }
    public string ExtNumber { get; set; }
    public string CallType { get; set; }
    public string CallDirection { get; set; }
    public string Name { get; set; }
    public string EntityId { get; set; }
    public string EntityType { get; set; }
    public string Agent { get; set; }
    public string AgentFirstName { get; set; }
    public string AgentLastName { get; set; }
    public string AgentEmail { get; set; }
    public long? DurationMs { get; set; }
    public long? CallStartTimeUTCMs { get; set; }
    public long? CallEstablishedTimeUTCMs { get; set; }
    public long? CallEndTimeUTCMillis { get; set; }
    public string Subject { get; set; }
    public string InboundCallText { get; set; }
    public string MissedCallText { get; set; }
    public string OutboundCallText { get; set; }
    public string NotAnsweredOutboundCallText { get; set; }
}