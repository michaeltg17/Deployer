namespace Api.Models;

public sealed record DeployRequest
{
    public string? Project { get; set; }
    public string? Environment { get; set; }
    public string? Tag { get; set; }
}