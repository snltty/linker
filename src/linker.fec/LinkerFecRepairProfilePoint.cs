namespace linker.fec;

/// <summary>
/// One point in a repair profile. The repair count is absolute for the given source count.
/// Missing source counts between profile points are filled by linear interpolation.
/// </summary>
public readonly record struct LinkerFecRepairProfilePoint(int SourceSymbols, int RepairSymbols);
