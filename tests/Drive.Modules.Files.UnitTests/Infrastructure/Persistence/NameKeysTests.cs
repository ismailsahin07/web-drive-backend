using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.Infrastructure.Persistence;
using Drive.Modules.Files.UnitTests.TestDoubles;

namespace Drive.Modules.Files.UnitTests.Infrastructure.Persistence
{
    public sealed class NameKeysTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public void For_AvailableItem_CombinesParentAndUppercasedName()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            NameKeys.For(folder).ShouldBe($"{_context.Root.Id}/WORK");
        }

        [Fact]
        public void For_PendingUpload_StillClaimsTheName()
        {
            var file = _context.SeedUpload(_context.Root, "photo.jpg", 1);

            NameKeys.For(file).ShouldBe($"{_context.Root.Id}/PHOTO.JPG");
        }

        [Fact]
        public void For_TrashedItem_IsNull()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");
            folder.Trash(_context.Owner, FilesTestContext.Now);

            NameKeys.For(folder).ShouldBeNull();
        }

        [Fact]
        public void For_Root_IsNull()
        {
            NameKeys.For(_context.Root).ShouldBeNull();
        }

        [Fact]
        public void ClaimId_IsDeterministicAndCaseInsensitive()
        {
            var parentId = ItemId.New();

            var lower = NameKeys.ClaimId(NameKeys.For(parentId, ItemName.Create("report.pdf").Value));
            var upper = NameKeys.ClaimId(NameKeys.For(parentId, ItemName.Create("REPORT.PDF").Value));

            lower.ShouldBe(upper);
            lower.ShouldStartWith("name-");
            lower.Length.ShouldBe("name-".Length + 64);
        }

        [Fact]
        public void ClaimId_DiffersPerFolder()
        {
            var name = ItemName.Create("report.pdf").Value;

            var first = NameKeys.ClaimId(NameKeys.For(ItemId.New(), name));
            var second = NameKeys.ClaimId(NameKeys.For(ItemId.New(), name));

            first.ShouldNotBe(second);
        }

        [Fact]
        public void SortKey_PutsFoldersBeforeFiles()
        {
            var folder = _context.SeedFolder(_context.Root, "zeta");
            var file = _context.SeedAvailableFile(_context.Root, "alpha.txt", 1);

            NameKeys.SortKey(folder).ShouldBe("0|ZETA");
            NameKeys.SortKey(file).ShouldBe("1|ALPHA.TXT");
            string.CompareOrdinal(NameKeys.SortKey(folder), NameKeys.SortKey(file)).ShouldBeLessThan(0);
        }
    }
}
