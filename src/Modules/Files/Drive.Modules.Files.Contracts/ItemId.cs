using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.Contracts
{
    public readonly record struct ItemId : IStronglyTypedId<ItemId>
    {
        private ItemId(Guid value) => Value = value;

        public Guid Value { get; }

        public static ItemId New() => new(Guid.CreateVersion7());

        public static ItemId From(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("An item ID cannot be empty.", nameof(value));
            }

            return new ItemId(value);
        }

        public override string ToString() => Value.ToString();
    }
}
