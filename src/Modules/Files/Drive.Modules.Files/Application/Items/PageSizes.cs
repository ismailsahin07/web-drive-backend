namespace Drive.Modules.Files.Application.Items
{
    internal static class PageSizes
    {
        public const int Default = 50;

        public const int Max = 200;

        public static int Resolve(int? requested) =>
            requested is { } size ? Math.Clamp(size, 1, Max) : Default;
    }
}
