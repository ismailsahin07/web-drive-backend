using Drive.SharedKernel.Results;

namespace Drive.SharedKernel.UnitTests.Results
{
    public sealed class ResultTests
    {
        private static readonly Error TestError = Error.Failure("Test.Failed", "Something failed.");

        [Fact]
        public void Success_IsSuccessful_AndHasNoError()
        {
            var result = Result.Success();

            result.IsSuccess.ShouldBeTrue();
            result.IsFailure.ShouldBeFalse();
            result.Error.ShouldBeNull();
        }

        [Fact]
        public void Success_ReturnsTheSameSharedInstance()
        {
            Result.Success().ShouldBeSameAs(Result.Success());
        }

        [Fact]
        public void Failure_IsFailure_AndExposesError()
        {
            var result = Result.Failure(TestError);

            result.IsFailure.ShouldBeTrue();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe(TestError);
        }

        [Fact]
        public void Failure_WithNullError_Throws()
        {
            Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
        }

        [Fact]
        public void ImplicitConversion_FromError_CreatesFailure()
        {
            Result result = TestError;

            result.IsFailure.ShouldBeTrue();
            result.Error.ShouldBe(TestError);
        }

        [Fact]
        public void Match_OnSuccess_RunsSuccessBranch()
        {
            var outcome = Result.Success().Match(() => "success", error => error.Code);

            outcome.ShouldBe("success");
        }

        [Fact]
        public void Match_OnFailure_RunsFailureBranchWithError()
        {
            var outcome = Result.Failure(TestError).Match(() => "success", error => error.Code);

            outcome.ShouldBe("Test.Failed");
        }
    }
}
