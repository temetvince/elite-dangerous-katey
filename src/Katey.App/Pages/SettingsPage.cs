using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>The journal folder and the watch, the journal backup, the Spansh switch, and where Katey keeps its saved data.</summary>
public sealed class SettingsPage : Page
{
    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public SettingsPage(Workspace workspace)
        : base(new PageKey(PageKind.Settings), workspace)
    {
        Title = "Settings";
        Kicker = "Journal";
        Folder = workspace.JournalFolder;
        WatchCommand = new RelayCommand(Watch, () => Folder.Trim().Length > 0);
        StopCommand = new RelayCommand(workspace.Monitor.Stop, () => IsWatching);
        DetectCommand = new RelayCommand(Detect);
        BackupNowCommand = new RelayCommand(() => workspace.Backup.BackUpNow(workspace.JournalFolder), () => workspace.Backup.IsConfigured && !workspace.Backup.IsBusy);
        Refresh();
    }

    /// <summary>Gets or sets the journal folder to watch. Saved when the watch starts.</summary>
    public string Folder
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                WatchCommand?.RaiseCanExecuteChanged();
            }
        }
    } = "";

    /// <summary>Gets whether the monitor is reading the folder, catching up or following.</summary>
    public bool IsWatching => Workspace.Monitor.State is MonitorState.Watching or MonitorState.CatchingUp;

    /// <summary>Gets one line describing the watch.</summary>
    public string Status { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on the inventory snapshot.</summary>
    public string InventoryText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets or sets whether Katey may ask Spansh for routes. Saved; off until turned on.</summary>
    public bool AllowSpansh
    {
        get => Workspace.Network.AllowRoutes;
        set
        {
            if (Workspace.Network.AllowRoutes != value)
            {
                Workspace.Network.SetRoutes(value);
                RefreshNetwork();
            }
        }
    }

    /// <summary>Gets or sets whether Katey may ask Spansh about the bodies of the system the ship is in. Saved; off until turned on.</summary>
    public bool AllowSpanshBodies
    {
        get => Workspace.Network.AllowSystemBodies;
        set
        {
            if (Workspace.Network.AllowSystemBodies != value)
            {
                Workspace.Network.SetSystemBodies(value);
                RefreshNetwork();
            }
        }
    }

    /// <summary>Gets what each outgoing request carries.</summary>
    public static string NetworkText => "Nothing leaves this machine unless you tick it here. A route plot sends Spansh the two system names and the ship's jump numbers: masses, tank sizes and the drive's figures. A system lookup sends Spansh the id of the system you have arrived in and brings back the bodies it knows there. Every request names Katey, its version and the project's address.";

    private void RefreshNetwork()
    {
        OnPropertyChanged(nameof(AllowSpansh));
        OnPropertyChanged(nameof(AllowSpanshBodies));
    }

    /// <summary>Gets or sets the folder the journal folder is backed up to. Empty for no backup. Saved as typed.</summary>
    public string BackupFolder
    {
        get => Workspace.Backup.Folder;
        set
        {
            if (!string.Equals(Workspace.Backup.Folder, (value ?? "").Trim(), StringComparison.Ordinal))
            {
                Workspace.Backup.Folder = value ?? "";
                OnPropertyChanged();
                BackupNowCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the backup runs when Katey closes. Saved.</summary>
    public bool BackupOnClose
    {
        get => Workspace.Backup.OnClose;
        set
        {
            if (Workspace.Backup.OnClose != value)
            {
                Workspace.Backup.OnClose = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets one line on the last backup, or what it is doing, or empty.</summary>
    public string BackupStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the command that backs the journal folder up now, in the background.</summary>
    public RelayCommand BackupNowCommand { get; }

    /// <summary>Gets what the backup copies, so the folder is an informed choice.</summary>
    public static string BackupText => "Copies every file in the journal folder, the live files included, into the folder you choose, and only what is new or changed. Point it at a folder your cloud drive syncs and the journals leave the machine.";

    /// <summary>
    /// Gets or sets the folder the saved data is kept in. Reads as the folder of the store's current file.
    /// Setting a different, non-empty folder moves the saved data there at once and is remembered across
    /// runs; an empty or unchanged value does nothing.
    /// </summary>
    public string DataFolder
    {
        get => Path.GetDirectoryName(Workspace.Store.Location) ?? "";
        set
        {
            var folder = (value ?? "").Trim();
            if (folder.Length > 0 && !string.Equals(DataFolder, folder, StringComparison.Ordinal))
            {
                MoveData(folder);
            }

            OnPropertyChanged();
        }
    }

    /// <summary>Gets one line on the last move, or why it did not happen, or empty.</summary>
    public string DataStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets what the saved data folder holds and what choosing another does, so the folder is an informed choice.</summary>
    public static string DataText => "Katey keeps its plan, engineers, fits and settings in this folder. Choose another and it keeps them there from then on. If that folder already holds a Katey data file, that file is loaded and becomes the saved data; otherwise the current data is written there. The old file is left where it was. Point two machines at one synced folder to share one set of data.";

    /// <summary>Gets the command that starts watching <see cref="Folder"/>.</summary>
    public RelayCommand WatchCommand { get; }

    /// <summary>Gets the command that stops watching.</summary>
    public RelayCommand StopCommand { get; }

    /// <summary>Gets the command that replaces <see cref="Folder"/> with the detected default, when one exists.</summary>
    public RelayCommand DetectCommand { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        OnPropertyChanged(nameof(IsWatching));
        RefreshNetwork();
        OnPropertyChanged(nameof(BackupFolder));
        OnPropertyChanged(nameof(BackupOnClose));
        BackupStatus = Workspace.Backup.Status;
        BackupNowCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(DataFolder));
        var progress = Workspace.Monitor.Progress;
        var inventory = Workspace.Inventory;
        Status = Workspace.Monitor.State switch
        {
            MonitorState.Stopped => "Not watching. Press Watch to read the journal.",
            MonitorState.FolderMissing => "That folder does not exist. Check the path or press Detect.",
            MonitorState.CatchingUp => string.Create(CultureInfo.InvariantCulture, $"Reading journal history: {progress.FilesDone} of {progress.FileCount} files."),
            MonitorState.Watching when !inventory.HasSnapshot => "Watching. No Materials snapshot yet: start the game and the inventory appears.",
            MonitorState.Watching => "Watching. Everything updates as the game writes its journal.",
            _ => "",
        };
        InventoryText = inventory.HasSnapshot
            ? $"Commander {inventory.Commander}, inventory as of {inventory.AsOf?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}."
            : "";
    }

    private void Watch()
    {
        var folder = Folder.Trim();
        if (folder.Length == 0)
        {
            return;
        }

        Workspace.JournalFolder = folder;
        Workspace.Monitor.Start(folder);
        Refresh();
    }

    private void Detect()
    {
        if (JournalFolder.FindDefault() is { } found)
        {
            Folder = found;
        }
    }

    private void MoveData(string folder)
    {
        bool found;
        try
        {
            found = Workspace.MoveStateTo(folder);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            DataStatus = $"Could not keep the saved data in {folder}: {exception.Message}";
            return;
        }

        DataStatus = found
            ? $"Loaded the Katey data already in {folder}. It is the saved data from now on."
            : $"The saved data now lives in {folder}.";
        Refresh();
    }
}
