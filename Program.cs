using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

builder.Services.Configure<DefaultAdminSettings>(builder.Configuration.GetSection("DefaultAdmin"));

builder.Services.AddHostedService<DbInitializer>();

builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(connectionString));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<ScheduledLinkService>();
builder.Services.AddHostedService<HangfireInitializer>();


var authenticationBuilder = builder.Services.AddAuthentication();

if (builder.Configuration.GetValue<bool>("ExternalLogins:Google:Use"))
{
    authenticationBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["ExternalLogins:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["ExternalLogins:Google:ClientSecret"];
    });
}

// Add Microsoft configuration here, chaining off the same builder.
if (builder.Configuration.GetValue<bool>("ExternalLogins:Microsoft:Use"))
{
    authenticationBuilder.AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = builder.Configuration["ExternalLogins:Microsoft:ClientId"];
        microsoftOptions.ClientSecret = builder.Configuration["ExternalLogins:Microsoft:ClientSecret"];
    });
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

var useHttps = app.Configuration.GetValue<bool>("ServerOptions:UseHttps");
if (useHttps)
{
    // The default HSTS value is 30 days. This should only be active if HTTPS is intended.
    app.UseHsts();

    // Only redirect to HTTPS if the user has configured it and intends to use it.
    app.UseHttpsRedirection();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
