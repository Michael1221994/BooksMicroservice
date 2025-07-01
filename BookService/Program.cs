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
using Microsoft.AspNetCore.Server.Kestrel.Core;

//using ReviewGrpcService;


DotNetEnv.Env.Load(); // This loads variables from .env into environment
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(); // add env

//using BookService.Services;



// Read JWT settings from .env
var jwtKey = Environment.GetEnvironmentVariable("JWT__SECRET_KEY");
var issuer = Environment.GetEnvironmentVariable("JWT__ISSUER");
var audience = Environment.GetEnvironmentVariable("JWT__AUDIENCE");


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
var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<BookDbContext>(options =>
    options.UseSqlServer(dbConnection));




builder.Services.AddScoped<BookService.Repositories.BookRepository>();

builder.Services
    .AddGrpcClient<ReviewGrpcService.ReviewGrpcServiceClient>(options =>
    {
        options.Address = new Uri("http://reviewservice:5222");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        UseCookies = false,
        //SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true },
        EnableMultipleHttp2Connections = true
    });



//for redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:Connection"] + ",abortConnect=false"
    )
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

builder.Services.AddAuthorization();


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5294, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});// gotta enable both for grpc communication


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
