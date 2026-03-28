using Core.Enums;
namespace Core;

public class Package<T>
{
    public string? UserMessage { get; set; }
    public string? DebugMessage { get; set; }
    public T? Data { get; set; }
    public PackageStatus Status { get; set; }
}
