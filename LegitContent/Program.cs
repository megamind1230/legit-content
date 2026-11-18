using LegitContent.Data;
using LegitContent.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var logDirectory = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
	"magnus", "legit-content", "logs");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
	.Enrich.FromLogContext()
	.MinimumLevel.Information()
	.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
	.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
	.WriteTo.Console()
	.WriteTo.File(
		Path.Combine(logDirectory, "legit-content-.log"),
		rollingInterval: RollingInterval.Day,
		retainedFileCountLimit: 14));

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddScoped<UserLinkStateService>();
builder.Services.AddScoped<UserService>();
builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
			options.LoginPath = "/Account/Login";
			options.ExpireTimeSpan = TimeSpan.FromDays(30);
			options.SlidingExpiration = true;
	});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
		app.UseExceptionHandler("/Error");
		app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
	 .WithStaticAssets();

app.MapGet("/tags", async (string? q, TagService tags, CancellationToken cancellationToken) =>
	Results.Json(await tags.SearchAsync(q ?? "", cancellationToken)));

await using (var scope = app.Services.CreateAsyncScope())
{
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();
		await DbInitializer.SeedAsync(db, builder.Configuration["Admin:Email"], builder.Configuration["Admin:Password"]);
}

app.Run();
