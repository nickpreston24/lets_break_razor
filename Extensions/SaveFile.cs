// namespace CodeMechanic.FileSystem;

public record SaveFile(string text)
{
    public string file_name { set; get; } = string.Empty;
    public string root { get; set; } = string.Empty;
    public string[] subfolders { set; get; } = Array.Empty<string>();
}
