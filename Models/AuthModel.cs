namespace GooberCord.Server.Models;

/// <summary>
/// Authenticaiton model
/// </summary>
public class AuthModel {
    /// <summary>
    /// Encoded JWT token
    /// </summary>
    public string Token { get; set; }
    
    /// <summary>
    /// JWT token expiration date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Creates a new auth model
    /// </summary>
    /// <param name="user">JWT user</param>
    public AuthModel(JwtUser user) {
        var token = JwtHelper.GetToken(user);
        ExpiresAt = token.ValidTo;
        Token = token.Encode();
    }
}