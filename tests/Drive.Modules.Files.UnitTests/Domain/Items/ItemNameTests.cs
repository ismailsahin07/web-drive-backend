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

        [Fact]
        public void Rehydrate_KeepsStoredValueWithoutRevalidating()
        {
            var name = ItemName.Rehydrate("CON");

            name.Value.ShouldBe("CON");
        }

        [Theory]
        [InlineData("report.pdf", 1, "report (1).pdf")]
        [InlineData("Work", 2, "Work (2)")]
        [InlineData(".gitignore", 1, ".gitignore (1)")]
        [InlineData("archive.tar.gz", 3, "archive.tar (3).gz")]
        public void WithNumberSuffix_InsertsNumberBeforeExtension(string value, int number, string expected)
        {
            var name = ItemName.Create(value).Value;

            name.WithNumberSuffix(number).Value.ShouldBe(expected);
        }

        [Fact]
        public void WithNumberSuffix_OnMaxLengthName_TruncatesButKeepsExtension()
        {
            var name = ItemName.Create(new string('a', ItemName.MaxLength - 4) + ".pdf").Value;

            var numbered = name.WithNumberSuffix(1);

            numbered.Value.Length.ShouldBe(ItemName.MaxLength);
            numbered.Value.ShouldEndWith(" (1).pdf");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WithNumberSuffix_NonPositiveNumber_Throws(int number)
        {
            var name = ItemName.Create("report.pdf").Value;

            Should.Throw<ArgumentOutOfRangeException>(() => name.WithNumberSuffix(number));
        }
    }
}
