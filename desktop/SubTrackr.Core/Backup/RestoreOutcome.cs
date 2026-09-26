using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Backup;

/// <summary>The database a restore builds, with the counts SPEC.md section 9.3 reports.</summary>
public sealed record RestoreOutcome(Database Result, int Added, int Updated, int Unchanged, int Total);
