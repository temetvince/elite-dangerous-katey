namespace Katey.App.Pages;

/// <summary>A page whose subject can be copied to the clipboard as a journal <c>Loadout</c> for EDSY.</summary>
public interface IEdsyExportable
{
    /// <summary>Gets the <c>Loadout</c> event as JSON, or empty when there is nothing to export.</summary>
    string EdsyJson { get; }

    /// <summary>Gets or sets what the window last said about the copy, such as "Copied."</summary>
    string ExportStatus { get; set; }
}
