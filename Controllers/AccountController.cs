using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using GooberCord.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GooberCord.Server.Controllers;

/// <summary>
/// Manages account authentication
/// </summary>
[ApiController] [Route("auth")]
public class AccountController : Controller {
    /// <summary>
    /// Static HTTP client instance
    /// </summary>
    private static readonly HttpClient _client = new();
    
    /// <summary>
    /// Initiates minecraft authentication.
    /// </summary>
    /// <remarks>
    /// Client must send authentication request to Mojang
    /// <see href="https://wiki.vg/Protocol_Encryption#Client">as specified here</see>
    /// with the SHA-1 hash of the received JWT token as serverId.
    /// </remarks>
    /// <param name="username">Username</param>
    /// <returns>JWT token</returns>
    [HttpPost("begin")]
    [ProducesResponseType(typeof(AuthModel), 200)]
    public ActionResult Begin([FromForm] string username) {
        var user = new JwtUser(username);
        return Json(new AuthModel(user));
    }
    
    /// <summary>
    /// Completes minecraft authentication.
    /// </summary>
    /// <remarks>
    /// Returns a new JWT token on success which can be used to access all APIs.
    /// The token will be valid for only a single day, so it's recommended to
    /// get a new one each time Minecraft is launched.
    /// </remarks>
    /// <returns>JWT token</returns>
    [HttpPost("verify")] 
    [Authorize("begin")]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(AuthModel), 200)]
    public async Task<IActionResult> Token() {
        var user = new JwtUser(User.Claims);
        var hash = Request.Headers.Authorization.ToString().Replace("Bearer ", "").GetHash();
        var resp = await _client.GetAsync(
            $"https://sessionserver.mojang.com/session/minecraft/hasJoined?username={user.Username}&serverId={hash}");
        if (resp.StatusCode != HttpStatusCode.OK) return Forbid();
        var str = await resp.Content.ReadAsStringAsync();
        var info = JsonSerializer.Deserialize<PlayerInfo>(str);
        if (info?.Username != user.Username) return Forbid();
        user.Uuid = info.Uuid;
        return Ok(new AuthModel(user));
    }

    /// <summary>
    /// Player info JSON
    /// </summary>
    private class PlayerInfo {
        [JsonPropertyName("id")]
        public string? Uuid { get; set; }
        
        [JsonPropertyName("name")]
        public string? Username { get; set; }
    }
}