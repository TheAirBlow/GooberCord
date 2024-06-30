using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace GooberCord.Server.Database;

/// <summary>
/// Proxy discord channel
/// </summary>
public class Channel {
    /// <summary>
    /// Discord Channel ID
    /// </summary>
    [BsonId]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Discord Guild ID
    /// </summary>
    public ulong GuildId { get; set; }
    
    /// <summary>
    /// Server address in IP:Port format
    /// </summary>
    public string Server { get; set; }

    /// <summary>
    /// Regex for extracting username and message
    /// </summary>
    public string Regex { get; set; } = "<(.*)> (.*)";
    
    /// <summary>
    /// Fetches channel by channel ID
    /// </summary>
    /// <param name="id">Channel ID</param>
    /// <returns>Channel, if exists</returns>
    public static async Task<Channel?> Get(ulong id)
        => await Controller.Channels.QueryFirst(x => x.Id == id);
    
    /// <summary>
    /// Fetches all channels in a guild by ID
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>List of channels</returns>
    public static async Task<List<Channel>> GetAll(ulong id)
        => await Controller.Channels.QueryAll(x => x.GuildId == id);
    
    /// <summary>
    /// Updates a field
    /// </summary>
    /// <param name="field">Field</param>
    /// <param name="value">New value</param>
    /// <typeparam name="T">Field type</typeparam>
    public async Task Update<T>(Expression<Func<Channel, T>> field, T value) {
        var filter = Builders<Channel>.Filter.Eq(x => x.Id, Id);
        var update = Builders<Channel>.Update.Set(field, value);
        await Controller.Channels.UpdateOneAsync(filter, update);
    }
}