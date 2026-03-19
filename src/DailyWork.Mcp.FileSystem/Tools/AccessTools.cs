using System.ComponentModel;
using DailyWork.Mcp.FileSystem.Data;
using DailyWork.Mcp.FileSystem.Entities;
using DailyWork.Mcp.FileSystem.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.FileSystem.Tools;

[McpServerToolType]
public class AccessTools(FileSystemDbContext db)
{
    [McpServerTool, Description("Add a directory to the always-allowed list so files within it can be read in future requests. Call this when the user grants persistent access to a directory.")]
    public async Task<object> AllowDirectory(
        string directoryPath,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(directoryPath);
        }
        catch (Exception ex)
        {
            return new { Error = $"Invalid path: {ex.Message}" };
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new { Error = $"Directory not found: {resolvedPath}" };
        }

        bool alreadyExists = await db.AllowedDirectories
            .AnyAsync(d => d.Path == resolvedPath, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return new
            {
                Message = "Directory is already in the allowed list.",
                Path = resolvedPath
            };
        }

        AllowedDirectory entry = new()
        {
            Id = Guid.NewGuid(),
            Path = resolvedPath,
            Label = label
        };

        db.AllowedDirectories.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            Message = "Directory added to the allowed list.",
            entry.Id,
            Path = resolvedPath,
            entry.Label,
            entry.CreatedAt
        };
    }

    [McpServerTool, Description("Remove a directory from the always-allowed list. This only affects user-granted permissions, not configuration-based defaults.")]
    public async Task<object> RevokeDirectory(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(directoryPath);
        }
        catch (Exception ex)
        {
            return new { Error = $"Invalid path: {ex.Message}" };
        }

        AllowedDirectory? entry = await db.AllowedDirectories
            .FirstOrDefaultAsync(d => d.Path == resolvedPath, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            return new
            {
                Error = "Directory not found in the allowed list.",
                Path = resolvedPath
            };
        }

        db.AllowedDirectories.Remove(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            Message = "Directory removed from the allowed list.",
            Path = resolvedPath
        };
    }

    [McpServerTool, Description("List all directories the agent is currently allowed to access, including both configuration-based and user-granted directories.")]
    public async Task<object> ListAllowedDirectories(
        CancellationToken cancellationToken = default)
    {
        List<AllowedDirectory> userGranted = await db.AllowedDirectories
            .OrderBy(d => d.Path)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new
        {
            UserGranted = userGranted.Select(d => new
            {
                d.Path,
                d.Label,
                d.CreatedAt
            }).ToArray(),
            Message = "Configuration-based directories are set at deployment and not listed here."
        };
    }
}
