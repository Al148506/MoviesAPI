using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoviesAPI.Models;
using MoviesAPI.Services;
using MoviesAPI.TestEntities;
using MoviesAPI.Utilities;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.HPRtree;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========================
// Kestrel (Azure PORT)
// ========================
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// ========================
// Controllers & Swagger
// ========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================
// AutoMapper
// ========================
builder.Services.AddSingleton(provider =>
{
    var geometryFactory = provider.GetRequiredService<GeometryFactory>();
    return new MapperConfiguration(cfg =>
    {
        cfg.AddProfile(new AutoMapperProfiles(geometryFactory));
    }).CreateMapper();
});

// ========================
// Database
// ========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        "name=DefaultConnection",
        sqlServer => sqlServer.UseNetTopologySuite()
    )
);

// ========================
// Services
// ========================
builder.Services.AddSingleton<IRepository, RepositorySqlServer>();
builder.Services.AddTransient<IStorageFiles, StorageArchivesAzure>();
builder.Services.AddTransient<IUserServices, UserServices>();

builder.Services.AddSingleton<GeometryFactory>(
    NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326)
);

builder.Services.AddHttpContextAccessor();

// ========================
// Output Cache
// ========================
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(15);
});

// ========================
// Identity
// ========================
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<IdentityUser>>();
builder.Services.AddScoped<SignInManager<IdentityUser>>();

// ========================
// Authentication (JWT)
// ========================
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["keyjwt"]!)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

// ========================
// Authorization
// ========================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("isadmin", policy => policy.RequireClaim("isadmin"));
});

// ========================
// CORS (🔥 CLAVE 🔥)
// ========================
var allowedOrigins = builder.Configuration
    .GetValue<string>("AllowedOrigins")!
    .Split(",", StringSplitOptions.RemoveEmptyEntries);

if(allowedOrigins.Length == 0)
{
    System.Diagnostics.Trace.TraceInformation("Error al leer la variable de AllowedOrigins");
}
else
{
    System.Diagnostics.Trace.TraceInformation("AllowedOrigins:");
    foreach (var origin in allowedOrigins)
    {
       System.Diagnostics.Trace.TraceInformation($" - {origin}");
    }
}

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("total-records-quantity");
        });
    });

var app = builder.Build();

// ========================
// Middleware Pipeline
// ========================
app.UseSwagger();
app.UseSwaggerUI();

// Debug DB info (opcional)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("Database: " + context.Database.GetDbConnection().Database);
    Console.WriteLine("Data Source: " + context.Database.GetDbConnection().DataSource);
}

app.UseStaticFiles();

app.UseHttpsRedirection();

// 🔥 CORS ANTES de Auth
app.UseCors("AllowAngular");

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});


app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();

app.Run();
