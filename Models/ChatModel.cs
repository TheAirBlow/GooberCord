using System.Text.Json.Serialization;

namespace GooberCord.Server.Models;

/// <summary>
/// Chat WebSockets model
/// </summary>
public class ChatModel {
    /// <summary>
    /// Message type
    /// </summary>
    public enum MessageType {
        /// <summary>
        /// Notifies client of an issue with last message.
        /// Server arguments: error message.
        /// </summary>
        Error = 0,
        
        /// <summary>
        /// Server acknowledged sent message.
        /// Server arguments: none.
        /// </summary>
        Ack = 1,
        
        /// <summary>
        /// Announces joining multiplayer to server.
        /// Client arguments: IP address + port.
        /// </summary>
        Join = 2,
        
        /// <summary>
        /// Announces leaving multiplayer to server.
        /// Client arguments: none.
        /// </summary>
        Leave = 3,
        
        /// <summary>
        /// Notifies server about a global chat message.
        /// Client arguments: message.
        /// </summary>
        Global = 4,
        
        /// <summary>
        /// Notifies about discord-only chat message.
        /// Server arguments: username, message, replyingTo.
        /// Client arguments: message.
        /// </summary>
        Local = 5
    }

    /// <summary>
    /// Creates a new chat WebSockets message
    /// </summary>
    /// <param name="type">Message type</param>
    /// <param name="args">Arguments</param>
    public ChatModel(MessageType type, params string[] args) {
        Type = type; Arguments = args;
    }
    
    /// <summary>
    /// Empty constructor for deserialization
    /// </summary>
    public ChatModel() {}
    
    /// <summary>
    /// Message type
    /// </summary>
    [JsonPropertyName("type")]
    public MessageType Type { get; init; }
    
    /// <summary>
    /// String arguments
    /// </summary>
    [JsonPropertyName("args")]
    public string[] Arguments { get; init; }
}