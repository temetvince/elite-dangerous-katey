using Katey.Core;

namespace Katey.App.Text;

/// <summary>
/// The keys of <c>CatalogNames.resx</c>: how each game name is addressed, and the full set a translation must
/// hold. The neutral table is empty, so <see cref="All"/> is what the completeness test and a translator work
/// from.
/// </summary>
public static class CatalogNameKeys
{
    /// <summary>The template an atmosphere is written through: <c>{modifiers}</c>, each with a trailing space, then <c>{gas}</c>.</summary>
    public const string AtmosphereTemplate = "Atmosphere.Template";

    /// <summary>The template volcanism is written through: <c>{magnitude}</c>, with a trailing space or empty, then <c>{kind}</c>.</summary>
    public const string VolcanismTemplate = "Volcanism.Template";

    /// <summary>Gets the words the journal puts before an atmosphere's gas.</summary>
    public static IReadOnlyList<string> AtmosphereModifiers { get; } = ["hot", "thin", "thick"];

    /// <summary>Gets the gases the journal names in an atmosphere, as it spells them.</summary>
    public static IReadOnlyList<string> AtmosphereGases { get; } =
    [
        "ammonia", "ammonia rich", "ammonia and oxygen", "argon", "argon rich", "carbon dioxide", "carbon dioxide rich",
        "earth like", "helium", "metallic vapour", "methane", "methane rich", "neon", "neon rich", "nitrogen", "oxygen",
        "silicate vapour", "sulfur dioxide", "sulphur dioxide", "water", "water rich",
    ];

    /// <summary>Gets the words the journal puts before volcanism's kind.</summary>
    public static IReadOnlyList<string> VolcanismMagnitudes { get; } = ["minor", "major"];

    /// <summary>Gets the kinds of volcanism the journal names, as it spells them.</summary>
    public static IReadOnlyList<string> VolcanismKinds { get; } =
    [
        "ammonia magma", "carbon dioxide geysers", "helium geysers", "metallic magma", "methane magma", "nitrogen magma",
        "nitrogen geysers", "rocky magma", "silicate magma", "silicate vapour geysers", "water geysers", "water magma",
    ];

    /// <summary>Gets the planet classes the journal and the body catalogues spell, as the journal spells them.</summary>
    public static IReadOnlyList<string> PlanetClasses { get; } =
    [
        "Metal rich body", "High metal content body", "Rocky body", "Icy body", "Rocky ice body", "Earthlike body",
        "Water world", "Ammonia world", "Water giant", "Water giant with life", "Gas giant with water based life",
        "Gas giant with ammonia based life", "Sudarsky class I gas giant", "Sudarsky class II gas giant",
        "Sudarsky class III gas giant", "Sudarsky class IV gas giant", "Sudarsky class V gas giant",
        "Helium rich gas giant", "Helium gas giant",
    ];

    /// <summary>Gets the module mounts the module catalog names.</summary>
    public static IReadOnlyList<string> Mounts { get; } = ["Fixed", "Gimballed", "Turreted"];

    /// <summary>Gets the surface signal types the journal reports besides the biological and geological counts, as the game's symbols.</summary>
    public static IReadOnlyList<string> Signals { get; } =
    [
        "$SAA_SignalType_Human;", "$SAA_SignalType_Guardian;", "$SAA_SignalType_Thargoid;", "$SAA_SignalType_Other;",
        "$SAA_SignalType_PlanetAnomaly;", "$PlanetaryMiningLocation_Name;",
    ];

    /// <summary>Gets the material families the traders group by, as <c>materials.json</c> spells them in its <c>line</c> column.</summary>
    public static IReadOnlyList<string> Families { get; } =
    [
        "Carbon", "Iron", "Lead", "Nickel", "Phosphorus", "Rhenium", "Sulphur",
        "Alloys", "Capacitors", "Chemical", "Composite", "Conductive", "Crystals", "Guardian", "Heat",
        "Mechanical Components", "Shielding", "Thargoid", "Thermic",
        "Data Archives", "Emission Data", "Encoded Firmware", "Encryption Files", "Shield Data", "Wake Scans",
    ];

