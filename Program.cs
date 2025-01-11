using System.Text.Json;
using System.Text.RegularExpressions;

namespace Scans;

public class Scan
{
    public string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Version { get; set; }
    public string Path { get; set; }
    public string[] Textures { get; set; } = Array.Empty<string>();
}

class Program
{
    private const string GithubURL = "https://raw.githubusercontent.com/dfgHiatus/Scans/refs/heads/main/";
    private const string RootDirectory = @"F:\Scans\";
    private static readonly string ModelDirectory = Path.Combine(RootDirectory, "Models");
    private const string Index = "Index.json";

    static void Main(string[] args)
    {
        string outputFilePath = Path.Combine(RootDirectory, "index.json");
        List<Scan> existingScans = LoadExistingScans(outputFilePath);
        List<Scan> updatedScans = new List<Scan>();

        ProcessDirectory(ModelDirectory, updatedScans, existingScans);

        string jsonOutput = JsonSerializer.Serialize(updatedScans, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFilePath, jsonOutput);

        Console.WriteLine($"Scan catalogue written to {outputFilePath}");
    }

    static List<Scan> LoadExistingScans(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<Scan>>(jsonContent) ?? new List<Scan>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load existing index file: {ex.Message}");
            }
        }

        return new List<Scan>();
    }

    static void ProcessDirectory(string directory, List<Scan> updatedScans, List<Scan> existingScans)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            ProcessDirectory(subDirectory, updatedScans, existingScans);
        }

        foreach (var file in Directory.GetFiles(directory))
        {
            if (Path.GetExtension(file).Equals(".obj", StringComparison.OrdinalIgnoreCase))
            {
                Scan scan = ProcessScan(directory, file, existingScans);
                if (scan != null)
                {
                    updatedScans.Add(scan);
                }
            }
        }
    }

    static Scan ProcessScan(string directory, string objFilePath, List<Scan> existingScans)
    {
        string folderName = Path.GetFileName(directory);
        string fileName = Path.GetFileNameWithoutExtension(objFilePath);
        string date = ExtractDate(folderName);

        // Determine version and collect textures if applicable
        string version = "1";
        string[] textures = Array.Empty<string>();
        if (HasVersion2Textures(directory, out var foundTextures))
        {
            version = "2";
            textures = foundTextures;
        }

        var existingScan = existingScans.FirstOrDefault(s => s.Path == objFilePath);
        var relativePath = Path.GetRelativePath(RootDirectory, objFilePath).Replace("\\", "/");
        var path = $"{GithubURL}{relativePath}";

        if (existingScan != null)
        {
            existingScan.Name = folderName;
            existingScan.Date = date;
            existingScan.Version = version;
            existingScan.Textures = textures;
            existingScan.Path = path;
            return existingScan;
        }

        return new Scan
        {
            Name = folderName,
            Date = date,
            Version = version,
            Path = path,
            Textures = textures
        };
    }

    static string ExtractDate(string input)
    {
        // Matches YYYYMMDD_HHMMSS
        var match = Regex.Match(input, @"\b\d{8}_\d{6}\b");
        return match.Success ? match.Value : string.Empty;
    }

    static bool HasVersion2Textures(string directory, out string[] textures)
    {
        var textureFiles = Directory.GetFiles(directory, "*.jpg")
            .Concat(Directory.GetFiles(directory, "*.png"))
            .ToArray();

        textures = textureFiles.ToArray();
        return textures.Length == 4;
    }
}
