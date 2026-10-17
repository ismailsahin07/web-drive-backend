using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.GetItem
{
    public sealed record GetItemQuery(DriveId DriveId, ItemId ItemId) : IQuery<DriveItemDto>;
}
