using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GooberCord.Server.Attributes;
using GooberCord.Server.Database;

namespace GooberCord.Server;

public class Commands : ApplicationCommandModule {
    [SlashCommand("link", "Gives a code for linking your Minecraft account to Discord")]
    public async Task Confirm(InteractionContext ctx) {
        var link = await Link.Get(ctx.User.Id, ctx.Guild.Id);
        await ctx.CreateResponseAsync(
            $"Here is your linking code: `{link.Code}`\n" +
            $"To use it, enter `!gc link <code>` in chat\n" +
            $"If you would like to unlink later, enter `!gc unlink <code>`\n\n" +
            "**DO NOT SHARE IT WITH ANYONE!** Code is valid only for 1 hour.\n" +
            "Keep in mind that you have to link on every guild you want to use the bot in.",
            ephemeral: true);
    }
    
    [SlashCommand("unlink", "Unlinks a Minecraft account from your account")]
    public async Task Unlink(InteractionContext ctx,
        [Option("code", "Linking code")] string code) {
        if (await Link.Unlink(code, ctx.User.Id)) {
            await ctx.CreateResponseAsync(
                $"Successfully unlinked `{code}` from your account!",
                ephemeral: true);
            return;
        }
        
        await ctx.CreateResponseAsync(
            "This code either does not exist, was not used or was not created by you!");
    }
    
    [SlashCommand("links", "Lists all used linking codes from all guilds")]
    public async Task Links(InteractionContext ctx) {
        var links = await Link.GetAll(ctx.User.Id);
        if (links.Count == 0) {
            await ctx.CreateResponseAsync(
                "None if your linking codes were used!",
                ephemeral: true);
            return;
        }
        var msg = new StringBuilder();
        msg.Append("Here are your used linking codes:\n");
        foreach (var link in links)
            msg.Append($"- `{link.Code}` (UUID: `{link.Uuid}`)\n");
        await ctx.CreateResponseAsync(msg.ToString(), ephemeral: true);
    }
    
    [ManagerOnly]
    [SlashCommand("create", "Creates a new relay channel")]
    public async Task Create(InteractionContext ctx,
        [Option("channel", "Target channel")] DiscordChannel channel,
        [Option("server", "Server's IP")] string server) {
        if (channel.Guild.Id != ctx.Guild.Id) {
            await ctx.CreateResponseAsync("You can only specify channels in the current guild!", ephemeral: true);
            return;
        }
        
        var existing = await Channel.Get(channel.Id);
        if (existing != null) {
            await ctx.CreateResponseAsync($"This channel is already used for server `{existing.Server}`", ephemeral: true);
            return;
        }
        
        try {
            server = await server.Lookup();
        } catch {
            await ctx.CreateResponseAsync("Invalid IP address or hostname specified!", ephemeral: true);
            return;
        }
        
        await Controller.Channels.InsertOneAsync(new Channel {
            Id = channel.Id, GuildId = channel.Guild.Id, Server = server
        });
        await ctx.CreateResponseAsync($"Successfully set {channel.Mention} as relay channel for `{server}`");
    }
    
    [ManagerOnly]
    [SlashCommand("delete", "Deletes a relay channel")]
    public async Task Delete(InteractionContext ctx,
        [Option("channel", "Target channel")] DiscordChannel channel) {
        if (channel.Guild.Id != ctx.Guild.Id) {
            await ctx.CreateResponseAsync("You can only specify channels in the current guild!", ephemeral: true);
            return;
        }
        
        var existing = await Channel.Get(channel.Id);
        if (existing == null) {
            await ctx.CreateResponseAsync("This channel is not used as a relay!", ephemeral: true);
            return;
        }

        await Controller.Channels.Delete(x => x.Id == channel.Id);
        await ctx.CreateResponseAsync($"Successfully deleted relay channel {channel.Mention}");
    }
    
    [ManagerOnly]
    [SlashCommand("regex", "Changes chat message regex for relay channel")]
    public async Task ChangeRegex(InteractionContext ctx,
        [Option("channel", "Target channel")] DiscordChannel channel,
        [Option("regex", "Chat message regex")] string regex) {
        if (channel.Guild.Id != ctx.Guild.Id) {
            await ctx.CreateResponseAsync("You can only specify channels in the current guild!", ephemeral: true);
            return;
        }
        
        var existing = await Channel.Get(channel.Id);
        if (existing == null) {
            await ctx.CreateResponseAsync("This channel is not used as a relay!", ephemeral: true);
            return;
        }
        
        await existing.Update(x => x.Regex, regex);
        await ctx.CreateResponseAsync($"Successfully changed chat message regex for {channel.Mention} to `{regex}`");
    }
    
    [ManagerOnly]
    [SlashCommand("server", "Changes server IP for relay channel")]
    public async Task Server(InteractionContext ctx,
        [Option("channel", "Target channel")] DiscordChannel channel,
        [Option("server", "Server's IP")] string server) {
        if (channel.Guild.Id != ctx.Guild.Id) {
            await ctx.CreateResponseAsync("You can only specify channels in the current guild!", ephemeral: true);
            return;
        }
        
        var existing = await Channel.Get(channel.Id);
        if (existing == null) {
            await ctx.CreateResponseAsync("This channel is not used as a relay!", ephemeral: true);
            return;
        }
        
        try {
            server = await server.Lookup();
        } catch {
            await ctx.CreateResponseAsync("Invalid IP address or hostname specified!", ephemeral: true);
            return;
        }

        await existing.Update(x => x.Server, server);
        await ctx.CreateResponseAsync($"Successfully changed server IP for {channel.Mention} to `{server}`");
    }
}