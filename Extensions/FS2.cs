using CodeMechanic.Diagnostics;
using CodeMechanic.Types;

// namespace CodeMechanic.FileSystem;

public static class FS2
{
    public static SaveFile Save(this string text) => new SaveFile(text);

    public static SaveFile To(this SaveFile saveFile, string root, params string[] subfolders)
    {
        // root.Dump("root");
        // if (saveFile.root.IsEmpty()) throw new NullReferenceException(nameof(saveFile.root));
        saveFile.root = root;
        // if (!subfolders.IsNullOrEmpty()) 
        saveFile.subfolders = subfolders;
        return saveFile;
    }

    public static FileInfo As(this SaveFile save_file, string filename, bool debug = false)
    {
        if (filename.IsNullOrEmpty()) throw new ArgumentNullException(nameof(filename));
        if (!Path.HasExtension(filename)) throw new ArgumentException($"{nameof(filename)} must have an extension!");
        save_file.file_name = filename;
        return SaveAsInternal(save_file, debug: true);
    }

    private static FileInfo SaveAsInternal(this SaveFile saveFile, bool debug = false)
    {
        if (saveFile == null) throw new NullReferenceException(nameof(saveFile));
        if (saveFile.root.IsEmpty()) throw new NullReferenceException(nameof(saveFile.root));
        if (saveFile.text.IsEmpty()) throw new NullReferenceException(nameof(saveFile.text));

        string save_folder = saveFile.root;

        // Combine subfolders:
        if (debug)
            saveFile.subfolders.Dump("subfolders");

        foreach (var subfolder in saveFile.subfolders ?? Array.Empty<string>())
        {
            save_folder = Path.Combine(save_folder, subfolder);
        }

        if (debug)
            Console.WriteLine("save folder: \n" + save_folder);

        // Create save path:
        string save_path = Path.Combine(save_folder, saveFile.file_name);

        if (debug)
            Console.WriteLine("save path: \n" + save_path);

        if (!Directory.Exists(save_folder)) Directory.CreateDirectory(save_folder);

        // Write to full save path:
        if (!saveFile.text.IsNullOrEmpty())
            File.WriteAllText(save_path, saveFile.text);

        // return meta about the file:
        return File.Exists(save_path)
            ? new FileInfo(save_path)
            : throw new Exception($"file saved to '{save_path}' could not be found");
    }
}