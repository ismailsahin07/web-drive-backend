using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Folders.CreateFolder
{
    public sealed record CreateFolderCommand(DriveId DriveId, ItemId ParentId, string Name) : ICommand<ItemId>;
}
