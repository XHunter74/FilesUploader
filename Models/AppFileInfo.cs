namespace xhunter74.FilesUploader.Models;

public record AppFileInfo
{
    public string Name { get; init; }
    public string Folder { get; init; }
    public DateTimeOffset Created { get; init; }
}
