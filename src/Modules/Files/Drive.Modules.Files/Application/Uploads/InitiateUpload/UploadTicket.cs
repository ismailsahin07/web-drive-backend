namespace Drive.Modules.Files.Application.Uploads.InitiateUpload
{
    public sealed record UploadTicket(Guid ItemId, Uri UploadUrl, DateTimeOffset ExpiresAt);
}
