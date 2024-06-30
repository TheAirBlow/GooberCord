using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static GooberCord.Server.Configuration;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using GooberCord.Server.Attributes;
using GooberCord.Server.Controllers;
using GooberCord.Server.Database;
using Serilog;

namespace GooberCord.Server;

/// <summary>
/// Discord bot manager
/// </summary>
public static class Discord {
    /// <summary>
    /// Discord client instance
    /// </summary>
    private static DiscordClient _client = null!;

    /// <summary>
    /// Dictionary of all online player UUIDs for channel ID
    /// </summary>
    private static readonly Dictionary<ulong, List<User>> _online = new();
    
    /// <summary>
    /// Initializes DSharpPlus
    /// </summary>
    public static async Task Initialize() {
        var factory = new LoggerFactory().AddSerilog();
        _client = new DiscordClient(new DiscordConfiguration {
            Intents = DiscordIntents.AllUnprivileged 
                      | DiscordIntents.MessageContents,
            Token = Config.Bot.Token,
            TokenType = TokenType.Bot,
            LoggerFactory = factory
        });
        
        _client.MessageCreated += async (_, e) => {
            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            DiscordMember? repliedTo = null;
            if (e.Message.ReferencedMessage! != null!) 
                repliedTo = await e.Guild.GetMemberAsync(
                    e.Message.ReferencedMessage.Author!.Id);
            await RelayMessage(member, e.Message, repliedTo);
        };
        
        _client.MessageUpdated += async (_, e) => {
            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            DiscordMember? repliedTo = null;
            if (e.Message.ReferencedMessage! != null!)
                repliedTo = await e.Guild.GetMemberAsync(
                    e.Message.ReferencedMessage.Author!.Id);
            await RelayMessage(member, e.Message, repliedTo);
        };

        var slash = _client.UseSlashCommands();
        slash.RegisterCommands<Commands>(Config.Bot.Guild);
        slash.SlashCommandErrored += SlashCommandError;
        await _client.ConnectAsync();
    }
    
    /// <summary>
    /// Handles a slash command error
    /// </summary>
    /// <param name="s">Slash commands extension</param>
    /// <param name="e">Event args</param>
    private static async Task SlashCommandError(SlashCommandsExtension s, SlashCommandErrorEventArgs e) {
        if (e.Exception is not SlashExecutionChecksFailedException ex) return;
        foreach (var check in ex.FailedChecks)
            switch (check) {
                case ManagerOnly _:
                    await e.Context.CreateResponseAsync(
                        "To use this command, you must have the `Manage Channels` permission!", true);
                    return;
            }
    }
    
    /// <summary>
    /// Relays a message
    /// </summary>
    /// <param name="author">Author</param>
    /// <param name="message">Message</param>
    /// <param name="repliedTo">Replied To</param>
    private static async Task RelayMessage(DiscordMember author, 
        DiscordMessage message, DiscordMember? repliedTo) {
        if (author.IsBot) return;
        List<User> players;
        lock (_online) {
            if (!_online.TryGetValue(message.ChannelId, out players!)) return;
        }
        
        var messageText = new StringBuilder(message.Content);
        if (message.Attachments.Count != 0)
            messageText.Append($"\n{string.Join("\n", message.Attachments.Select(i => $"<{i.FileName}>"))}");
        foreach (var user in players)
            await ChatController.Send(user.Uuid, author.DisplayName, 
                messageText.ToString(), repliedTo?.DisplayName);
    }

    /// <summary>
    /// Notify about a new player joining
    /// </summary>
    /// <param name="server">Server IP:Port</param>
    /// <param name="uuid">Player's UUID</param>
    /// <param name="username">Player's username</param>
    public static async Task PlayerJoined(string server, string uuid, string username) {
        var links = await Link.GetAll(uuid);
        foreach (var link in links) {
            var guild = await _client.GetGuildAsync(link.GuildId);
            var member = await guild.GetMemberAsync(link.UserId);
            var channels = await Channel.GetAll(link.GuildId);
            foreach (var channel in channels.Where(channel => channel.Server == server)) {
                var real = await _client.GetChannelAsync(channel.Id);
                var perms = real.PermissionsFor(member);
                if (!perms.HasPermission(Permissions.AccessChannels) || 
                    !perms.HasPermission(Permissions.SendMessages)) continue;
                
                bool notify;
                lock (_online) {
                    _online.TryAdd(channel.Id, []);
                    _online[channel.Id].Add(new User(uuid, username));
                    notify = _online[channel.Id].Count == 1;
                }

                if (notify) await real.SendMessageAsync(string.Format(
                    Config.Customization.AssignedProxy, username));
            }
        }
    }
    
