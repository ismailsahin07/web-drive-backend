using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Paging;

namespace Drive.Modules.Files.Application.Items.ListChildren
{
    public sealed record ListChildrenQuery(
        DriveId DriveId,
        ItemId FolderId,
        int? PageSize = null,
        string? ContinuationToken = null) : IQuery<PagedResult<DriveItemDto>>;
}
