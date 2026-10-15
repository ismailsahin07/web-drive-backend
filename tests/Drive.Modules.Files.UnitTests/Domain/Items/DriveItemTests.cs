using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.UnitTests.Domain.Items
{
    public sealed class DriveItemTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 15, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset Later = Now.AddHours(1);
        private static readonly UserId Owner = UserId.From(Guid.NewGuid());
        private static readonly UserId Editor = UserId.From(Guid.NewGuid());

        // ---------- Root folder ----------

        [Fact]
        public void CreateRootFolder_CreatesAvailableFolderWithoutParent()
        {
            var driveId = DriveId.New();

            var root = DriveItem.CreateRootFolder(ItemId.New(), driveId, Owner, Now);

            root.DriveId.ShouldBe(driveId);
            root.ParentId.ShouldBeNull();
            root.IsRoot.ShouldBeTrue();
            root.IsFolder.ShouldBeTrue();
            root.IsAvailable.ShouldBeTrue();
            root.Name.Value.ShouldBe("My Drive");
            root.DomainEvents.ShouldBeEmpty();
        }

        // ---------- CreateFolder ----------

        [Fact]
        public void CreateFolder_InAvailableFolder_CreatesFolderAndRaisesEvent()
        {
            var root = CreateRoot();
            var id = ItemId.New();

            var result = DriveItem.CreateFolder(id, root, Name("Work"), Owner, Now);

            var folder = result.Value;
            folder.Id.ShouldBe(id);
            folder.DriveId.ShouldBe(root.DriveId);
            folder.ParentId.ShouldBe(root.Id);
            folder.Kind.ShouldBe(ItemKind.Folder);
            folder.Status.ShouldBe(ItemStatus.Available);
            folder.Name.Value.ShouldBe("Work");
            folder.CreatedBy.ShouldBe(Owner);
            folder.CreatedAt.ShouldBe(Now);
            folder.ModifiedBy.ShouldBe(Owner);
            folder.ModifiedAt.ShouldBe(Now);

            var domainEvent = folder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<FolderCreatedDomainEvent>();
            domainEvent.ParentId.ShouldBe(root.Id);
        }

        [Fact]
        public void CreateFolder_InsideFile_FailsWithDestinationNotFolder()
        {
            var file = CreateAvailableFile(CreateRoot());

            var result = DriveItem.CreateFolder(ItemId.New(), file, Name("Work"), Owner, Now);

            result.Error.ShouldBe(DriveItemErrors.DestinationNotFolder);
        }

        [Fact]
        public void CreateFolder_InsideTrashedFolder_FailsWithDestinationNotAvailable()
        {
            var parent = CreateFolder(CreateRoot(), "Old");
            parent.Trash(Owner, Now);

            var result = DriveItem.CreateFolder(ItemId.New(), parent, Name("Work"), Owner, Now);

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        // ---------- CreatePendingFile ----------

        [Fact]
        public void CreatePendingFile_CreatesFileWaitingForUpload()
        {
            var root = CreateRoot();

            var result = DriveItem.CreatePendingFile(
                ItemId.New(), root, Name("report.pdf"), 1024, "application/pdf", "blob/name", Owner, Now);

            var file = result.Value;
            file.Kind.ShouldBe(ItemKind.File);
            file.Status.ShouldBe(ItemStatus.PendingUpload);
            file.IsAvailable.ShouldBeFalse();
            file.SizeBytes.ShouldBe(1024);
            file.ContentType.ShouldBe("application/pdf");
            file.BlobName.ShouldBe("blob/name");
            file.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePendingFile_InsideFile_FailsWithDestinationNotFolder()
        {
            var file = CreateAvailableFile(CreateRoot());

            var result = DriveItem.CreatePendingFile(
                ItemId.New(), file, Name("a.txt"), 1, "text/plain", "blob", Owner, Now);

            result.Error.ShouldBe(DriveItemErrors.DestinationNotFolder);
        }

        [Fact]
        public void CreatePendingFile_NegativeSize_Throws()
        {
            var root = CreateRoot();

            Should.Throw<ArgumentOutOfRangeException>(() => DriveItem.CreatePendingFile(
                ItemId.New(), root, Name("a.txt"), -1, "text/plain", "blob", Owner, Now));
        }

        [Fact]
        public void CreatePendingFile_EmptyContentType_Throws()
        {
            var root = CreateRoot();

            Should.Throw<ArgumentException>(() => DriveItem.CreatePendingFile(
                ItemId.New(), root, Name("a.txt"), 1, " ", "blob", Owner, Now));
        }

        // ---------- MarkUploaded ----------

        [Fact]
        public void MarkUploaded_WithDeclaredSize_MakesFileAvailableAndRaisesEvent()
        {
            var file = CreatePendingFile(CreateRoot(), sizeBytes: 500);

            var result = file.MarkUploaded(500, Later);

            result.IsSuccess.ShouldBeTrue();
            file.Status.ShouldBe(ItemStatus.Available);
            file.ModifiedAt.ShouldBe(Later);
            var domainEvent = file.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<FileUploadedDomainEvent>();
            domainEvent.SizeBytes.ShouldBe(500);
        }

        [Fact]
        public void MarkUploaded_WithDifferentSize_FailsAndStaysPending()
        {
            var file = CreatePendingFile(CreateRoot(), sizeBytes: 500);

            var result = file.MarkUploaded(499, Later);

            result.Error.ShouldBe(DriveItemErrors.UploadSizeMismatch);
            file.Status.ShouldBe(ItemStatus.PendingUpload);
            file.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void MarkUploaded_CalledTwice_IsIdempotent()
        {
            var file = CreatePendingFile(CreateRoot(), sizeBytes: 500);
            file.MarkUploaded(500, Later);

            var second = file.MarkUploaded(500, Later.AddMinutes(1));

            second.IsSuccess.ShouldBeTrue();
            file.ModifiedAt.ShouldBe(Later);
            file.DomainEvents.Count.ShouldBe(1);
        }

        [Fact]
        public void MarkUploaded_OnFolder_Throws()
        {
            var folder = CreateFolder(CreateRoot(), "Work");

            Should.Throw<InvalidOperationException>(() => folder.MarkUploaded(0, Later));
        }

        // ---------- Rename ----------

        [Fact]
        public void Rename_ChangesNameAndRaisesEvent()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.PullDomainEvents();

            var result = folder.Rename(Name("Projects"), Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.Name.Value.ShouldBe("Projects");
            folder.ModifiedBy.ShouldBe(Editor);
            folder.ModifiedAt.ShouldBe(Later);
            var domainEvent = folder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ItemRenamedDomainEvent>();
            domainEvent.OldName.Value.ShouldBe("Work");
            domainEvent.NewName.Value.ShouldBe("Projects");
            domainEvent.ActorId.ShouldBe(Editor);
        }

        [Fact]
        public void Rename_ToSameName_DoesNothing()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.PullDomainEvents();

            var result = folder.Rename(Name("Work"), Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.ModifiedAt.ShouldBe(Now);
            folder.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void Rename_CaseOnlyChange_IsARealRename()
        {
            var folder = CreateFolder(CreateRoot(), "work");
            folder.PullDomainEvents();

            folder.Rename(Name("Work"), Editor, Later);

            folder.Name.Value.ShouldBe("Work");
            folder.DomainEvents.ShouldHaveSingleItem();
        }

        [Fact]
        public void Rename_Root_FailsWithRootCannotBeModified()
        {
            var root = CreateRoot();

            var result = root.Rename(Name("Other"), Owner, Later);

            result.Error.ShouldBe(DriveItemErrors.RootCannotBeModified);
        }

        [Fact]
        public void Rename_TrashedItem_FailsWithNotAvailable()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.Trash(Owner, Now);

            var result = folder.Rename(Name("Projects"), Owner, Later);

            result.Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        // ---------- MoveTo ----------

        [Fact]
        public void MoveTo_AnotherFolder_ChangesParentAndRaisesEvent()
        {
            var root = CreateRoot();
            var source = CreateFolder(root, "Source");
            var destination = CreateFolder(root, "Destination");
            source.PullDomainEvents();

            var result = source.MoveTo(destination, [root.Id], Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            source.ParentId.ShouldBe(destination.Id);
            var domainEvent = source.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ItemMovedDomainEvent>();
            domainEvent.FromParentId.ShouldBe(root.Id);
            domainEvent.ToParentId.ShouldBe(destination.Id);
        }

        [Fact]
        public void MoveTo_CurrentParent_DoesNothing()
        {
            var root = CreateRoot();
            var folder = CreateFolder(root, "Work");
            folder.PullDomainEvents();

            var result = folder.MoveTo(root, [], Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.ParentId.ShouldBe(root.Id);
            folder.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void MoveTo_Itself_FailsWithCannotMoveIntoItself()
        {
            var root = CreateRoot();
            var folder = CreateFolder(root, "Work");

            var result = folder.MoveTo(folder, [root.Id], Editor, Later);

            result.Error.ShouldBe(DriveItemErrors.CannotMoveIntoItself);
        }

        [Fact]
        public void MoveTo_OwnSubfolder_FailsWithCannotMoveIntoItself()
        {
            var root = CreateRoot();
            var parent = CreateFolder(root, "Parent");
            var child = CreateFolder(parent, "Child");

            var result = parent.MoveTo(child, [parent.Id, root.Id], Editor, Later);

            result.Error.ShouldBe(DriveItemErrors.CannotMoveIntoItself);
            parent.ParentId.ShouldBe(root.Id);
        }

        [Fact]
        public void MoveTo_File_FailsWithDestinationNotFolder()
        {
            var root = CreateRoot();
            var folder = CreateFolder(root, "Work");
            var file = CreateAvailableFile(root);

            var result = folder.MoveTo(file, [root.Id], Editor, Later);

            result.Error.ShouldBe(DriveItemErrors.DestinationNotFolder);
        }

        [Fact]
        public void MoveTo_TrashedFolder_FailsWithDestinationNotAvailable()
        {
            var root = CreateRoot();
            var folder = CreateFolder(root, "Work");
            var trashed = CreateFolder(root, "Old");
            trashed.Trash(Owner, Now);

            var result = folder.MoveTo(trashed, [root.Id], Editor, Later);

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        [Fact]
        public void MoveTo_FolderInAnotherDrive_FailsWithDifferentDrive()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            var otherRoot = CreateRoot();

            var result = folder.MoveTo(otherRoot, [], Editor, Later);

            result.Error.ShouldBe(DriveItemErrors.DifferentDrive);
        }

        [Fact]
        public void MoveTo_Root_FailsWithRootCannotBeModified()
        {
            var root = CreateRoot();
            var folder = CreateFolder(root, "Work");

            var result = root.MoveTo(folder, [root.Id], Owner, Later);

            result.Error.ShouldBe(DriveItemErrors.RootCannotBeModified);
        }

        // ---------- Trash / Restore ----------

        [Fact]
        public void Trash_AvailableItem_TrashesAndRaisesEvent()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.PullDomainEvents();

            var result = folder.Trash(Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.Status.ShouldBe(ItemStatus.Trashed);
            folder.TrashedAt.ShouldBe(Later);
            folder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ItemTrashedDomainEvent>();
        }

        [Fact]
        public void Trash_AlreadyTrashedItem_IsIdempotent()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.Trash(Editor, Now);
            folder.PullDomainEvents();

            var result = folder.Trash(Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.TrashedAt.ShouldBe(Now);
            folder.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void Trash_Root_FailsWithRootCannotBeModified()
        {
            var root = CreateRoot();

            root.Trash(Owner, Later).Error.ShouldBe(DriveItemErrors.RootCannotBeModified);
        }

        [Fact]
        public void Trash_PendingUpload_FailsWithNotAvailable()
        {
            var file = CreatePendingFile(CreateRoot(), sizeBytes: 10);

            file.Trash(Owner, Later).Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        [Fact]
        public void Restore_TrashedItem_MakesItAvailableAndRaisesEvent()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.Trash(Owner, Now);
            folder.PullDomainEvents();

            var result = folder.Restore(Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.Status.ShouldBe(ItemStatus.Available);
            folder.TrashedAt.ShouldBeNull();
            folder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ItemRestoredDomainEvent>();
        }

        [Fact]
        public void Restore_AvailableItem_IsIdempotent()
        {
            var folder = CreateFolder(CreateRoot(), "Work");
            folder.PullDomainEvents();

            var result = folder.Restore(Editor, Later);

            result.IsSuccess.ShouldBeTrue();
            folder.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void Restore_PendingUpload_FailsWithNotAvailable()
        {
            var file = CreatePendingFile(CreateRoot(), sizeBytes: 10);

            file.Restore(Owner, Later).Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        // ---------- Rehydrate ----------

        [Fact]
        public void Rehydrate_RestoresStateWithoutRaisingEvents()
        {
            var id = ItemId.New();
            var driveId = DriveId.New();
            var parentId = ItemId.New();

            var item = DriveItem.Rehydrate(
                id, driveId, parentId, ItemKind.File, Name("a.txt"), ItemStatus.Trashed,
                42, "text/plain", "blob", Owner, Now, Editor, Later, trashedAt: Later);

            item.Id.ShouldBe(id);
            item.DriveId.ShouldBe(driveId);
            item.ParentId.ShouldBe(parentId);
            item.Status.ShouldBe(ItemStatus.Trashed);
            item.ModifiedBy.ShouldBe(Editor);
            item.TrashedAt.ShouldBe(Later);
            item.DomainEvents.ShouldBeEmpty();
        }

        // ---------- Helpers ----------

        private static ItemName Name(string value) => ItemName.Create(value).Value;

        private static DriveItem CreateRoot() =>
            DriveItem.CreateRootFolder(ItemId.New(), DriveId.New(), Owner, Now);

        private static DriveItem CreateFolder(DriveItem parent, string name) =>
            DriveItem.CreateFolder(ItemId.New(), parent, Name(name), Owner, Now).Value;

        private static DriveItem CreatePendingFile(DriveItem parent, long sizeBytes) =>
            DriveItem.CreatePendingFile(
                ItemId.New(), parent, Name("file.bin"), sizeBytes, "application/octet-stream", "blob", Owner, Now).Value;

        private static DriveItem CreateAvailableFile(DriveItem parent)
        {
            var file = CreatePendingFile(parent, sizeBytes: 10);
            file.MarkUploaded(10, Now);
            return file;
        }
    }
}