    /// <summary>
    /// Notify about a new player joining
    /// </summary>
    /// <param name="server">Server IP:Port</param>
    /// <param name="uuid">Player's UUID</param>
    public static async Task PlayerLeft(string server, string uuid) {
        var links = await Link.GetAll(uuid);
        foreach (var link in links) {
            var guild = await _client.GetGuildAsync(link.GuildId);
            var member = await guild.GetMemberAsync(link.UserId);
            var channels = await Channel.GetAll(link.GuildId);
            foreach (var channel in channels.Where(channel => channel.Server == server)) {
                var real = await _client.GetChannelAsync(channel.Id);
                var perms = real.PermissionsFor(member);
                if (!perms.HasPermission(Permissions.AccessChannels) || 
                    !perms.HasPermission(Permissions.SendMessages)) continue;

                string? newProxy = null;
                var notify = false;
                lock (_online) {
                    var players = _online[channel.Id];
                    if (players[0].Uuid == uuid) {
                        if (players.Count > 1)
                            newProxy = players[1].Username;
                        notify = true;
                    }
                    
                    players.Remove(players.First(x => x.Uuid == uuid));
                    if (players.Count == 0)
                        _online.Remove(channel.Id);
                }
                
                if (!notify) continue;
                if (newProxy == null) {
                    await real.SendMessageAsync(
                        Config.Customization.NoProxy);
                    continue;
                }
                
                await real.SendMessageAsync(string.Format(
                    Config.Customization.AssignedProxy, newProxy));
            }
        }
    }

    /// <summary>
    /// Sends a global chat message if player is assigned as a proxy
    /// </summary>
    /// <param name="server">Server IP:Port</param>
    /// <param name="uuid">Player's UUID</param>
    /// <param name="message">Message content</param>
    public static async Task SendGlobal(string server, string uuid, string message) {
        var links = await Link.GetAll(uuid);
        foreach (var link in links) {
            var guild = await _client.GetGuildAsync(link.GuildId);
            var member = await guild.GetMemberAsync(link.UserId);
            var channels = await Channel.GetAll(link.GuildId);
            foreach (var channel in channels.Where(channel => channel.Server == server)) {
                var real = await _client.GetChannelAsync(channel.Id);
                var perms = real.PermissionsFor(member);
                if (!perms.HasPermission(Permissions.AccessChannels) || 
                    !perms.HasPermission(Permissions.SendMessages)) continue;

                lock (_online) {
                    if (!_online.TryGetValue(channel.Id, out var list)) continue;
                    if (list[0].Uuid != uuid) continue;
                }

                var match = Regex.Match(message, channel.Regex);
                if (!match.Success) continue;
                await real.SendMessageAsync(string.Format(Config.Customization.GlobalMessage,
                    match.Groups[1].Value, match.Groups[2].Value));
            }
        }
    }
    
    /// <summary>
    /// Sends a local chat message
    /// </summary>
    /// <param name="server">Server IP":port</param>
    /// <param name="uuid">Player's UUID</param>
    /// <param name="username">Player's Username</param>
    /// <param name="message">Message content</param>
    public static async Task SendLocal(string server, string uuid, string username, string message) {
        var links = await Link.GetAll(uuid);
        foreach (var link in links) {
            var guild = await _client.GetGuildAsync(link.GuildId);
            var member = await guild.GetMemberAsync(link.UserId);
            var channels = await Channel.GetAll(link.GuildId);
            foreach (var channel in channels.Where(channel => channel.Server == server)) {
                var real = await _client.GetChannelAsync(channel.Id);
                var perms = real.PermissionsFor(member);
                if (!perms.HasPermission(Permissions.AccessChannels) || 
                    !perms.HasPermission(Permissions.SendMessages)) continue;
                await real.SendMessageAsync(string.Format(
                    Config.Customization.LocalMessage, username, message));
            }
        }
    }

    /// <summary>
    /// User information
    /// </summary>
    private class User {
        /// <summary>
        /// Player's UUID
        /// </summary>
        public string Uuid { get; set; }
        
        /// <summary>
        /// Player's username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="uuid">UUID</param>
        /// <param name="username">Username</param>
        public User(string uuid, string username) {
            Uuid = uuid; Username = username;
        }
    }
}