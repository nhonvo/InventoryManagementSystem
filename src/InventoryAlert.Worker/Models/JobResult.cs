using System.Diagnostics.CodeAnalysis;

namespace InventoryAlert.Worker.Models;

public enum JobStatus
{
    Success,
    Failed,
    Skipped,
    PartiallySucceeded
}

[ExcludeFromCodeCoverage]
public record JobResult(
    JobStatus Status,
    string Message = "",
    int ProcessedCount = 0,
    Exception? Error = null);



