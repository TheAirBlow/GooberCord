using System.Linq.Expressions;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace GooberCord.Server.Database;

/// <summary>
/// Discord linking code
/// </summary>
public class Link {
    /// <summary>
    /// Random hexadecimal code
    /// </summary>
    [BsonId]
    public string Code { get; set; }
    
    /// <summary>
    /// Discord User ID
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Discord Guild ID
    /// </summary>
    public ulong GuildId { get; set; }
    
    /// <summary>
    /// Player's UUID if linked
    /// </summary>
    public string? Uuid { get; set; }
    
    /// <summary>
    /// Expiration date, set to null if linked
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// Creates a new linking code or returns
    /// an existing one if it didn't expire
    /// </summary>
    /// <param name="user">User ID</param>
    /// <param name="guild">Guild ID</param>
    public static async Task<Link> Get(ulong user, ulong guild) {
        var link = await Controller.Links.QueryFirst(
            x => x.ExpireAt > DateTime.UtcNow && 
                 x.UserId == user && x.GuildId == guild);
        if (link != null) return link;
        link = new Link {
            ExpireAt = DateTime.UtcNow + TimeSpan.FromHours(1),
            Code = RandomNumberGenerator.GetHexString(32),
            UserId = user, GuildId = guild
        };
        await Controller.Links.InsertOneAsync(link);
        return link;
    }
    
    /// <summary>
    /// Returns all used linking codes 
    /// </summary>
    /// <param name="user">User ID</param>
    public static async Task<List<Link>> GetAll(ulong user)
        => await Controller.Links.QueryAll(
            x => x.ExpireAt == null && x.UserId == user);
    
    /// <summary>
    /// Fetches link by minecraft UUID
    /// </summary>
    /// <param name="uuid">Minecraft UUID</param>
    public static async Task<List<Link>> GetAll(string uuid)
        => await Controller.Links.QueryAll(x => x.Uuid == uuid);

    /// <summary>
    /// Confirms account linking
    /// </summary>
    /// <param name="code">Linking code</param>
    /// <param name="uuid">Minecraft UUID</param>
    /// <returns>True on success</returns>
    public static async Task<bool> Confirm(string code, string uuid) {
        var link = await Controller.Links.QueryFirst(
            x => x.ExpireAt > DateTime.UtcNow && x.Code == code);
        if (link == null) return false;
        var existing = await Controller.Links.QueryFirst(
            x => x.ExpireAt == null && x.UserId == link.UserId && x.GuildId == link.GuildId && x.Uuid == uuid);
        if (existing != null) return false;
        await link.Update(x => x.ExpireAt, null);
        await link.Update(x => x.Uuid, uuid);
        return true;
    }
    
    /// <summary>
    /// Confirms account unlinking
    /// </summary>
    /// <param name="code">Linking code</param>
    /// <param name="uuid">Minecraft UUID</param>
    /// <returns>True on success</returns>
    public static async Task<bool> Unlink(string code, string uuid) {
        var link = await Controller.Links.QueryFirst(
            x => x.ExpireAt == null && x.Code == code && x.Uuid == uuid);
        if (link == null) return false;
        await Controller.Links.Delete(x => x.Code == code);
        return true;
    }
    
    /// <summary>
    /// Confirms account unlinking
    /// </summary>
    /// <param name="code">Linking code</param>
    /// <param name="user">User ID</param>
    /// <returns>True on success</returns>
    public static async Task<bool> Unlink(string code, ulong user) {
        var link = await Controller.Links.QueryFirst(
            x => x.ExpireAt == null && x.Code == code && x.UserId == user);
        if (link == null) return false;
        await Controller.Links.Delete(x => x.Code == code);
        return true;
    }
    
    /// <summary>
    /// Updates a field
    /// </summary>
    /// <param name="field">Field</param>
    /// <param name="value">New value</param>
    /// <typeparam name="T">Field type</typeparam>
    public async Task Update<T>(Expression<Func<Link, T>> field, T value) {
        var filter = Builders<Link>.Filter.Eq(x => x.Code, Code);
        var update = Builders<Link>.Update.Set(field, value);
        await Controller.Links.UpdateOneAsync(filter, update);
    }
}