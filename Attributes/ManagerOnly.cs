using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace GooberCord.Server.Attributes;

/// <summary>
/// Checks that the user has manage channels permission
/// </summary>
public class ManagerOnly : SlashCheckBaseAttribute {
    /// <summary>
    /// Executes the check
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>Boolean</returns>
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(ctx.Member.Permissions.HasPermission(Permissions.ManageChannels));
}