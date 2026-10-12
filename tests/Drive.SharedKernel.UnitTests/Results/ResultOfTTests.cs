using Drive.SharedKernel.Results;

namespace Drive.SharedKernel.UnitTests.Results
{
    public sealed class ResultOfTTests
    {
        private static readonly Error TestError = Error.NotFound("Test.NotFound", "Not found.");

        [Fact]
        public void Success_ExposesValue()
        {
            var result = Result.Success(42);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(42);
            result.Error.ShouldBeNull();
        }

        [Fact]
        public void Success_WithNullValue_Throws()
        {
            Should.Throw<ArgumentNullException>(() => Result.Success<string>(null!));
        }

        [Fact]
        public void Failure_ExposesError()
        {
            var result = Result.Failure<int>(TestError);

            result.IsFailure.ShouldBeTrue();
            result.Error.ShouldBe(TestError);
        }

        [Fact]
        public void Value_OnFailure_ThrowsWithErrorInMessage()
        {
            var result = Result.Failure<int>(TestError);

            var exception = Should.Throw<InvalidOperationException>(() => result.Value);

            exception.Message.ShouldContain("Test.NotFound");
        }

        [Fact]
        public void ImplicitConversion_FromValue_CreatesSuccess()
        {
            Result<string> result = "report.pdf";

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe("report.pdf");
        }

        [Fact]
        public void ImplicitConversion_FromError_CreatesFailure()
        {
            Result<string> result = TestError;

            result.IsFailure.ShouldBeTrue();
            result.Error.ShouldBe(TestError);
        }

        [Fact]
        public void Match_OnSuccess_PassesValueToSuccessBranch()
        {
            var outcome = Result.Success(21).Match(value => value * 2, _ => -1);

            outcome.ShouldBe(42);
        }

        [Fact]
        public void Match_OnFailure_RunsFailureBranch()
        {
            var outcome = Result.Failure<int>(TestError).Match(value => value * 2, _ => -1);

            outcome.ShouldBe(-1);
        }
    }
}
