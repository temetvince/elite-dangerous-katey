using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>The journal folder and the watch, the journal backup, the outgoing connection switches, the release check, and where Katey keeps its saved data.</summary>
public sealed class SettingsPage : Page
{
    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public SettingsPage(Workspace workspace)
        : base(new PageKey(PageKind.Settings), workspace)
    {
        Title = Strings.Get(Keys.Nav_Settings);
        Kicker = Strings.Get(Keys.Settings_Kicker);
        Folder = workspace.JournalFolder;
        WatchCommand = new RelayCommand(Watch, () => Folder.Trim().Length > 0);
        StopCommand = new RelayCommand(workspace.Monitor.Stop, () => IsWatching);
        DetectCommand = new RelayCommand(Detect);
        BackupNowCommand = new RelayCommand(() => workspace.Backup.BackUpNow(workspace.JournalFolder), () => workspace.Backup.IsConfigured && !workspace.Backup.IsBusy);
        CheckUpdatesCommand = new RelayCommand(workspace.Updates.Check, () => workspace.Network.AllowUpdateCheck && !workspace.Updates.IsBusy);
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

    /// <summary>Gets or sets whether Katey may ask GitHub for its latest release. Saved; off until turned on. Turning it on checks at once.</summary>
    public bool AllowUpdateCheck
    {
        get => Workspace.Network.AllowUpdateCheck;
        set
        {
            if (Workspace.Network.AllowUpdateCheck != value)
            {
                Workspace.Network.SetUpdateCheck(value);
                RefreshNetwork();
            }
        }
    }

    /// <summary>Gets what each outgoing request carries.</summary>
    public static string NetworkText => Strings.Get(Keys.Settings_NetworkText);

    /// <summary>Gets one line naming the version running.</summary>
    public static string VersionText => Strings.Format(Keys.Settings_Version, ("version", AppVersion.Text));

    /// <summary>Gets one line on the release check: off, running, a newer release, none, or why it failed.</summary>
    public string UpdateStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether the release check found a release newer than the one running.</summary>
    public bool IsUpdateAvailable { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the page of the newer release, for the view to open in the browser, or <see langword="null"/> while none is known.</summary>
    public Uri? ReleasePage => IsUpdateAvailable ? Workspace.Updates.Latest?.Page : null;

    /// <summary>Gets the command that asks GitHub for the latest release now. Runs only while the switch is on and no check is running.</summary>
    public RelayCommand CheckUpdatesCommand { get; }

    /// <summary>Gets what a release check does and does not do, so the switch is an informed choice.</summary>
    public static string UpdateText => Strings.Get(Keys.Settings_UpdateText);

    private void RefreshNetwork()
    {
        OnPropertyChanged(nameof(AllowSpansh));
        OnPropertyChanged(nameof(AllowSpanshBodies));
        OnPropertyChanged(nameof(AllowUpdateCheck));
        RefreshUpdates();
    }

    private void RefreshUpdates()
    {
        var updates = Workspace.Updates;
        UpdateStatus = updates.Status;
        IsUpdateAvailable = updates.IsUpdateAvailable;
        OnPropertyChanged(nameof(ReleasePage));
        CheckUpdatesCommand?.RaiseCanExecuteChanged();
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
    public static string BackupText => Strings.Get(Keys.Settings_BackupText);

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
    public static string DataText => Strings.Get(Keys.Settings_DataText);

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
            MonitorState.Stopped => Strings.Get(Keys.Settings_NotWatching),
            MonitorState.FolderMissing => Strings.Get(Keys.Settings_FolderMissing),
            MonitorState.CatchingUp => Strings.Format(Keys.Settings_CatchingUp, ("done", progress.FilesDone), ("total", progress.FileCount)),
            MonitorState.Watching when !inventory.HasSnapshot => Strings.Get(Keys.Settings_WatchingNoSnapshot),
            MonitorState.Watching => Strings.Get(Keys.Settings_Watching),
            _ => "",
        };
        InventoryText = inventory.HasSnapshot
            ? Strings.Format(Keys.Settings_InventoryOf, ("commander", inventory.Commander), ("when", inventory.AsOf?.ToLocalTime()))
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
            DataStatus = Strings.Format(Keys.Settings_DataMoveFailed, ("folder", folder), ("message", exception.Message));
            return;
        }

        DataStatus = found
            ? Strings.Format(Keys.Settings_DataLoaded, ("folder", folder))
            : Strings.Format(Keys.Settings_DataMoved, ("folder", folder));
        Refresh();
    }
}
