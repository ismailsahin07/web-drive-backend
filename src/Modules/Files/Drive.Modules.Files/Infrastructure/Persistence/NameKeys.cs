using System.Security.Cryptography;
using System.Text;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.Infrastructure.Persistence
{
    internal static class NameKeys
    {
        private const string ClaimIdPrefix = "name-";

        public static string? For(DriveItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return item.ParentId is { } parentId && item.Status != ItemStatus.Trashed
                ? For(parentId, item.Name)
                : null;
        }

        public static string For(ItemId parentId, ItemName name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return $"{parentId}/{name.ComparisonKey}";
        }

        public static string ClaimId(string nameKey)
        {
            ArgumentNullException.ThrowIfNull(nameKey);

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(nameKey));
            return ClaimIdPrefix + Convert.ToHexStringLower(hash);
        }

        public static string SortKey(DriveItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var kindOrder = item.Kind == ItemKind.Folder ? "0" : "1";
            return kindOrder + "|" + item.Name.ComparisonKey;
        }
    }
}
