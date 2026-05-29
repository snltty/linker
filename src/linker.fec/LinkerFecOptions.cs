using System;
using System.Collections.Generic;
using System.Linq;

namespace linker.fec;

public enum LinkerFecRepairGenerationMode
{
    Auto,
    SourceCoefficients,
    IntermediateSymbols
}

public sealed class LinkerFecOptions
{
    public const int MinSymbolSize = 64;
    public const int MaxSymbolSize = MaxFrameLength - LinkerFecEncodedSymbol.HeaderSize - LinkerFecEncodedSymbol.RepairLengthSymbolSize;
    public const int MinSourceSymbolsPerBlock = 1;
    public const int MaxSourceSymbolsPerBlock = byte.MaxValue;
    public const int MinRepairSymbolsPerBlock = 1;
    public const int MaxRepairSymbolsPerBlock = byte.MaxValue;
    public const int MaxSymbolsPerBlock = byte.MaxValue + 1;
    public const int RecordLengthPrefixSize = sizeof(ushort);
    public const int MaxRecordPayloadLength = ushort.MaxValue;
    public const int FrameLengthPrefixSize = sizeof(ushort);
    public const int MaxFrameLength = ushort.MaxValue;

    public int SymbolSize { get; init; } = 1420 + LinkerFecEncodedSymbol.HeaderSize;
    public int SourceSymbolsPerBlock { get; init; } = 10;
    public int RepairSymbolsPerBlock { get; init; } = 2;
    public IReadOnlyList<LinkerFecRepairProfilePoint>? RepairProfile { get; init; }
    public int MaxDecoderBlocks { get; init; } = 256;
    public int MaxSkipBlocks { get; init; } = 30;
    public LinkerFecRepairGenerationMode RepairGenerationMode { get; init; } = LinkerFecRepairGenerationMode.Auto;

    public int MaxSourceSymbolsPerEncodedBlock
    {
        get
        {
            var profile = RepairProfile;
            if (profile is null || profile.Count == 0)
            {
                return SourceSymbolsPerBlock;
            }
            return profile.Max(c => c.SourceSymbols);
        }
    }
    public int MaxRepairSymbolsPerEncodedBlock
    {
        get
        {
            var profile = RepairProfile;
            if (profile is null || profile.Count == 0)
            {
                return RepairSymbolsPerBlock;
            }
            return profile.Max(c => c.RepairSymbols);
        }
    }

    public int MaxEncodeBufferSize => checked(
        SourceSymbolsPerBlock * (FrameLengthPrefixSize + LinkerFecEncodedSymbol.HeaderSize + SymbolSize) +
        MaxRepairSymbolsPerEncodedBlock * (FrameLengthPrefixSize + LinkerFecEncodedSymbol.HeaderSize + sizeof(ushort) + SymbolSize));

    public int MaxDecodeBufferSize => checked((SymbolSize + RecordLengthPrefixSize) * SourceSymbolsPerBlock);

    public int GetRepairSymbolsForSourceCount(int sourceSymbolCount)
    {
        if (sourceSymbolCount is < MinSourceSymbolsPerBlock or > MaxSourceSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount,
                $"Source symbol count must be in [{MinSourceSymbolsPerBlock}, {MaxSourceSymbolsPerBlock}].");
        }

