using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DnsClient;
using DnsClient.Protocol;
using GooberCord.Server.Models;
using MongoDB.Driver;

namespace GooberCord.Server;

/// <summary>
/// Various useful extensions
/// </summary>
public static class Extensions {
    /// <summary>
    /// Returns claim value
    /// </summary>
    /// <param name="claims">Claims</param>
    /// <param name="name">Type</param>
    /// <returns>Value</returns>
    public static string? Get(this IEnumerable<Claim> claims, string name)
        => claims.FirstOrDefault(x => x.Type == name)?.Value;
    
    /// <summary>
    /// Returns SHA-256 hash of the string in hex form
    /// </summary>
    /// <param name="str">String</param>
    /// <returns>SHA-256 hash</returns>
    public static string GetHash(this string str)
        => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(str)));

    /// <summary>
    /// Sends a string
    /// </summary>
    /// <param name="ws">WebSocket</param>
    /// <param name="str">String</param>
    /// <param name="token">Cancellation token</param>
    public static async Task Send(this WebSocket ws, string str, 
        CancellationToken? token = null) {
        var buf = Encoding.UTF8.GetBytes(str);
        await ws.SendAsync(
            new ArraySegment<byte>(buf),
            WebSocketMessageType.Text, true,
            token ?? CancellationToken.None);
    }

    /// <summary>
    /// Sends a string
    /// </summary>
    /// <param name="ws">WebSocket</param>
    /// <param name="obj">Any object</param>
    /// <param name="token">Cancellation token</param>
    public static async Task Send(this WebSocket ws, object obj,
        CancellationToken? token = null)
        => await ws.Send(JsonSerializer.Serialize(obj), token);

    /// <summary>
    /// Closes WebSockets connection
    /// </summary>
    /// <param name="ws">WebSocket</param>
    /// <param name="token">Cancellation token</param>
    public static async Task Close(this WebSocket ws, CancellationToken? token = null)
        => await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
            "Normal closure", token ?? CancellationToken.None);

    /// <summary>
    /// Encodes JWT token to string
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Encoded token</returns>
    public static string Encode(this JwtSecurityToken token)
        => new JwtSecurityTokenHandler().WriteToken(token);
    
    /// <summary>
    /// Checks if a string is a valid IP address
    /// </summary>
    /// <param name="ip">IP address</param>
    /// <returns>True if valid</returns>
    public static bool IsValidIp(this string ip) {
        var octets = ip.Split(":")[0].Split('.');
        return octets.Length == 4 && octets.All(x => byte.TryParse(x, out _));
    }
    
    /// <summary>
    /// Checks if a string is a valid IP:Port
    /// </summary>
    /// <param name="ip">IP:Port string</param>
    /// <returns>True if valid</returns>
    public static bool IsValidIpAndPort(this string ip) {
        var split = ip.Split(":");
        return split.Length == 2 && split[0].IsValidIp() && ushort.TryParse(split[1], out _);
    }

    /// <summary>
    /// Performs SRV and A record lookup if necessary to
    /// get Minecraft server's IP address and port
    /// </summary>
    /// <param name="server">IP or Hostname</param>
    /// <returns>IP:Port</returns>
    public static async Task<string> Lookup(this string server) {
        if (server.IsValidIpAndPort()) return server;
        if (server.IsValidIp()) return $"{server}:25565";
        var split = server.Split(":");
        if (split.Length > 2) throw new Exception();
        var port = split.Length == 2 ? ushort.Parse(split[1]) : 25565;
        var hostname = split[0];
        var client = new LookupClient();
        var result = await client.QueryAsync($"_minecraft._tcp.{hostname}", QueryType.SRV);
        if (result.Answers.Count == 0) {
            result = await client.QueryAsync(hostname, QueryType.A);
            if (result.Answers.Count == 0) throw new Exception(
                $"Failed to lookup A record for {hostname}");
            var record1 = (ARecord)result.Answers[0];
            return $"{record1.Address}:{port}";
        }
        
        var srv = (SrvRecord)result.Answers[0];
        result = await client.QueryAsync(srv.Target, QueryType.A);
        if (result.Answers.Count == 0) throw new Exception(
            $"Failed to lookup A record for {srv.Target}");
        var record = (ARecord)result.Answers[0];
        return $"{record.Address}:{srv.Port}";
    }
    
    /// <summary>
    /// Queries the first item by an expression query
    /// </summary>
    /// <param name="collection">MongoDB collection</param>
    /// <param name="query">Expression query</param>
    /// <typeparam name="T">Collection type</typeparam>
    /// <returns>First element (can be null)</returns>
    public static async Task<T?> QueryFirst<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> query) {
        var filter = new ExpressionFilterDefinition<T>(query);
        using var res = await collection.FindAsync(filter, new FindOptions<T> { BatchSize = 1 });
        await res.MoveNextAsync(); return res.Current.First();
    }
    
    /// <summary>
    /// Queries the first item by an expression query
    /// </summary>
    /// <param name="collection">MongoDB collection</param>
    /// <param name="query">Expression query</param>
    /// <typeparam name="T">Collection type</typeparam>
    /// <returns>First element (can be null)</returns>
    public static async Task<List<T>> QueryAll<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> query) {
        var filter = new ExpressionFilterDefinition<T>(query);
        using var res = await collection.FindAsync(filter);
        return res.ToList();
    }
    
    /// <summary>
    /// Queries the first item by an expression query
    /// </summary>
    /// <param name="collection">MongoDB collection</param>
    /// <param name="query">Expression query</param>
    /// <typeparam name="T">Collection type</typeparam>
    /// <returns>First element (can be null)</returns>
    public static async Task<long> Count<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> query) {
        var filter = new ExpressionFilterDefinition<T>(query);
        return await collection.CountDocumentsAsync(filter);
    }
    
    /// <summary>
    /// Deletes an item
    /// </summary>
    /// <param name="collection">MongoDB collection</param>
    /// <param name="query">Expression query</param>
    /// <typeparam name="T">Collection type</typeparam>
    public static async Task Delete<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> query) {
        var filter = new ExpressionFilterDefinition<T>(query);
        await collection.DeleteOneAsync(filter);
    }
}