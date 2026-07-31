using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace EfbisMuhasebe.Web.Extensions;

/// <summary>
/// Super Admin Impersonation & Claims extension helper methods.
/// </summary>
public static class ClaimsExtensions
{
    public const string ImpersonatedTenantIdClaim = "ImpersonatedTenantId";
    public const string ImpersonatedTenantNameClaim = "ImpersonatedTenantName";

    /// <summary>
    /// Updates authentication cookie to include ImpersonatedTenantId claim and sets active session.
    /// </summary>
    public static async Task ImpersonateTenantAsync(this HttpContext httpContext, int tenantId, string tenantName)
    {
        if (httpContext.User.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return;

        // Remove existing impersonation claims if present
        RemoveClaimIfExists(identity, ImpersonatedTenantIdClaim);
        RemoveClaimIfExists(identity, ImpersonatedTenantNameClaim);

        // Add new impersonation claims
        identity.AddClaim(new Claim(ImpersonatedTenantIdClaim, tenantId.ToString()));
        identity.AddClaim(new Claim(ImpersonatedTenantNameClaim, tenantName));

        // Update cookie authentication ticket
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        // Synchronize ActiveTenantId session state for multi-tenant EF Core query filters
        httpContext.Session.SetInt32("ActiveTenantId", tenantId);
    }

    /// <summary>
    /// Removes ImpersonatedTenantId claim from cookie and resets active tenant session to global view (0).
    /// </summary>
    public static async Task StopImpersonatingAsync(this HttpContext httpContext)
    {
        if (httpContext.User.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return;

        RemoveClaimIfExists(identity, ImpersonatedTenantIdClaim);
        RemoveClaimIfExists(identity, ImpersonatedTenantNameClaim);

        // Re-issue cookie authentication ticket without impersonation claims
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        // Reset active session to 0 (Global Platform View)
        httpContext.Session.SetInt32("ActiveTenantId", 0);
    }

    private static void RemoveClaimIfExists(ClaimsIdentity identity, string claimType)
    {
        var existingClaim = identity.FindFirst(claimType);
        if (existingClaim != null)
        {
            identity.RemoveClaim(existingClaim);
        }
    }
}