        if (sourceSymbolCount > SourceSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount,
                "Source symbol count cannot exceed the configured source symbols per block.");
        }

        var profile = RepairProfile;
        if (profile is null || profile.Count == 0)
        {
            return RepairSymbolsPerBlock;
        }

        var previous = profile[0];
        if (sourceSymbolCount <= previous.SourceSymbols)
        {
            return previous.RepairSymbols;
        }

        for (var i = 1; i < profile.Count; i++)
        {
            var next = profile[i];
            if (sourceSymbolCount == next.SourceSymbols)
            {
                return next.RepairSymbols;
            }

            if (sourceSymbolCount < next.SourceSymbols)
            {
                var sourceDelta = next.SourceSymbols - previous.SourceSymbols;
                var repairDelta = next.RepairSymbols - previous.RepairSymbols;
                var interpolated = previous.RepairSymbols +
                    (repairDelta * (double)(sourceSymbolCount - previous.SourceSymbols) / sourceDelta);
                return checked((int)Math.Ceiling(interpolated));
            }

            previous = next;
        }

        return previous.RepairSymbols;
    }

    internal void Validate()
    {
        if (SymbolSize is < MinSymbolSize or > MaxSymbolSize)
        {
            throw new ArgumentOutOfRangeException(nameof(SymbolSize), SymbolSize,
                $"Symbol size must be in [{MinSymbolSize}, {MaxSymbolSize}].");
        }

        if (SourceSymbolsPerBlock is < MinSourceSymbolsPerBlock or > MaxSourceSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(SourceSymbolsPerBlock), SourceSymbolsPerBlock,
                $"Source symbol count must be in [{MinSourceSymbolsPerBlock}, {MaxSourceSymbolsPerBlock}].");
        }

        if (RepairSymbolsPerBlock is < MinRepairSymbolsPerBlock or > MaxRepairSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(RepairSymbolsPerBlock), RepairSymbolsPerBlock,
                $"Repair symbol count must be in [{MinRepairSymbolsPerBlock}, {MaxRepairSymbolsPerBlock}].");
        }

        ValidateRepairProfile();

        if (SourceSymbolsPerBlock + MaxRepairSymbolsPerEncodedBlock > MaxSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(RepairSymbolsPerBlock),
                "The compact frame format supports at most 256 total source and repair symbols per block.");
        }

        if ((long)(SymbolSize + RecordLengthPrefixSize) * SourceSymbolsPerBlock > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(SourceSymbolsPerBlock),
                "The configured decoded record list is larger than a single .NET byte array.");
        }

        if (MaxDecoderBlocks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDecoderBlocks), MaxDecoderBlocks,
                "The decoder block limit must be positive.");
        }

        if (MaxSkipBlocks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSkipBlocks), MaxSkipBlocks,
                "The decoder skip window must be positive.");
        }

        if (MaxSkipBlocks > MaxDecoderBlocks)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSkipBlocks), MaxSkipBlocks,
                "The decoder skip window cannot exceed the decoder block limit.");
        }

        if (!Enum.IsDefined(RepairGenerationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(RepairGenerationMode), RepairGenerationMode,
                "Invalid repair generation mode.");
        }

        for (var sourceCount = MinSourceSymbolsPerBlock; sourceCount <= SourceSymbolsPerBlock; sourceCount++)
        {
            var repairCount = GetRepairSymbolsForSourceCount(sourceCount);
            if (repairCount is < MinRepairSymbolsPerBlock or > MaxRepairSymbolsPerBlock)
            {
                throw new ArgumentOutOfRangeException(nameof(RepairProfile), repairCount,
                    "Repair profile generated an invalid repair symbol count.");
            }

            if (sourceCount + repairCount > MaxSymbolsPerBlock)
            {
                throw new ArgumentOutOfRangeException(nameof(RepairProfile),
                    "Repair profile generated a block that exceeds the compact 256-symbol limit.");
            }
        }
    }

    private void ValidateRepairProfile()
    {
        var profile = RepairProfile;
        if (profile is null)
        {
            return;
        }

        if (profile.Count == 0)
        {
            throw new ArgumentException("Repair profile cannot be empty.", nameof(RepairProfile));
        }

        var previousSourceSymbols = 0;
        for (var i = 0; i < profile.Count; i++)
        {
            var point = profile[i];
            if (point.SourceSymbols is < MinSourceSymbolsPerBlock or > MaxSourceSymbolsPerBlock)
            {
                throw new ArgumentOutOfRangeException(nameof(RepairProfile), point.SourceSymbols,
                    $"Repair profile source count must be in [{MinSourceSymbolsPerBlock}, {MaxSourceSymbolsPerBlock}].");
            }

            if (point.SourceSymbols > SourceSymbolsPerBlock)
            {
                throw new ArgumentOutOfRangeException(nameof(RepairProfile), point.SourceSymbols,
                    "Repair profile source count cannot exceed the configured source symbols per block.");
            }

            if (point.SourceSymbols <= previousSourceSymbols)
            {
                throw new ArgumentException("Repair profile source counts must be in strictly ascending order.", nameof(RepairProfile));
            }

            if (point.RepairSymbols is < MinRepairSymbolsPerBlock or > MaxRepairSymbolsPerBlock)
            {
                throw new ArgumentOutOfRangeException(nameof(RepairProfile), point.RepairSymbols,
                    $"Repair profile repair count must be in [{MinRepairSymbolsPerBlock}, {MaxRepairSymbolsPerBlock}].");
            }

            previousSourceSymbols = point.SourceSymbols;
        }

        if (profile[^1].SourceSymbols != SourceSymbolsPerBlock)
        {
            throw new ArgumentException("Repair profile must end at SourceSymbolsPerBlock.", nameof(RepairProfile));
        }
    }
}
