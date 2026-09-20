using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace Mesh.Api.Auth;

public static class CurrentUserExtensions
{
    /// <summary>Reads the authenticated user's Id from the JWT 'sub' claim.
    /// Every module's controller needs this same lookup to scope queries
    /// to the current user, so it lives here once rather than repeated.</summary>
    public static string GetUserId(this ControllerBase controller) =>
        controller.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new InvalidOperationException("No authenticated user on this request.");
}