    /// <summary>Gets the sections of the game's lifetime statistics.</summary>
    public static IReadOnlyList<string> FigureSections { get; } =
    [
        "Bank_Account", "Combat", "Crafting", "Crew", "Crime", "Exobiology", "Exploration", "Material_Trader_Stats", "Mining",
        "Multicrew", "Passengers", "Search_And_Rescue", "Smuggling", "Squadron", "Trading",
    ];

    /// <summary>Gets the lifetime figures the game reports, by their journal keys.</summary>
    public static IReadOnlyList<string> Figures { get; } =
    [
        "Current_Wealth", "Insurance_Claims", "MercCoins_Current", "MercCoins_Spent_On_Engineering", "MercCoins_Spent_On_MercGear",
        "MercCoins_Total_Earned", "MercCoins_Total_Spent", "Owned_Ship_Count", "Premium_Stock_Bought", "Spent_On_Ammo_Consumables",
        "Spent_On_Fuel", "Spent_On_Insurance", "Spent_On_Outfitting", "Spent_On_Premium_Stock", "Spent_On_Repairs", "Spent_On_Ships",
        "Spent_On_Suit_Consumables", "Spent_On_Suits", "Spent_On_Weapons", "Suits_Owned", "Weapons_Owned",
        "Assassination_Profits", "Assassinations", "Bounties_Claimed", "Bounty_Hunting_Profit", "Combat_Bond_Profits", "Combat_Bonds",
        "ConflictZone_High", "ConflictZone_High_Wins", "ConflictZone_Low", "ConflictZone_Low_Wins", "ConflictZone_Medium",
        "ConflictZone_Medium_Wins", "ConflictZone_Total", "ConflictZone_Total_Wins", "Dropships_Booked", "Dropships_Cancelled",
        "Dropships_Taken", "Highest_Single_Reward", "OnFoot_Combat_Bonds", "OnFoot_Combat_Bonds_Profits", "OnFoot_Scavs_Killed",
        "OnFoot_Ships_Destroyed", "OnFoot_Skimmers_Killed", "OnFoot_Vehicles_Destroyed", "Settlement_Conquered", "Settlement_Defended",
        "Skimmers_Killed",
        "Count_Of_Used_Engineers", "Recipes_Generated", "Recipes_Generated_Rank_1", "Recipes_Generated_Rank_2", "Recipes_Generated_Rank_3",
        "Recipes_Generated_Rank_4", "Recipes_Generated_Rank_5", "Suit_Mods_Applied", "Suit_Mods_Applied_Full", "Suits_Upgraded",
        "Suits_Upgraded_Full", "Weapon_Mods_Applied", "Weapon_Mods_Applied_Full", "Weapons_Upgraded", "Weapons_Upgraded_Full",
        "NpcCrew_Died", "NpcCrew_Fired", "NpcCrew_Hired", "NpcCrew_TotalWages",
        "Bounties_Received", "Citizens_Murdered", "Data_Stolen", "Fines", "Goods_Stolen", "Guards_Murdered", "Highest_Bounty",
        "Malware_Uploaded", "Notoriety", "Omnipol_Murdered", "Production_Sabotage", "Production_Theft", "Profiles_Cloned", "Sample_Stolen",
        "Settlements_State_Shutdown", "Total_Bounties", "Total_Fines", "Total_Murders", "Total_Stolen", "Turrets_Destroyed",
        "Turrets_Overloaded", "Turrets_Total", "Value_Stolen_StateChange",
        "First_Logged", "First_Logged_Profits", "Organic_Data", "Organic_Data_Profits", "Organic_Genus", "Organic_Genus_Encountered",
        "Organic_Planets", "Organic_Species", "Organic_Species_Encountered", "Organic_Systems", "Organic_Variant_Encountered",
        "Efficient_Scans", "Exploration_Profits", "First_Footfalls", "Greatest_Distance_From_Start", "Highest_Payout",
        "OnFoot_Distance_Travelled", "Planet_Footfalls", "Planets_Scanned_To_Level_2", "Planets_Scanned_To_Level_3", "Settlements_Visited",
        "Shuttle_Distance_Travelled", "Shuttle_Journeys", "Spent_On_Shuttles", "Systems_Visited", "Time_Played", "Total_Hyperspace_Distance",
        "Total_Hyperspace_Jumps",
        "Assets_Traded_In", "Assets_Traded_Out", "Encoded_Materials_Traded", "Grade_1_Materials_Traded", "Grade_2_Materials_Traded",
        "Grade_3_Materials_Traded", "Grade_4_Materials_Traded", "Grade_5_Materials_Traded", "Materials_Traded", "Raw_Materials_Traded",
        "Trades_Completed",
        "Materials_Collected", "Mining_Profits", "Quantity_Mined",
        "Multicrew_Credits_Total", "Multicrew_Fighter_Time_Total", "Multicrew_Fines_Total", "Multicrew_Gunner_Time_Total", "Multicrew_Time_Total",
        "Passengers_Missions_Accepted", "Passengers_Missions_Bulk", "Passengers_Missions_Delivered", "Passengers_Missions_Ejected", "Passengers_Missions_VIP",
        "Maglocks_Opened", "Panels_Opened", "Salvage_Illegal_POI", "Salvage_Illegal_Settlements", "Salvage_Legal_POI", "Salvage_Legal_Settlements",
        "SearchRescue_Count", "SearchRescue_Profit", "SearchRescue_Traded", "Settlements_State_FireOut", "Settlements_State_Reboot",
        "Average_Profit", "Black_Markets_Profits", "Black_Markets_Traded_With", "Highest_Single_Transaction", "Resources_Smuggled",
        "Squadron_Bank_Commodities_Deposited_Num", "Squadron_Bank_Commodities_Deposited_Value", "Squadron_Bank_Commodities_Withdrawn_Num",
        "Squadron_Bank_Commodities_Withdrawn_Value", "Squadron_Bank_Credits_Deposited", "Squadron_Bank_Credits_Withdrawn",
        "Squadron_Bank_PersonalAssets_Deposited_Num", "Squadron_Bank_PersonalAssets_Deposited_Value", "Squadron_Bank_PersonalAssets_Withdrawn_Num",
        "Squadron_Bank_PersonalAssets_Withdrawn_Value", "Squadron_Bank_Ships_Deposited_Num", "Squadron_Bank_Ships_Deposited_Value",
        "Squadron_Leaderboard_aegis_highestcontribution", "Squadron_Leaderboard_bgs_highestcontribution", "Squadron_Leaderboard_bounty_highestcontribution",
        "Squadron_Leaderboard_colonisation_contribution_highestcontribution", "Squadron_Leaderboard_combat_highestcontribution",
        "Squadron_Leaderboard_cqc_highestcontribution", "Squadron_Leaderboard_exploration_highestcontribution", "Squadron_Leaderboard_mining_highestcontribution",
        "Squadron_Leaderboard_operationscore_highestcontribution", "Squadron_Leaderboard_podiums", "Squadron_Leaderboard_powerplay_highestcontribution",
        "Squadron_Leaderboard_trade_highestcontribution", "Squadron_Leaderboard_trade_illicit_highestcontribution",
        "Assets_Sold", "Data_Sold", "Goods_Sold", "Market_Profits", "Markets_Traded_With", "Resources_Traded",
    ];

