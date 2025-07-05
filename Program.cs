using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Models;
using MoviesAPI.Services;
using MoviesAPI.TestEntities;
using MoviesAPI.Utilities;
using NetTopologySuite;
using NetTopologySuite.Geometries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
    {
      var geometryFactory = provider.GetRequiredService<GeometryFactory>();
      cfg.AddProfile(new AutoMapperProfiles(geometryFactory));
    }).CreateMapper());

builder.Services.AddSingleton<IRepository, RepositorySqlServer>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("name=DefaultConnection", sqlServer =>
        sqlServer.UseNetTopologySuite()
    )
);

builder.Services.AddSingleton<GeometryFactory>(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));
builder.Services.AddTransient<IStorageFiles, StorageArchivesLocal>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(15);
});

var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")!.Split(",");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(optionsCORS =>
    {
        optionsCORS.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()
        .WithExposedHeaders("total-records-quantity");
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("Database: " + context.Database.GetDbConnection().Database);
    Console.WriteLine("Data Source: " + context.Database.GetDbConnection().DataSource);
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors();

app.UseOutputCache();

app.UseAuthorization();

app.MapControllers();

app.Run();
