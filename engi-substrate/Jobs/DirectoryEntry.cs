namespace Engi.Substrate.Jobs;

public class DirectoryEntry
{
    public string Path { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Extension { get; set; } = null;
    public List<DirectoryEntry> Children { get; set; } = null!;

    public static string FileType(string path)
    {
        if (System.IO.Path.HasExtension(path))
        {
            return "file";
        }
        else
        {
            return "directory";
        }
    }

    public static string FileExtension(string path)
    {
        if (System.IO.Path.HasExtension(path))
        {
            return System.IO.Path.GetExtension(path);
        }
        else
        {
            return null;
        }
    }

    public static List<DirectoryEntry> DirectoryEntries(string[] files)
    {
        Dictionary<string, DirectoryEntry> entries = new Dictionary<string, DirectoryEntry>();
        List<DirectoryEntry> result = new List<DirectoryEntry>();

        foreach (var fullPath in files)
        {
            var splitted = fullPath.Split("/")[1..];
            var path = splitted[0];

            if (!entries.ContainsKey(path))
            {
                var val = new DirectoryEntry {
                    Path = path,
                    Name = path,
                    Type = FileType(path),
                    Children = new List<DirectoryEntry>(),
                };

                entries.Add(path, val);
                result.Add(val);
            }

            foreach (var component in splitted[1..])
            {
                var parent = entries[path];
                path = path + "/" + component;

                if (entries.ContainsKey(path))
                {
                    continue;
                }

                var child = new DirectoryEntry {
                    Path = path,
                    Name = component,
                    Type = FileType(component),
                    Extension = FileExtension(component),
                    Children = new List<DirectoryEntry>(),
                };

                entries.Add(path, child);
                parent.Children.Add(child);
            }
        }

        return result;
    }
}
