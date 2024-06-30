using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GooberCord.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace GooberCord.Server.Controllers;

/// <summary>
/// Manages chat messages
/// </summary>
[ApiController] [Route("chat")]
public class ChatController : Controller {
    /// <summary>
    /// Dictionary of all sessions of a player by UUID
    /// </summary>
    private static readonly Dictionary<string, List<Session>> _players = new();
    
    /// <summary>
    /// Initiates a WebSockets connection.
    /// </summary>
    /// <remarks>
    /// Server-bound messages:<br/>
    /// 1) Join server: <code>{"type":2,"args":["IP:Port"]}</code><br/>
    /// 2) Leave server: <code>{"type":3,"args":[]}</code><br/>
    /// 3) Global message: <code>{"type":4,"args":["&lt;username&gt; message"]}</code><br/>
    /// 4) Local message: <code>{"type":5,"args":["message"]}</code><br/><br/>
    /// Client-bound messages:<br/>
    /// 1) Error: <code>{"type":0,"args":["message"]}</code><br/>
    /// 2) Acknowledgement: <code>{"type":1,"args":[]}</code><br/>
    /// 3) Local message: <code>{"type":5,"args":["username","message","replying to"]}</code>
    /// </remarks>
    /// <response code="400">Not a websockets request</response>
    /// <response code="101">Switching to WebSockets</response>
    /// <returns>WebSockets connection</returns>
    [HttpGet("ws")] [Authorize]
    [ProducesResponseType(400)]
    [ProducesResponseType(101)]
    public async Task WebSockets() {
        if (!HttpContext.WebSockets.IsWebSocketRequest) {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var user = new JwtUser(User.Claims);
        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var session = new Session(user, ws);

        try {
            await HandleClient(session);
        } catch { /* Ignore */ }

        await session.Close();
    }

    /// <summary>
    /// Handles a WebSockets client
    /// </summary>
    /// <param name="session">Session</param>
    private async Task HandleClient(Session session) {
        while (session.Socket.State == WebSocketState.Open) {
            using var memory = new MemoryStream();
            WebSocketReceiveResult result;
            do {
                var messageBuffer = WebSocket.CreateClientBuffer(1024, 16);
                result = await session.Socket.ReceiveAsync(messageBuffer, session.Token.Token);
                memory.Write(messageBuffer.Array!, messageBuffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text) {
                await session.Send(ChatModel.MessageType.Error, "Only text messages are allowed");
                continue;
            }

            ChatModel? json;
            memory.Position = 0;
            try {
                json = JsonSerializer.Deserialize<ChatModel>(memory);
                if (json == null) throw new Exception("Failed to deserialize");
            } catch {
                await session.Send(ChatModel.MessageType.Error, "Failed to deserialize message");
                continue;
            }

            new Thread(async () => {
                try {
                    await HandleMessage(session, json);
                } catch (Exception e) {
                    await session.Send(ChatModel.MessageType.Error, "Internal error");
                    Log.Error("Failed to handle message: {0}", e);
                }
            }).Start();
        }
    }

    /// <summary>
    /// Handles a WebSockets message
    /// </summary>
    /// <param name="session">Session</param>
    /// <param name="json">Message JSON</param>
    private async Task HandleMessage(Session session, ChatModel json) {
        switch (json.Type) {
            case ChatModel.MessageType.Join:
                if (json.Arguments.Length != 1) {
                    await session.Send(ChatModel.MessageType.Error, "Missing required arguments");
                    return;
                }

                if (!json.Arguments[0].IsValidIpAndPort()) {
                    await session.Send(ChatModel.MessageType.Error, "Invalid IP:Port specified");
                    return;
                }

                if (session.Server != null) {
                    await session.Send(ChatModel.MessageType.Error, "Leave current server first");
                    return;
                }

                Session? existing;
                lock (_players)
                    existing = _players[session.Uuid].FirstOrDefault(x => x.Server == json.Arguments[0]);

                try {
                    if (existing != null) {
                        await existing.Send(ChatModel.MessageType.Error, "Session is no longer valid");
                        await existing.Token.CancelAsync();
                    }
                } catch { /* Ignore */ }
                
                session.Server = json.Arguments[0];
                await Discord.PlayerJoined(session.Server, session.Uuid, session.Username);
                break;
            case ChatModel.MessageType.Leave:
                if (session.Server == null) {
                    await session.Send(ChatModel.MessageType.Error, "Join a server first");
                    return;
                }
                
                await Discord.PlayerLeft(session.Server, session.Uuid);
                session.Server = null;
                break;
            case ChatModel.MessageType.Global:
                if (json.Arguments.Length != 1) {
                    await session.Send(ChatModel.MessageType.Error, "Missing required arguments");
                    return;
                }
                
                if (session.Server == null) {
                    await session.Send(ChatModel.MessageType.Error, "Join a server first");
                    return;
                }
                
                await Discord.SendGlobal(session.Server, session.Uuid, json.Arguments[0]);
                break;
            case ChatModel.MessageType.Local:
                if (json.Arguments.Length != 1) {
                    await session.Send(ChatModel.MessageType.Error, "Missing required arguments");
                    return;
                }
                
                if (session.Server == null) {
                    await session.Send(ChatModel.MessageType.Error, "Join a server first");
                    return;
                }
                
                await Discord.SendLocal(session.Server, session.Uuid, session.Username, json.Arguments[0]);
                break;
            default:
                await session.Send(ChatModel.MessageType.Error, "Specified type not supported");
                return;
        }
        
        await session.Send(ChatModel.MessageType.Ack);
    }

    /// <summary>
    /// Sends chat message to a player by UUID
    /// </summary>
    /// <param name="uuid">Target player's UUID</param>
    /// <param name="username">Username</param>
    /// <param name="message">Message</param>
    /// <param name="replyingTo">Replying to</param>
    public static async Task Send(string uuid, string username, string message, string? replyingTo) {
        var args = new List<string> { username, message };
        if (replyingTo != null) args.Add(replyingTo);
        var arr = args.ToArray();
        IEnumerable<Task> tasks;
        lock (_players) {
            if (!_players.TryGetValue(uuid, out var players)) return;
            tasks = players.Select(x => x.Send(ChatModel.MessageType.Local, arr));
        }

        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// A WebSocket session
    /// </summary>
    private class Session {
        /// <summary>
        /// Termination cancellation token
        /// </summary>
        public CancellationTokenSource Token { get; set; }
        
        /// <summary>
        /// Player's UUID
        /// </summary>
        public string Uuid { get; set; }
        
        /// <summary>
        /// Player's username
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// WebSocket instance
        /// </summary>
        public WebSocket Socket { get; set; }
        
        /// <summary>
        /// Current server in IP:Port format
        /// </summary>
        public string? Server { get; set; }

        /// <summary>
        /// Sends a JSON serialized object
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="args">Arguments</param>
        public async Task Send(ChatModel.MessageType type, params string[] args) {
            var token = new CancellationTokenSource();
            var task = Socket.Send(new ChatModel(type, args), token.Token);
            if (await Task.WhenAny(task, Task.Delay(10000, Token.Token)) == task) {
                await task;
                return;
            }

            await token.CancelAsync();
        }
        
        /// <summary>
        /// Closes current session
        /// </summary>
        public async Task Close() {
            lock (_players) {
                var list = _players[Uuid];
                list.Remove(this);
                if (list.Count == 0)
                    _players.Remove(Uuid);
            }
            
            if (Server != null)
                await Discord.PlayerLeft(Server, Uuid);
            
            if (Socket.State == WebSocketState.Open)
                try {
                    var token = new CancellationTokenSource();
                    var task = Socket.Close(token.Token);
                    if (await Task.WhenAny(task, Task.Delay(5000, Token.Token)) == task) {
                        await task;
                        return;
                    }

                    await token.CancelAsync();
                } catch { /* Ignore */ }
        }

        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="user">JWT user</param>
        /// <param name="socket">WebSocket</param>
        public Session(JwtUser user, WebSocket socket) {
            Token = new CancellationTokenSource();
            Uuid = user.Uuid!; Socket = socket;
            Username = user.Username;
            lock (_players) {
                if (!_players.ContainsKey(Uuid))
                    _players.Add(Uuid, []);
                _players[Uuid].Add(this);
            }
        }
    }
}