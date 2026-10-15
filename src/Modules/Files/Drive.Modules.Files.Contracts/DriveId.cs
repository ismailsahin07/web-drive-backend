using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.Contracts
{
    public readonly record struct DriveId : IStronglyTypedId<DriveId>
    {
        private DriveId(Guid value) => Value = value;

        public Guid Value { get; }

        public static DriveId New() => new(Guid.CreateVersion7());

        public static DriveId From(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("A drive ID cannot be empty.", nameof(value));
            }

            return new DriveId(value);
        }

        public override string ToString() => Value.ToString();
    }
}
