using System;

namespace SS14.Launcher.Models.Data;

/// <summary>
/// One game session started from the launcher.
/// </summary>
public sealed class ConnectionHistoryEntry
{
    public Guid Id { get; init; }
    public string Address { get; init; } = "";
    public string? Name { get; init; }
    public DateTimeOffset Start { get; init; }

    /// <summary>Null while the session is running (or if the launcher was closed before it ended).</summary>
    public DateTimeOffset? End { get; set; }
}
