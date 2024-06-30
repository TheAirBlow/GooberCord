using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace GooberCord.Server;

/// <summary>
/// Global configuration
/// </summary>
public class Configuration {
    /// <summary>
    /// JSON serializer options
    /// </summary>
    public static readonly JsonSerializerOptions Options = new() {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true, IncludeFields = true
    };
    
    /// <summary>
    /// Static object instance
    /// </summary>
    public static Configuration Config;

    /// <summary>
    /// Load the configuration
    /// </summary>
    static Configuration() {
        if (File.Exists("config.json")) {
            Log.Information("Loading configuration file...");
            var content = File.ReadAllText("config.json");
            try {
                Config = JsonSerializer.Deserialize<Configuration>(content, Options)!;
            } catch (Exception e) {
                Log.Fatal("Failed to load config: {0}", e);
                Environment.Exit(-1);
            }
            return;
        }

        Config = new Configuration(); Config.Save();
        Log.Fatal("Configuration file doesn't exist, created a new one!");
        Log.Fatal("Please fill it with all the necessary information.");
        Environment.Exit(-1);
    }

    /// <summary>
    /// Discord bot configuration
    /// </summary>
    public class BotClass {
        /// <summary>
        /// Discord bot token
        /// </summary>
        public string Token { get; set; } = "change_me";
    
        /// <summary>
        /// Guild to register commands in
        /// </summary>
        public ulong? Guild { get; set; }
    }
    
    /// <summary>
    /// Various customization options
    /// </summary>
    public class CustomizationClass {
        /// <summary>
        /// Format for a global chat message
        /// </summary>
        public string GlobalMessage { get; set; } = "*{0}*: {1}";
    
        /// <summary>
        /// Format for a discord-only chat message
        /// </summary>
        public string LocalMessage { get; set; } = "<:zpmvibrator:1171802059343921173> *{0}*: {1}";

        /// <summary>
        /// Notification sent when a new proxy player is assigned
        /// </summary>
        public string AssignedProxy { get; set; } = "<:obama:1138432261411324025> *{0}* is now proxying chat";
        
        /// <summary>
        /// Notification sent on failure to assign a new proxy player
        /// </summary>
        public string NoProxy { get; set; } = "<:TrollShrug:1256731287310569563> No players left to proxy chat";
    }
    
    /// <summary>
    /// Secret string used for authentication
    /// </summary>
    public string AuthSecret { get; init; } = RandomNumberGenerator.GetHexString(32);

    /// <summary>
    /// Various customization options
    /// </summary>
    public CustomizationClass Customization { get; set; } = new();
    
    /// <summary>
    /// Discord bot configuration
    /// </summary>
    public BotClass Bot { get; set; } = new();
    
    /// <summary>
    /// Save configuration changes
    /// </summary>
    public void Save() => File.WriteAllText("config.json", 
        JsonSerializer.Serialize(Config, Options));
}