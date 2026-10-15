using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Items
{
    internal sealed record ItemName
    {
        public const int MaxLength = 255;

        private static readonly SearchValues<char> InvalidCharacters =
            SearchValues.Create("/\\:*?\"<>|");

        private static readonly FrozenSet<string> ReservedDeviceNames = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private ItemName(string value) => Value = value;

        public string Value { get; }

        public string ComparisonKey => Value.ToUpperInvariant();

        public static Result<ItemName> Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ItemNameErrors.Empty;
            }

            if (HasUnpairedSurrogate(value))
            {
                return ItemNameErrors.InvalidCharacters;
            }

            var name = value.Trim().Normalize(NormalizationForm.FormC);

            if (name.Length > MaxLength)
            {
                return ItemNameErrors.TooLong;
            }

            if (name is "." or "..")
            {
                return ItemNameErrors.Reserved;
            }

            if (name.AsSpan().ContainsAny(InvalidCharacters) || name.Any(char.IsControl))
            {
                return ItemNameErrors.InvalidCharacters;
            }

            if (name.EndsWith('.'))
            {
                return ItemNameErrors.EndsWithPeriod;
            }

            if (ReservedDeviceNames.Contains(GetBaseName(name)))
            {
                return ItemNameErrors.Reserved;
            }

            return new ItemName(name);
        }

        public override string ToString() => Value;

        private static string GetBaseName(string name)
        {
            var firstDot = name.IndexOf('.', StringComparison.Ordinal);
            return firstDot < 0 ? name : name[..firstDot];
        }

        private static bool HasUnpairedSurrogate(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                    continue;
                }

                if (char.IsSurrogate(value[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
