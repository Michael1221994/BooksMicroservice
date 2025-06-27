using StackExchange.Redis;
using BookService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using BookService.Configuration;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen; // SwaggerGenOptions
using Grpc.Net.ClientFactory;
using ReviewService.Grpc;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Grpc.Net.Client;
using Grpc.Core;
//using ReviewGrpcService;


DotNetEnv.Env.Load(); // This loads variables from .env into environment


//using BookService.Services;


var builder = WebApplication.CreateBuilder(args);

// Read JWT settings from .env
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen();
//builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();


//for the DBContext
builder.Services.AddDbContext<BookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddScoped<BookService.Repositories.BookRepository>();

builder.Services.AddGrpcClient<ReviewGrpcService.ReviewGrpcServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5212");
})
.ConfigureChannel(options =>
{
    options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
});

//for redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"])

    
);
builder.Services.AddSingleton<BookService.Services.RedisCacheService>();

// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5294);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Book API {description.GroupName.ToUpperInvariant()}");
        }
    });
}



/*app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();*/
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/*record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}*/
