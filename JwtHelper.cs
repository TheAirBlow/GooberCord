using static GooberCord.Server.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GooberCord.Server;

/// <summary>
/// JSON web token helper
/// </summary>
public static class JwtHelper {
    /// <summary>
    /// JWT issuer name
    /// </summary>
    public const string Issuer = "GooberCord";
    
    /// <summary>
    /// JWT audience name
    /// </summary>
    public const string Audience = "MinecraftClient";

    /// <summary>
    /// JWT credentials
    /// </summary>
    public static readonly SigningCredentials Credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.AuthSecret)),
        SecurityAlgorithms.HmacSha256Signature);

    /// <summary>
    /// Generates a JWT token from a JWT user
    /// </summary>
    /// <param name="user">JWT user</param>
    /// <returns>JWT token</returns>
    public static JwtSecurityToken GetToken(JwtUser user) {
        var createdAt = DateTime.UtcNow;
        var expireAt = user.Uuid != null
            ? createdAt.AddDays(1)
            : createdAt.AddMinutes(5);
        return new JwtSecurityToken(Issuer, Audience, 
            user.GetClaims(), createdAt, expireAt, Credentials);
    }
}

/// <summary>
/// User object used in the JWT
/// </summary>
public class JwtUser {
    /// <summary>
    /// Player's UUID
    /// </summary>
    public string? Uuid { get; set; }
    
    /// <summary>
    /// Player's username
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Returns list of claims
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Claim> GetClaims() {
        var claims = new List<Claim> {
            new("name", Username)
        };
        
        if (Uuid != null)
            claims.Add(new Claim("uuid", Uuid));
        
        return claims;
    }

    /// <summary>
    /// Creates a new JWT user
    /// </summary>
    /// <param name="uuid">UUID</param>
    /// <param name="username">Username</param>
    public JwtUser(string username, string? uuid = null) {
        Uuid = uuid; Username = username;
    }

    /// <summary>
    /// Creates a new JWT user
    /// </summary>
    /// <param name="claims">Claims</param>
    public JwtUser(IEnumerable<Claim> claims) {
        Username = claims.Get("name")!;
        Uuid = claims.Get("uuid");
    }
}