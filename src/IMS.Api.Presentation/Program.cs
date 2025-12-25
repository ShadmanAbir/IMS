using IMS.Api.Application;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure DB connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IMS API", Version = "v1" });
});

// Application & Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connectionString);

// Configure EF Core with Npgsql if connection string provided
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });

    // Register IDbConnectionFactory for Dapper - AddInfrastructure will also register if provided
    // so keep for backward compatibility
    builder.Services.AddSingleton<IDbConnectionFactory>(sp => new PostgreSqlConnectionFactory(connectionString));
}

var app = builder.Build();

// Apply migrations at startup in Development / Testing
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
    if (db != null && (env.IsDevelopment() || env.EnvironmentName == "Testing"))
    {
        db.Database.Migrate();

        // Seed roles etc
        var seedServiceType = typeof(IMS.Api.Infrastructure.Services.DataSeedingService);
        var method = seedServiceType.GetMethod("SeedAsync");
        if (method != null)
        {
            await (Task)method.Invoke(null, new object[] { scope.ServiceProvider });
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();