namespace Drive.SharedKernel.Results
{
    public sealed class Result<TValue> : Result
    {
        private readonly TValue? _value;

        internal Result(TValue? value, Error? error)
            : base(error) => _value = value;

        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access the value of a failed result. Error: {Error}");

        public static implicit operator Result<TValue>(TValue value) => Success(value);

        public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

        public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure) =>
            IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
