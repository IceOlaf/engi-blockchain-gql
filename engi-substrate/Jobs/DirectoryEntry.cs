namespace Engi.Substrate.Jobs;

public class DirectoryEntry
{
    public string path { get; set; } = null!;
    public string name { get; set; } = null!;
    public string type { get; set; } = null!;
    public string? extension { get; set; } = null;
    public List<DirectoryEntry> children { get; set; } = null!;

    public static string FileType(string path)
    {
        if (Path.HasExtension(path))
        {
            return "file";
        }
        else
        {
            return "directory";
        }
    }

    public static string? FileExtension(string path)
    {
        return Path.HasExtension(path) ? Path.GetExtension(path) : null;
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
                    path = path,
                    name = path,
                    type = FileType(path),
                    children = new List<DirectoryEntry>(),
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
                    path = path,
                    name = component,
                    type = FileType(component),
                    extension = FileExtension(component),
                    children = new List<DirectoryEntry>(),
                };

                entries.Add(path, child);
                parent.children.Add(child);
            }
        }

        return result;
    }
}