    /// <summary>The key of a material's name.</summary>
    /// <param name="symbol">The journal symbol.</param>
    /// <returns>The key.</returns>
    public static string Material(string symbol) => "Material." + symbol;

    /// <summary>The key of a blueprint's name.</summary>
    /// <param name="key">The blueprint key.</param>
    /// <returns>The key.</returns>
    public static string Blueprint(string key) => "Blueprint." + key;

    /// <summary>The key of an experimental effect's name.</summary>
    /// <param name="key">The effect's key.</param>
    /// <returns>The key.</returns>
    public static string Experimental(string key) => "Experimental." + key;

    /// <summary>The key of a module type's name.</summary>
    /// <param name="code">The module type code.</param>
    /// <returns>The key.</returns>
    public static string ModuleType(string code) => "ModuleType." + code;

    /// <summary>The key of an unlock's name.</summary>
    /// <param name="key">The unlock key.</param>
    /// <returns>The key.</returns>
    public static string Unlock(string key) => "Unlock." + key;

    /// <summary>The key of a mount's word.</summary>
    /// <param name="mount">The mount as the module catalog spells it.</param>
    /// <returns>The key.</returns>
    public static string Mount(string mount) => "Mount." + mount;

    /// <summary>The key for a material family.</summary>
    /// <param name="line">The family as <c>materials.json</c> spells it, such as <c>Wake Scans</c>.</param>
    /// <returns><c>Family.</c> and the family lower-cased with underscores for spaces.</returns>
    public static string Family(string line) => "Family." + Token(line);

