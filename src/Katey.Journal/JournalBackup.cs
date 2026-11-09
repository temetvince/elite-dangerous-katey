using Microsoft.Extensions.Logging;

namespace Katey.Journal;

/// <summary>What one backup run did.</summary>
/// <param name="Copied">Files written to the destination because they were new or had changed.</param>
/// <param name="Skipped">Files already up to date in the destination.</param>
/// <param name="Preserved">Earlier copies kept under a <c>.rewritten-</c> name because the source had shrunk.</param>
/// <param name="BytesCopied">Bytes written.</param>
/// <param name="Failures">One line per file that could not be copied, naming the file and the reason.</param>
public sealed record BackupResult(int Copied, int Skipped, long BytesCopied, IReadOnlyList<string> Failures, int Preserved = 0)
{
    /// <summary>Gets whether every file is in the destination.</summary>
    public bool IsComplete => Failures.Count == 0;
}

/// <summary>Something that mirrors one folder into another.</summary>
public interface IJournalBackup
{
    /// <summary>Mirrors a folder, files and subfolders, into another.</summary>
    /// <param name="sourceFolder">The folder to copy. Must exist.</param>
    /// <param name="destinationFolder">Where the copy goes. Created when missing. Must not be the source or inside it.</param>
    /// <returns>What was done. A file that cannot be read or written is reported in the failures, not thrown.</returns>
    /// <exception cref="ArgumentException">A folder is empty, the source does not exist, or the destination is the source or inside it.</exception>
    BackupResult Copy(string sourceFolder, string destinationFolder);
}

/// <summary>
/// Mirrors the journal folder into a backup folder: every file and subfolder, copying only what is new
/// or has changed since the last run, so a folder that a cloud drive syncs makes an off-machine copy.
/// </summary>
/// <remarks>
/// A file is copied when the destination lacks it, its size differs, or the source was written more than two
/// seconds after the copy. Files the game holds open are read with sharing allowed, so the live journal and
/// the status files copy while the game runs. The copy keeps each file's last-write time. Nothing in the
/// destination is ever deleted.
/// </remarks>
public sealed class JournalBackup : IJournalBackup
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(2);

    private readonly ILogger<JournalBackup> _logger;

    /// <summary>Creates the backup.</summary>
    /// <param name="logger">Where each run's summary and each failure go.</param>
    public JournalBackup(ILogger<JournalBackup> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public BackupResult Copy(string sourceFolder, string destinationFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolder);
        var source = Path.GetFullPath(sourceFolder);
        var destination = Path.GetFullPath(destinationFolder);
        if (!Directory.Exists(source))
        {
            throw new ArgumentException($"The folder {source} does not exist.", nameof(sourceFolder));
        }

        if (IsSameOrInside(destination, source))
        {
            throw new ArgumentException("The backup folder must not be the journal folder or inside it.", nameof(destinationFolder));
        }

        var copied = 0;
        var skipped = 0;
        var preserved = 0;
        var bytes = 0L;
        var failures = new List<string>();
        Directory.CreateDirectory(destination);
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, path);
            var target = Path.Combine(destination, relative);
            try
            {
                var from = new FileInfo(path);
                var to = new FileInfo(target);
                if (to.Exists && to.Length == from.Length && from.LastWriteTimeUtc - to.LastWriteTimeUtc <= Tolerance)
                {
                    skipped++;
                    continue;
                }

                Directory.CreateDirectory(to.DirectoryName!);
                if (to.Exists && from.Length < to.Length && JournalFolder.IsJournal(from.Name))
                {
                    // A journal shrank: the game rewrote it. The longer copy is history nothing else holds. The
                    // live files shrink and grow all day and are rewritten whole, so they are simply replaced.
                    var keptName = $"{Path.GetFileNameWithoutExtension(to.Name)}.rewritten-{to.LastWriteTimeUtc:yyyyMMddTHHmmss}{to.Extension}";
                    var kept = Path.Combine(to.DirectoryName!, keptName);
                    if (!File.Exists(kept))
                    {
                        File.Move(to.FullName, kept);
                        Log.BackupPreserved(_logger, relative, keptName);
                        preserved++;
                    }
                }

                bytes += CopyFile(from, to);
                copied++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                failures.Add($"{relative}: {exception.Message}");
                Log.BackupFileFailed(_logger, relative, exception);
            }
        }

        Log.BackedUp(_logger, copied, skipped, failures.Count);
        return new BackupResult(copied, skipped, bytes, failures, preserved);
    }

    private static bool IsSameOrInside(string candidate, string folder)
    {
        var root = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return path.StartsWith(root, comparison);
    }

    private static long CopyFile(FileInfo from, FileInfo to)
    {
        using var reader = new FileStream(from.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using (var writer = new FileStream(to.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            reader.CopyTo(writer);
        }

        File.SetLastWriteTimeUtc(to.FullName, from.LastWriteTimeUtc);
        return reader.Length;
    }
}
