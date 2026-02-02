using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureApi;
using SecureApi.Configuration;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Resources;
using SecureApi.Services;
using SecureApi.Services.Interfaces;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("ENV = " + builder.Environment.EnvironmentName);
Console.WriteLine("CS = " + builder.Configuration.GetConnectionString("Default"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>()  // ✅ Required
.AddDefaultTokenProviders();
// ===== Swagger =====
builder.Services.AddSwaggerGen();
// ===== Localization =====
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllers()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        {
            // Specify the shared resource type for DataAnnotations
            var assemblyName = new System.Reflection.AssemblyName(typeof(CommonResources).Assembly.FullName!);
            return factory.Create("CommonResources", assemblyName.Name!);

        };
    });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {

        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
    };
});

// ===== Services Registration =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategroyService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBasketService, BasketService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "myapp-redis";
});
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

    //options.AddPolicy("ProdCors", policy =>
    //{
    //    policy.WithOrigins(builder.Configuration["Frontend:FrontendUrl"])
    //          .AllowAnyMethod()
    //          .AllowAnyHeader()
    //          .AllowCredentials();
    //});
});

// ===== Request Localization =====
var supportedCultures = new[] { "en", "ar" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// ===== JWT, Identity, DbContext, AutoMapper, Services etc =====
// (Keep your existing configuration here)

var app = builder.Build();
// ===== APPLY MIGRATIONS AUTOMATICALLY =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Checking database connection...");

        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("Database connection OK — using existing database");
        }
        else
        {
            logger.LogWarning("Cannot connect to database — make sure it is restored");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection check failed");
        // do not throw — allow app to start
    }
}

// ===== Development Tools =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}
else
{
    // app.UseCors("ProdCors");
    app.UseHsts();
}

//app.UseHttpsRedirection();

// ===== Request Localization (early) =====
var locOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

// ===== Static Files =====
app.UseDefaultFiles();// Important: finds index.html automatically
app.UseStaticFiles(); // Serves files from wwwroot
var imgPath=Path.Combine(Directory.GetCurrentDirectory(), "Img");
Console.WriteLine("Serving static images from: " + imgPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Img")),
    RequestPath = "/StaticImages"
});

// ===== Authentication / Authorization =====
app.UseAuthentication();
app.UseAuthorization();

// ===== IMPORTANT: Map Controllers BEFORE Fallback =====
app.MapControllers();

// ===== SPA Fallback (LAST - only for unmatched client routes) =====
app.MapFallbackToFile("index.html");

app.Run();