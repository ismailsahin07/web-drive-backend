using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.UnitTests.Domain.Items
{
    public sealed class ItemNameTests
    {
        [Theory]
        [InlineData("report.pdf")]
        [InlineData("Work Documents")]
        [InlineData("a")]
        [InlineData(".gitignore")]
        [InlineData("archive.tar.gz")]
        [InlineData("file (1).txt")]
        [InlineData("résumé.docx")]
        [InlineData("CONSOLE.txt")]
        public void Create_ValidName_Succeeds(string value)
        {
            var result = ItemName.Create(value);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(value);
        }

        [Fact]
        public void Create_TrimsSurroundingWhitespace()
        {
            var result = ItemName.Create("   report.pdf  ");

            result.Value.Value.ShouldBe("report.pdf");
        }

        [Fact]
        public void Create_NormalizesUnicode_SoVisuallyIdenticalNamesAreEqual()
        {
            var composed = ItemName.Create("résumé.pdf").Value;
            var decomposed = ItemName.Create("résumé.pdf").Value;

            decomposed.ShouldBe(composed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_MissingName_FailsWithEmpty(string? value)
        {
            var result = ItemName.Create(value);

            result.Error.ShouldBe(ItemNameErrors.Empty);
        }

        [Fact]
        public void Create_NameAtMaxLength_Succeeds()
        {
            var result = ItemName.Create(new string('a', ItemName.MaxLength));

            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void Create_NameOverMaxLength_FailsWithTooLong()
        {
            var result = ItemName.Create(new string('a', ItemName.MaxLength + 1));

            result.Error.ShouldBe(ItemNameErrors.TooLong);
        }

        [Theory]
        [InlineData("a/b")]
        [InlineData("a\\b")]
        [InlineData("a:b")]
        [InlineData("a*b")]
        [InlineData("a?b")]
        [InlineData("a\"b")]
        [InlineData("a<b")]
        [InlineData("a>b")]
        [InlineData("a|b")]
        [InlineData("a\tb")]
        [InlineData("a\u0000b")]
        public void Create_InvalidCharacter_FailsWithInvalidCharacters(string value)
        {
            var result = ItemName.Create(value);

            result.Error.ShouldBe(ItemNameErrors.InvalidCharacters);
        }

        [Fact]
        public void Create_BrokenUnicode_FailsWithInvalidCharacters()
        {
            var result = ItemName.Create("a\uD800b");

            result.Error.ShouldBe(ItemNameErrors.InvalidCharacters);
        }

        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("CON")]
        [InlineData("con.txt")]
        [InlineData("Lpt1")]
        [InlineData("NUL.tar.gz")]
        public void Create_ReservedName_FailsWithReserved(string value)
        {
            var result = ItemName.Create(value);

            result.Error.ShouldBe(ItemNameErrors.Reserved);
        }

        [Fact]
        public void Create_TrailingPeriod_FailsWithEndsWithPeriod()
        {
            var result = ItemName.Create("report.");

            result.Error.ShouldBe(ItemNameErrors.EndsWithPeriod);
        }

        [Fact]
        public void ComparisonKey_IgnoresCase()
        {
            var upper = ItemName.Create("Report.PDF").Value;
            var lower = ItemName.Create("report.pdf").Value;

            upper.ComparisonKey.ShouldBe(lower.ComparisonKey);
        }

        [Fact]
        public void Names_DifferingOnlyInCase_AreNotEqual()
        {
            var upper = ItemName.Create("Report.pdf").Value;
            var lower = ItemName.Create("report.pdf").Value;

            upper.ShouldNotBe(lower);
        }
    }
}
