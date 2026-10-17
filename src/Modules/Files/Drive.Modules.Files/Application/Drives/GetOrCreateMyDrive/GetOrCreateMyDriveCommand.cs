using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Drives.GetOrCreateMyDrive
{
    public sealed record GetOrCreateMyDriveCommand : ICommand<StorageDriveDto>;
}
