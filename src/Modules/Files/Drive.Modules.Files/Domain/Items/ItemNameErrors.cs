using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Items
{
    internal static class ItemNameErrors
    {
        public static readonly Error Empty = Error.Validation(
            "Files.ItemName.Empty",
            "A name is required.");

        public static readonly Error TooLong = Error.Validation(
            "Files.ItemName.TooLong",
            $"A name cannot be longer than {ItemName.MaxLength} characters.");

        public static readonly Error InvalidCharacters = Error.Validation(
            "Files.ItemName.InvalidCharacters",
            "A name cannot contain control characters or any of these characters: / \\ : * ? \" < > |");

        public static readonly Error Reserved = Error.Validation(
            "Files.ItemName.Reserved",
            "This name is reserved by the operating system and cannot be used.");

        public static readonly Error EndsWithPeriod = Error.Validation(
            "Files.ItemName.EndsWithPeriod",
            "A name cannot end with a period.");
    }
}
