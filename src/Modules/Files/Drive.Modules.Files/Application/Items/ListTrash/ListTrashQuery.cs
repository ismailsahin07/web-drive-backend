using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Paging;

namespace Drive.Modules.Files.Application.Items.ListTrash
{
    public sealed record ListTrashQuery(
        DriveId DriveId,
        int? PageSize = null,
        string? ContinuationToken = null) : IQuery<PagedResult<DriveItemDto>>;
}
