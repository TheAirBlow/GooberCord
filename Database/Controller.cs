using MongoDB.Driver;

namespace GooberCord.Server.Database; 

/// <summary>
/// Database controller
/// </summary>
public static class Controller {
    public static readonly IMongoCollection<Channel> Channels;
    public static readonly IMongoCollection<Link> Links;
    
    /// <summary>
    /// Initializes the MongoDB database
    /// </summary>
    static Controller() {
        var client = new MongoClient(new MongoClientSettings {
            Server = new MongoServerAddress("localhost"),
            MaxConnectionPoolSize = 500
        });
        var database = client.GetDatabase("goobercord");
        Channels = database.GetCollection<Channel>("channels");
        Links = database.GetCollection<Link>("links");
    }
}