using System.Text;
using System.Text.Json;
using Katey.Core.Fleet;

namespace Katey.Core.Edsy;

/// <summary>Writes a build as a journal <c>Loadout</c> event, the form EDSY imports.</summary>
public static class EdsyExport
{
    /// <summary>Writes a <c>Loadout</c> event for a hull and its modules.</summary>
    /// <param name="hull">Ship type symbol such as <c>anacondamkii</c>.</param>
    /// <param name="shipName">The ship's name, or empty.</param>
    /// <param name="shipIdent">The ship's ident, or empty.</param>
    /// <param name="modules">Every module, stock ones included. Engineering is written as blueprint, grade and experimental; EDSY fills in the statistics.</param>
    /// <returns>One JSON object on one line, as the game writes it.</returns>
    public static string LoadoutJson(string hull, string shipName, string shipIdent, IEnumerable<FitModule> modules)
    {
        ArgumentNullException.ThrowIfNull(hull);
        ArgumentNullException.ThrowIfNull(shipName);
        ArgumentNullException.ThrowIfNull(shipIdent);
        ArgumentNullException.ThrowIfNull(modules);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteString("event", "Loadout");
            writer.WriteString("Ship", hull);
            writer.WriteString("ShipName", shipName);
            writer.WriteString("ShipIdent", shipIdent);
            writer.WriteStartArray("Modules");
            foreach (var module in modules)
            {
                writer.WriteStartObject();
                writer.WriteString("Slot", module.Slot);
                writer.WriteString("Item", module.Item);
                writer.WriteBoolean("On", true);
                writer.WriteNumber("Priority", 0);
                if (module.Engineering is { } engineering)
                {
                    writer.WriteStartObject("Engineering");
                    writer.WriteString("BlueprintName", engineering.Blueprint);
                    writer.WriteNumber("Level", engineering.Level);
                    writer.WriteNumber("Quality", 1);
                    if (engineering.Experimental is { } effect)
                    {
                        writer.WriteString("ExperimentalEffect", effect);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
