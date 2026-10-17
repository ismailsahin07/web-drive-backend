using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.Application.Drives
{
    internal static class PersonalDrives
    {
        public static DriveId IdFor(UserId ownerId) => DriveId.From(ownerId.Value);
    }
}
