namespace FinOS.Analytics.Domain.Entities;

public class Scenario
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScenarioType { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
