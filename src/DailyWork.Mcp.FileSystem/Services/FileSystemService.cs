using DailyWork.Mcp.FileSystem.Configuration;
using DailyWork.Mcp.FileSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.FileSystem.Services;

public class FileSystemService(
    FileSystemDbContext db,
    IOptions<FileSystemOptions> options,
    ILogger<FileSystemService> logger)
{
    private readonly FileSystemOptions config = options.Value;

    public async Task<PathValidationResult> ValidateAndResolvePathAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Validating path '{Path}'", path);

        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Invalid path '{Path}': {Error}", path, ex.Message);
            return PathValidationResult.Denied($"Invalid path: {ex.Message}", requestedPath: path);
        }

        if (IsWithinAllowedConfigDirectories(resolvedPath))
        {
            return PathValidationResult.Allowed(resolvedPath);
        }

        List<string> dbDirectories = await db.AllowedDirectories
            .Select(d => d.Path)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (IsWithinDirectories(resolvedPath, dbDirectories))
        {
            return PathValidationResult.Allowed(resolvedPath);
        }

        string containingDirectory = File.Exists(resolvedPath)
            ? Path.GetDirectoryName(resolvedPath) ?? resolvedPath
            : resolvedPath;

        logger.LogWarning("Access denied for path '{ResolvedPath}'", resolvedPath);
        return PathValidationResult.Denied(
            $"Access denied. The path '{resolvedPath}' is not within any allowed directory.",
            requiresAccess: containingDirectory);
    }

    public async Task<object> ReadFileAsync(
        string resolvedPath,
        int? maxLines = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading file '{FilePath}'", resolvedPath);

        FileInfo fileInfo = new(resolvedPath);

        if (!fileInfo.Exists)
        {
            logger.LogWarning("File not found: '{FilePath}'", resolvedPath);
            return new { Error = $"File not found: {resolvedPath}" };
        }

        if (fileInfo.Length > config.MaxFileSizeBytes)
        {
            return new
            {
                Error = $"File exceeds maximum size limit of {config.MaxFileSizeBytes:N0} bytes.",
                FileSize = fileInfo.Length,
                FilePath = resolvedPath,
                Suggestion = "Use the maxLines parameter to read a portion of the file."
            };
        }

        if (IsBinaryFile(resolvedPath))
        {
            return new
            {
                Error = "File appears to be a binary file and cannot be read as text.",
                FilePath = resolvedPath,
                Extension = fileInfo.Extension
            };
        }

        if (maxLines.HasValue)
        {
            List<string> lines = [];
            int count = 0;
            await foreach (string line in File.ReadLinesAsync(resolvedPath, cancellationToken).ConfigureAwait(false))
            {
                lines.Add(line);
                if (++count >= maxLines.Value)
                {
                    break;
                }
            }

            return new
            {
                FilePath = resolvedPath,
                Content = string.Join(Environment.NewLine, lines),
                LinesRead = lines.Count,
                Truncated = count >= maxLines.Value,
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension
            };
        }

        string content = await File.ReadAllTextAsync(resolvedPath, cancellationToken).ConfigureAwait(false);

        return new
        {
            FilePath = resolvedPath,
            Content = content,
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension,
            SizeBytes = fileInfo.Length
        };
    }

    public Task<object> ListDirectoryAsync(
        string resolvedPath,
        string? searchPattern = null,
        bool recursive = false)
    {
        logger.LogInformation("Listing directory '{DirectoryPath}' with pattern '{SearchPattern}'", resolvedPath, searchPattern);

        if (!Directory.Exists(resolvedPath))
        {
            logger.LogWarning("Directory not found: '{DirectoryPath}'", resolvedPath);
            return Task.FromResult<object>(new { Error = $"Directory not found: {resolvedPath}" });
        }

        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string pattern = searchPattern ?? "*";

        List<object> entries = [];

        foreach (string dir in Directory.EnumerateDirectories(resolvedPath, pattern, searchOption))
        {
            DirectoryInfo dirInfo = new(dir);
            entries.Add(new
            {
                Name = dirInfo.Name,
                Path = dirInfo.FullName,
                Type = "directory",
                LastModified = dirInfo.LastWriteTimeUtc
            });
        }

        foreach (string file in Directory.EnumerateFiles(resolvedPath, pattern, searchOption))
        {
            FileInfo fileInfo = new(file);
            entries.Add(new
            {
                Name = fileInfo.Name,
                Path = fileInfo.FullName,
                Type = "file",
                SizeBytes = fileInfo.Length,
                Extension = fileInfo.Extension,
                LastModified = fileInfo.LastWriteTimeUtc
            });
        }

        return Task.FromResult<object>(new
        {
            DirectoryPath = resolvedPath,
            Pattern = pattern,
            Recursive = recursive,
            TotalEntries = entries.Count,
            Entries = entries
        });
    }

    public Task<object> GetFileInfoAsync(string resolvedPath)
    {
        logger.LogInformation("Getting file info for '{FilePath}'", resolvedPath);

        if (Directory.Exists(resolvedPath))
        {
            DirectoryInfo dirInfo = new(resolvedPath);
            return Task.FromResult<object>(new
            {
                Path = dirInfo.FullName,
                Name = dirInfo.Name,
                Type = "directory",
                LastModified = dirInfo.LastWriteTimeUtc,
                Created = dirInfo.CreationTimeUtc
            });
        }

        if (!File.Exists(resolvedPath))
        {
            logger.LogWarning("Path not found: '{FilePath}'", resolvedPath);
            return Task.FromResult<object>(new { Error = $"Path not found: {resolvedPath}" });
        }

        FileInfo fileInfo = new(resolvedPath);
        int lineCount = 0;
        bool isBinary = IsBinaryFile(resolvedPath);

        if (!isBinary && fileInfo.Length <= config.MaxFileSizeBytes)
        {
            using StreamReader reader = new(resolvedPath);
            while (reader.ReadLine() is not null)
            {
                lineCount++;
            }
        }

        return Task.FromResult<object>(new
        {
            Path = fileInfo.FullName,
            Name = fileInfo.Name,
            Type = "file",
            Extension = fileInfo.Extension,
            SizeBytes = fileInfo.Length,
            LastModified = fileInfo.LastWriteTimeUtc,
            Created = fileInfo.CreationTimeUtc,
            IsBinary = isBinary,
            LineCount = isBinary ? (int?)null : lineCount
        });
    }

    public Task<object> SearchContentAsync(
        string resolvedPath,
        string searchTerm,
        string? filePattern = null)
    {
        logger.LogInformation("Searching for '{SearchTerm}' in '{DirectoryPath}' with pattern '{FilePattern}'", searchTerm, resolvedPath, filePattern);

        if (!Directory.Exists(resolvedPath))
        {
            logger.LogWarning("Directory not found: '{DirectoryPath}'", resolvedPath);
            return Task.FromResult<object>(new { Error = $"Directory not found: {resolvedPath}" });
        }

        string pattern = filePattern ?? "*";
        List<object> matches = [];
        int filesSearched = 0;
        const int maxMatches = 100;

        foreach (string file in Directory.EnumerateFiles(resolvedPath, pattern, SearchOption.AllDirectories))
        {
            if (IsBinaryFile(file))
            {
                continue;
            }

            FileInfo fileInfo = new(file);
            if (fileInfo.Length > config.MaxFileSizeBytes)
            {
                continue;
            }

            filesSearched++;
            int lineNumber = 0;

            foreach (string line in File.ReadLines(file))
            {
                lineNumber++;
                if (line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        LineNumber = lineNumber,
                        LineContent = line.Trim()
                    });

                    if (matches.Count >= maxMatches)
                    {
                        return Task.FromResult<object>(new
                        {
                            SearchTerm = searchTerm,
                            DirectoryPath = resolvedPath,
                            Pattern = pattern,
                            FilesSearched = filesSearched,
                            TotalMatches = matches.Count,
                            Truncated = true,
                            Matches = matches
                        });
                    }
                }
            }
        }

        return Task.FromResult<object>(new
        {
            SearchTerm = searchTerm,
            DirectoryPath = resolvedPath,
            Pattern = pattern,
            FilesSearched = filesSearched,
            TotalMatches = matches.Count,
            Truncated = false,
            Matches = matches
        });
    }

    internal bool IsWithinAllowedConfigDirectories(string resolvedPath) =>
        IsWithinDirectories(resolvedPath, config.AllowedDirectories);

    internal static bool IsWithinDirectories(string resolvedPath, IEnumerable<string> directories)
    {
        foreach (string dir in directories)
        {
            string canonicalDir = Path.GetFullPath(dir);
            if (!canonicalDir.EndsWith(Path.DirectorySeparatorChar))
            {
                canonicalDir += Path.DirectorySeparatorChar;
            }

            if (resolvedPath.StartsWith(canonicalDir, StringComparison.OrdinalIgnoreCase) ||
                resolvedPath.Equals(canonicalDir.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsBinaryFile(string filePath)
    {
        string[] textExtensions =
        [
            ".txt", ".md", ".markdown", ".csv", ".tsv", ".json", ".xml", ".yaml", ".yml",
            ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx",
            ".cs", ".csproj", ".sln", ".slnx", ".props", ".targets",
            ".py", ".rb", ".go", ".rs", ".java", ".kt", ".swift",
            ".c", ".cpp", ".h", ".hpp",
            ".sh", ".bash", ".zsh", ".ps1", ".psm1",
            ".sql", ".graphql",
            ".env", ".gitignore", ".editorconfig", ".dockerignore",
            ".toml", ".ini", ".cfg", ".conf", ".config",
            ".log", ".razor", ".cshtml"
        ];

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (textExtensions.Contains(extension))
        {
            return false;
        }

        if (string.IsNullOrEmpty(extension))
        {
            return DetectBinaryContent(filePath);
        }

        return DetectBinaryContent(filePath);
    }

    private static bool DetectBinaryContent(string filePath)
    {
        try
        {
            byte[] buffer = new byte[8192];
            using FileStream stream = File.OpenRead(filePath);
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < bytesRead; i++)
            {
                byte b = buffer[i];
                if (b == 0)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return true;
        }
    }
}

public class PathValidationResult
{
    public bool IsAllowed { get; private init; }

    public string? ResolvedPath { get; private init; }

    public string? Error { get; private init; }

    public string? RequiresAccess { get; private init; }

    public static PathValidationResult Allowed(string resolvedPath) =>
        new() { IsAllowed = true, ResolvedPath = resolvedPath };

    public static PathValidationResult Denied(string error, string? requiresAccess = null, string? requestedPath = null) =>
        new() { IsAllowed = false, Error = error, RequiresAccess = requiresAccess ?? requestedPath };
}
