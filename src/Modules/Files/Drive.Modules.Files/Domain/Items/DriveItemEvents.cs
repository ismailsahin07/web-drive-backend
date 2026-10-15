using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.Domain.Items
{
    internal sealed record FolderCreatedDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        ItemId ParentId,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    internal sealed record FileUploadedDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        long SizeBytes,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    internal sealed record ItemRenamedDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        ItemName OldName,
        ItemName NewName,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    internal sealed record ItemMovedDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        ItemId FromParentId,
        ItemId ToParentId,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    internal sealed record ItemTrashedDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    internal sealed record ItemRestoredDomainEvent(
        ItemId ItemId,
        DriveId DriveId,
        UserId ActorId,
        DateTimeOffset OccurredAt) : IDomainEvent;
}
