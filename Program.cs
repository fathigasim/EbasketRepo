using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ===== Localization =====
builder.Services.AddLocalization(options => { options.ResourcesPath = "Resources"; });

builder.Services.AddControllers()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("ProdCors", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:FrontendUrl"])
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== JWT, Identity, DbContext, AutoMapper, Services etc =====
// (Keep your existing configuration here)

// ===== Request Localization =====
var supportedCultures = new[] { "en", "ar" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
    options.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
});

var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}
else
{
    app.UseCors("ProdCors");
    app.UseHsts();
    // app.UseMiddleware<GlobalExceptionMiddleware>(); // optional
}

app.UseHttpsRedirection();

// ===== Serve API static files =====


// ===== Serve React SPA from wwwroot/frontend =====
var frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "frontend");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(frontendPath),
    RequestPath = "" // Serve from root URL
});

// ===== SPA fallback for React routing =====
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(frontendPath)
});

// ===== Request Localization =====
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
