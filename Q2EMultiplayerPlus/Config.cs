using Q2EMultiplayerPlus.Template.Configuration;
using System.ComponentModel;

namespace Q2EMultiplayerPlus.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.

            By default, configuration saves as "Config.json" in mod user config folder.    
            Need more config files/classes? See Configuration.cs

            Available Attributes:
            - Category
            - DisplayName
            - Description
            - DefaultValue

            // Technically Supported but not Useful
            - Browsable
            - Localizable

            The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
        */

        private const string CategoryLobbySearch = "Lobby Search";

        [Category(CategoryLobbySearch)]
        [DisplayName("Maximum results count")]
        [Description("The maximum number of lobbies that the search will return. Minimum: 1, Maximum: 50")]
        [DefaultValue(50)]
        public int LobbyMaxResultCount { get; set; } = 50;

        [Category(CategoryLobbySearch)]
        [DisplayName("Filter by gamemode")]
        [Description("Only show lobbies of this specific gamemode")]
        [DefaultValue(LobbyFilterByGameModeEnum.Disabled)]
        public LobbyFilterByGameModeEnum LobbyFilterByGameMode { get; set; } = LobbyFilterByGameModeEnum.Disabled;

        [Category(CategoryLobbySearch)]
        [DisplayName("Sort by")]
        [Description("Sort the lobby list by a property")]
        [DefaultValue(LobbySortByEnum.Disabled)]
        public LobbySortByEnum LobbySortBy { get; set; } = LobbySortByEnum.Disabled;

        [Category(CategoryLobbySearch)]
        [DisplayName("ADVANCED: Custom filter")]
        [Description(@"ONLY FOR EXPERT USERS!
Allows you use a custom expression for filtering the lobby list.
If you type these incorrectly, you might crash the game or not get any lobby results.
There's no guarantee that they'll work as it works directly with Playfab.

Type in multiple rules and use the following operators: 'and' (And), 'eq' (Equals), 'ne' (Not equal), 'ge' (Greater or equal), 'gt' (Greater than), 'le' (Less than or equal), 'lt' (Less then)

Examples:
* Filter only by West Europe games: string_key3 eq 'pfRegion\WestEurope'
* Filter only by games with 5 players or more: lobby/memberCount ge 5
* Filter only by Coop games that are in-game: string_key5 eq 'gamemode\$m_coop' and string_key6 eq 'ingame\1'

Available keys:
* string_key2: map\<MapName>
* string_key3: pfRegion\<Region> (Playfab region, look up AzureRegion Api)
* string_key5: gamemode\<Gamemode> (Available: $m_deathmatch, $m_coop, $m_ctf, $m_tdm)
* string_key6: ingame\<0|1> (Is the lobby playing?)
* string_key7: kMaxPlayers\<Number> (Maximum number of players)
* string_key8: name\<Player name>
* string_key11: TeamChat\<0|1>
* string_key12: q2game\<Mod> (ex: baseq2)

Note that this filter applies additively to the default filter plus any filters you've chosen in the configuration.
")]
        [DefaultValue(null)]
        public string? LobbyAdvancedFilter { get; set; } = null;

        [Category(CategoryLobbySearch)]
        [DisplayName("ADVANCED: Custom sort")]
        [Description(@"ONLY FOR EXPORT USERS!
Allows you to use a custom expression for sorting the lobby list.
If you type these incorrectly, you might crash the game or not get any lobby results.
There's no guarantee that they'll work as it works directly with Playfab.

Type in the sort priority in the following format: <Key> <asc|desc>
Use comma to separate further sorting

Examples:
* Sort by gamemodes: string_key5 asc
* Sort by player name: string_key8 asc
* Sort by player count: lobby/memberCount desc
* Sort by ingame and then names: string_key6 asc,string_key8 asc

Please refer to Custom Filter description to learn about which fields you can use.
Note that this sorting applies additively to any other sorting that you've chosen in the configuration.")]
        [DefaultValue(null)]
        public string? LobbyAdvancedSort { get; set; } = null;

        public enum LobbyFilterByGameModeEnum
        {
            Disabled,
            Deathmatch,
            Cooperative,
            CaptureTheFlag,
            TeamDeathmatch,
        }

        public enum LobbySortByEnum
        {
            Disabled,
            PlayerCount
        }
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}