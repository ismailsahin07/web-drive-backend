using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.TrashItem
{
    public sealed record TrashItemCommand(DriveId DriveId, ItemId ItemId) : ICommand;
}
