using LegitContent.Models;
using LegitContent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LegitContent.Pages.Account;

public class LoginModel(UserService users) : PageModel
{
		[BindProperty]
		public LoginInput Input { get; set; } = new();

		[BindProperty(SupportsGet = true)]
		public string? ReturnUrl { get; set; }

		public void OnGet()
		{
		}

		public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
		{
				if (!ModelState.IsValid) return Page();

				var user = await users.VerifyAsync(Input.Email, Input.Password, cancellationToken);
				if (user is null)
				{
						ModelState.AddModelError(string.Empty, "Invalid email or password.");
						return Page();
				}

				await HttpContext.SignInUserAsync(user);
				return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl);
		}
}

public class LoginInput
{
		[Required, EmailAddress]
		public string Email { get; set; } = "";

		[Required]
		public string Password { get; set; } = "";
}
