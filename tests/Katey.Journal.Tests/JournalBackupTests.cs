using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.Journal.Tests;

/// <summary>Checks the folder mirror: new, changed and current files, subfolders, open files, and the guard against copying into itself.</summary>
public sealed class JournalBackupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "katey-backup-" + Guid.NewGuid().ToString("N"));

    public JournalBackupTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Copy_KeepsTheEarlierCopy_WhenTheSourceHasShrunk()
    {
        var source = Path.Combine(_root, "journals");
        var destination = Path.Combine(_root, "backup");
        Directory.CreateDirectory(source);
        var journal = Path.Combine(source, "Journal.2026-11-08T093719.01.log");
        File.WriteAllText(journal, "a long day of events");
        var backup = new JournalBackup(NullLogger<JournalBackup>.Instance);
        Assert.Equal(0, backup.Copy(source, destination).Preserved);

        // The game relaunched and wrote the same file again from the start.
        File.WriteAllText(journal, "header");
        File.SetLastWriteTimeUtc(journal, DateTime.UtcNow.AddMinutes(1));
        var result = backup.Copy(source, destination);
        Assert.Equal(1, result.Copied);
        Assert.Equal(1, result.Preserved);
        Assert.Equal("header", File.ReadAllText(Path.Combine(destination, "Journal.2026-11-08T093719.01.log")));
        var kept = Assert.Single(Directory.GetFiles(destination, "Journal.2026-11-08T093719.01.rewritten-*.log"));
        Assert.Equal("a long day of events", File.ReadAllText(kept));

        // The same again keeps the second copy too, under its own time.
        File.WriteAllText(journal, "hi");
        File.SetLastWriteTimeUtc(journal, DateTime.UtcNow.AddMinutes(2));
        File.SetLastWriteTimeUtc(Path.Combine(destination, "Journal.2026-11-08T093719.01.log"), DateTime.UtcNow.AddMinutes(-5));
        backup.Copy(source, destination);
        Assert.Equal(2, Directory.GetFiles(destination, "Journal.2026-11-08T093719.01.rewritten-*.log").Length);
    }

    [Fact]
    public void Copy_MirrorsNewAndChangedFiles_AndSkipsCurrentOnes()
    {
        var source = Path.Combine(_root, "journals");
        var destination = Path.Combine(_root, "backup");
        Directory.CreateDirectory(Path.Combine(source, "nested"));
        File.WriteAllText(Path.Combine(source, "Journal.1.log"), "one");
        File.WriteAllText(Path.Combine(source, "Status.json"), "{}");
        File.WriteAllText(Path.Combine(source, "nested", "note.txt"), "deep");
        var backup = new JournalBackup(NullLogger<JournalBackup>.Instance);

        var first = backup.Copy(source, destination);
        Assert.Equal(3, first.Copied);
        Assert.Equal(0, first.Skipped);
        Assert.Equal(9, first.BytesCopied);
        Assert.True(first.IsComplete);
        Assert.Equal("deep", File.ReadAllText(Path.Combine(destination, "nested", "note.txt")));
        Assert.Equal(File.GetLastWriteTimeUtc(Path.Combine(source, "Journal.1.log")), File.GetLastWriteTimeUtc(Path.Combine(destination, "Journal.1.log")));

        var second = backup.Copy(source, destination);
        Assert.Equal(0, second.Copied);
        Assert.Equal(3, second.Skipped);

        // The live journal grows while the game holds it open for writing.
        using (var live = new FileStream(Path.Combine(source, "Journal.1.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            live.Write("two"u8);
            live.Flush();
            var third = backup.Copy(source, destination);
            Assert.Equal(1, third.Copied);
            Assert.Equal(2, third.Skipped);
        }

        Assert.Equal("onetwo", File.ReadAllText(Path.Combine(destination, "Journal.1.log")));
    }

    [Fact]
    public void Copy_RefusesTheSourceAndItsSubfolders_AndAMissingSource()
    {
        var source = Path.Combine(_root, "journals");
        Directory.CreateDirectory(source);
        var backup = new JournalBackup(NullLogger<JournalBackup>.Instance);

        Assert.Throws<ArgumentException>(() => backup.Copy(source, source));
        Assert.Throws<ArgumentException>(() => backup.Copy(source, Path.Combine(source, "inside")));
        Assert.Throws<ArgumentException>(() => backup.Copy(Path.Combine(_root, "missing"), Path.Combine(_root, "backup")));
        Assert.Throws<ArgumentException>(() => backup.Copy(" ", Path.Combine(_root, "backup")));
    }
}
