using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.Application.Items
{
    internal static class DriveItemMappings
    {
        public static DriveItemDto ToDto(this DriveItem item) => new(
            item.Id.Value,
            item.DriveId.Value,
            item.ParentId?.Value,
            item.Kind.ToString(),
            item.Name.Value,
            item.Status.ToString(),
            item.SizeBytes,
            item.ContentType,
            item.CreatedAt,
            item.CreatedBy.Value,
            item.ModifiedAt,
            item.ModifiedBy.Value,
            item.TrashedAt);
    }
}
