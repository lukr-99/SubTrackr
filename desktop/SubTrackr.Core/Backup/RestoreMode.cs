namespace SubTrackr.Core.Backup;

/// <summary>How a backup lands on the device (SPEC.md section 9.3).</summary>
public enum RestoreMode
{
    /// <summary>The default: last-writer-wins merge of subscriptions; settings stay.</summary>
    Merge,

    /// <summary>The backup's subscriptions and settings replace the device's, except its sync project.</summary>
    Replace,
}
