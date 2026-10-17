using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Downloads.GetDownloadUrl
{
    public sealed record GetDownloadUrlQuery(DriveId DriveId, ItemId ItemId) : IQuery<DownloadLink>;
}
