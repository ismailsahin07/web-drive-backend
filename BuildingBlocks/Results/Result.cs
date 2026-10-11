using System.Diagnostics.CodeAnalysis;

namespace Drive.SharedKernel.Results
{
    public class Result
    {
        private static readonly Result SuccessResult = new(null);
        protected Result(Error? error) => Error = error;
        public Error? Error { get; }

        [MemberNotNullWhen(false, nameof(Error))]
        public bool IsSuccess => Error is null;

        [MemberNotNullWhen(true, nameof(Error))]
        public bool IsFailure => Error is not null;

        public static Result Success() => SuccessResult;
        public static Result Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new Result(error);
        }
        public static Result<TValue> Success<TValue>(TValue value)
        {
            if(value is null)
            {
                throw new ArgumentNullException(nameof(value), "A successfull result must have a value. Return an error instead of null.");
            }

            return new Result<TValue>(value, error: null);
        }

        public static Result<TValue> Failure<TValue>(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new Result<TValue>(default, error);
        }

        public static implicit operator Result(Error error) => Failure(error);

        public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
            IsSuccess ? onSuccess() : onFailure(Error);
    }
}
