using System.ComponentModel;
using DailyWork.Mcp.FileSystem.Services;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.FileSystem.Tools;

[McpServerToolType]
public class FileSystemTools(FileSystemService fileSystem)
{
    [McpServerTool, Description("Read the contents of a file. Returns file content as text. Use maxLines to limit output for large files.")]
    public async Task<object> ReadFile(
        string filePath,
        int? maxLines = null,
        CancellationToken cancellationToken = default)
    {
        PathValidationResult validation = await fileSystem
            .ValidateAndResolvePathAsync(filePath, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsAllowed)
        {
            return new { validation.Error, validation.RequiresAccess };
        }

        return await fileSystem
            .ReadFileAsync(validation.ResolvedPath!, maxLines, cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("List files and directories at a given path. Use searchPattern for filtering (e.g., '*.md') and recursive to include subdirectories.")]
    public async Task<object> ListFiles(
        string directoryPath,
        string? searchPattern = null,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        PathValidationResult validation = await fileSystem
            .ValidateAndResolvePathAsync(directoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsAllowed)
        {
            return new { validation.Error, validation.RequiresAccess };
        }

        return await fileSystem
            .ListDirectoryAsync(validation.ResolvedPath!, searchPattern, recursive)
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Get metadata about a file or directory including size, last modified date, extension, and line count.")]
    public async Task<object> GetFileInfo(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        PathValidationResult validation = await fileSystem
            .ValidateAndResolvePathAsync(filePath, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsAllowed)
        {
            return new { validation.Error, validation.RequiresAccess };
        }

        return await fileSystem
            .GetFileInfoAsync(validation.ResolvedPath!)
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Search for text content across files in a directory. Returns matching lines with file paths and line numbers. Use filePattern to filter by file type (e.g., '*.cs').")]
    public async Task<object> SearchInFiles(
        string directoryPath,
        string searchTerm,
        string? filePattern = null,
        CancellationToken cancellationToken = default)
    {
        PathValidationResult validation = await fileSystem
            .ValidateAndResolvePathAsync(directoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsAllowed)
        {
            return new { validation.Error, validation.RequiresAccess };
        }

        return await fileSystem
            .SearchContentAsync(validation.ResolvedPath!, searchTerm, filePattern)
            .ConfigureAwait(false);
    }
}
