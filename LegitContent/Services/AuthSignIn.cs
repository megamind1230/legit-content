using LegitContent.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace LegitContent.Services;

public static class AuthSignIn
{
		public static async Task SignInUserAsync(this HttpContext httpContext, User user)
		{
				var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id),
			new(ClaimTypes.Name, user.Email),
		};
				if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

				var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				await httpContext.SignInAsync(new ClaimsPrincipal(identity));
		}
}
