namespace ArchiForge.Application.Jobs;

public sealed record BackgroundJobFile(
    string FileName,
    string ContentType,
    byte[] Bytes);
