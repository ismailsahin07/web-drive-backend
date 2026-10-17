using Drive.Modules.Files.UnitTests.TestDoubles;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class ItemTreeTests
    {
        [Fact]
        public async Task GetAncestorsAsync_ForRoot_ReturnsEmpty()
        {
            var context = new FilesTestContext();

            var ancestors = await context.ItemTree.GetAncestorsAsync(context.Root, TestContext.Current.CancellationToken);

            ancestors.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetAncestorsAsync_ForNestedItem_ReturnsNearestParentFirst()
        {
            var context = new FilesTestContext();
            var work = context.SeedFolder(context.Root, "Work");
            var reports = context.SeedFolder(work, "Reports");

            var ancestors = await context.ItemTree.GetAncestorsAsync(reports, TestContext.Current.CancellationToken);

            ancestors.Select(a => a.Id).ShouldBe(new[] { work.Id, context.Root.Id });
        }

        [Fact]
        public async Task HasTrashedAncestorAsync_WhenAnAncestorIsTrashed_ReturnsTrue()
        {
            var context = new FilesTestContext();
            var old = context.SeedFolder(context.Root, "Old");
            var sub = context.SeedFolder(old, "Sub");
            old.Trash(context.Owner, FilesTestContext.Now);

            var result = await context.ItemTree.HasTrashedAncestorAsync(sub, TestContext.Current.CancellationToken);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HasTrashedAncestorAsync_WhenNoAncestorIsTrashed_ReturnsFalse()
        {
            var context = new FilesTestContext();
            var work = context.SeedFolder(context.Root, "Work");

            var result = await context.ItemTree.HasTrashedAncestorAsync(work, TestContext.Current.CancellationToken);

            result.ShouldBeFalse();
        }
    }
}
