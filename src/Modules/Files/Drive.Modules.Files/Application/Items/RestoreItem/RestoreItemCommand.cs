using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.RestoreItem
{
    public sealed record RestoreItemCommand(DriveId DriveId, ItemId ItemId) : ICommand;
}
