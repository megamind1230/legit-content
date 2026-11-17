using LegitContent.Models;
using LegitContent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LegitContent.Pages.Account;

public class RegisterModel(UserService users) : PageModel
{
		[BindProperty]
		public RegisterInput Input { get; set; } = new();

		[BindProperty(SupportsGet = true)]
		public string? ReturnUrl { get; set; }

		public void OnGet()
		{
		}

		public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
		{
				if (!ModelState.IsValid) return Page();

				var result = await users.RegisterAsync(Input.Email, Input.Password, cancellationToken);
				if (!result.Success)
				{
						ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the account.");
						return Page();
				}

				var user = result.User!;
				await HttpContext.SignInUserAsync(user);
				return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl);
		}
}

public class RegisterInput
{
		[Required, EmailAddress]
		public string Email { get; set; } = "";

		[Required, MinLength(8)]
		public string Password { get; set; } = "";
}
