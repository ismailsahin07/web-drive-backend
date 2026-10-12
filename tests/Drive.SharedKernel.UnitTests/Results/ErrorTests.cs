using Drive.SharedKernel.Results;

namespace Drive.SharedKernel.UnitTests.Results
{
    public sealed class ErrorTests
    {
        [Fact]
        public void Factories_SetMatchingErrorType()
        {
            Error.Failure("C", "D").Type.ShouldBe(ErrorType.Failure);
            Error.Validation("C", "D").Type.ShouldBe(ErrorType.Validation);
            Error.NotFound("C", "D").Type.ShouldBe(ErrorType.NotFound);
            Error.Conflict("C", "D").Type.ShouldBe(ErrorType.Conflict);
            Error.Forbidden("C", "D").Type.ShouldBe(ErrorType.Forbidden);
        }

        [Fact]
        public void Factory_SetsCodeAndDescription()
        {
            var error = Error.NotFound("Files.ItemNotFound", "The item was not found.");

            error.Code.ShouldBe("Files.ItemNotFound");
            error.Description.ShouldBe("The item was not found.");
        }

        [Fact]
        public void Errors_WithSameValues_AreEqual()
        {
            var first = Error.Conflict("Files.NameTaken", "Name taken.");
            var second = Error.Conflict("Files.NameTaken", "Name taken.");

            first.ShouldBe(second);
        }

        [Fact]
        public void Errors_WithDifferentType_AreNotEqual()
        {
            var conflict = Error.Conflict("Same.Code", "Same text.");
            var notFound = Error.NotFound("Same.Code", "Same text.");

            conflict.ShouldNotBe(notFound);
        }

        [Fact]
        public void ToString_ReturnsCodeAndDescription()
        {
            var error = Error.Conflict("Files.NameTaken", "Name taken.");

            error.ToString().ShouldBe("Files.NameTaken: Name taken.");
        }

        [Fact]
        public void ValidationError_HasValidationTypeAndFieldErrors()
        {
            var fieldErrors = new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required."],
            };

            var error = new ValidationError(fieldErrors);

            error.Type.ShouldBe(ErrorType.Validation);
            error.Code.ShouldBe("General.Validation");
            error.Errors["name"].ShouldBe(new[] { "Name is required." });
        }
    }
}