    /// <summary>The key for a surface signal type.</summary>
    /// <param name="type">The game's symbol, such as <c>$SAA_SignalType_Human;</c>.</param>
    /// <returns><c>Signal.</c> and the symbol without its <c>$</c> and <c>;</c>, lower-cased.</returns>
    public static string Signal(string type) => "Signal." + Token(type.Trim('$', ';'));

    /// <summary>The key of a planet class.</summary>
    /// <param name="planetClass">The class as the journal spells it.</param>
    /// <returns>The key, the class's spaces as underscores.</returns>
    public static string PlanetClass(string planetClass) => "PlanetClass." + Token(planetClass);

    /// <summary>The key of an atmosphere modifier word.</summary>
    /// <param name="word">The word as the journal spells it.</param>
    /// <returns>The key.</returns>
    public static string AtmosphereModifier(string word) => "Atmosphere.Modifier." + Token(word);

    /// <summary>The key of an atmosphere's gas.</summary>
    /// <param name="gas">The gas as the journal spells it, such as <c>carbon dioxide rich</c>.</param>
    /// <returns>The key, the gas's spaces as underscores.</returns>
    public static string AtmosphereGas(string gas) => "Atmosphere.Gas." + Token(gas);

    /// <summary>The key of a volcanism magnitude word.</summary>
    /// <param name="word">The word as the journal spells it.</param>
    /// <returns>The key.</returns>
    public static string VolcanismMagnitude(string word) => "Volcanism.Magnitude." + Token(word);

    /// <summary>The key of a volcanism kind.</summary>
    /// <param name="kind">The kind as the journal spells it, such as <c>water magma</c>.</param>
    /// <returns>The key, the kind's spaces as underscores.</returns>
    public static string VolcanismKind(string kind) => "Volcanism.Kind." + Token(kind);

    /// <summary>The key of a lifetime figure's label.</summary>
    /// <param name="key">The journal key.</param>
    /// <returns>The key.</returns>
    public static string Figure(string key) => "Figure." + key;

    /// <summary>The key of a lifetime section's label.</summary>
    /// <param name="section">The journal section.</param>
    /// <returns>The key.</returns>
    public static string FigureSection(string section) => "FigureSection." + section;

    /// <summary>Every key a complete translation holds, for a catalog.</summary>
    /// <param name="catalog">The engineering catalog, whose tables name the materials, blueprints, effects, types and unlocks.</param>
    /// <returns>The keys, in a stable order.</returns>
    public static IReadOnlyList<string> All(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return
        [
            .. catalog.Materials.Values.Select(material => Material(material.Symbol)),
            .. catalog.Blueprints.Keys.Select(Blueprint),
            .. catalog.Experimentals.Keys.Select(Experimental),
            .. catalog.ModuleTypes.Keys.Select(ModuleType),
            .. catalog.Unlocks.Keys.Select(Unlock),
            .. Mounts.Select(Mount),
            .. Families.Select(Family),
            .. Signals.Select(Signal),
            .. PlanetClasses.Select(PlanetClass),
            AtmosphereTemplate,
            .. AtmosphereModifiers.Select(AtmosphereModifier),
            .. AtmosphereGases.Select(AtmosphereGas),
            VolcanismTemplate,
            .. VolcanismMagnitudes.Select(VolcanismMagnitude),
            .. VolcanismKinds.Select(VolcanismKind),
            .. FigureSections.Select(FigureSection),
            .. Figures.Select(Figure),
        ];
    }

    private static string Token(string text) => text.Trim().ToLowerInvariant().Replace(' ', '_');
}
