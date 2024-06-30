using GooberCord.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace GooberCord.Server.Controllers;

/// <summary>
/// Manages discord linking
/// </summary>
[ApiController] [Route("discord")]
public class DiscordController : Controller {
    /// <summary>
    /// Links minecraft to discord with a linking code.
    /// </summary>
    /// <response code="400">Code was used or does not exist</response>
    /// <response code="200">Account successfully linked</response>
    /// <returns>JWT token</returns>
    [HttpPut("link/{code}")] [Authorize]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Confirm([FromRoute] string code) {
        var user = new JwtUser(User.Claims);
        if (await Link.Confirm(code, user.Uuid!))
            return Ok();
        return BadRequest();
    }

    /// <summary>
    /// Unlinks minecraft from discord with a linking code.
    /// </summary>
    /// <response code="400">Code was not used or does not exist</response>
    /// <response code="200">Account successfully unlinked</response>
    /// <returns>JWT token</returns>
    [HttpDelete("link/{code}")] [Authorize]
    [ProducesResponseType(400)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Unlink([FromRoute] string code) {
        var user = new JwtUser(User.Claims);
        if (await Link.Unlink(code, user.Uuid!))
            return Ok();
        return BadRequest();
    }
    
    /// <summary>
    /// Returns all linking codes used by current account
    /// </summary>
    /// <returns>List of all linking codes</returns>
    [HttpGet("links")] [Authorize]
    [ProducesResponseType(typeof(List<Link>), 200)]
    public async Task<IActionResult> Links() {
        var user = new JwtUser(User.Claims);
        return Json(await Link.GetAll(user.Uuid!));
    }
}