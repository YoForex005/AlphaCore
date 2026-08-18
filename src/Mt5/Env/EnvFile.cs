namespace TraderIntelligence.Mt5.Env;

public static class EnvFile
{
    public static string? FindAndLoad()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(cwd, ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", ".env")),
            @"D:\Prop\.env"
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            return null;
        Load(path);
        return path;
    }

    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
                continue;
            var i = line.IndexOf('=');
            var key = line[..i].Trim();
            var value = line[(i + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
