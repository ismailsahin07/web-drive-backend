using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.MoveItem
{
    public sealed record MoveItemCommand(DriveId DriveId, ItemId ItemId, ItemId DestinationFolderId) : ICommand;
}
