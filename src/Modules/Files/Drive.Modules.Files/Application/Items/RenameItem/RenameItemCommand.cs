using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.RenameItem
{
    public sealed record RenameItemCommand(DriveId DriveId, ItemId ItemId, string NewName) : ICommand;
}
