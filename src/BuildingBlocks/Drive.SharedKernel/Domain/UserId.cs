namespace Drive.SharedKernel.Domain
{
    public readonly record struct UserId : IStronglyTypedId<UserId>
    {
        private UserId(Guid value) => Value = value;

        public Guid Value { get; }

        public static UserId From(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("A user ID cannot be empty.", nameof(value));
            }

            return new UserId(value);
        }

        public override string ToString() => Value.ToString();
    }
}
