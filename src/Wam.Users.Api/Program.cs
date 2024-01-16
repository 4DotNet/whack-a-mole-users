using HexMaster.RedisCache;
using Wam.Api.Infrastructure;
using Wam.Api.Infrastructure.Swagger;
using Wam.Core.ExtensionMethods;
using Wam.Core.Identity;
using Wam.Users.Repositories;
using Wam.Users.Services;

var corsPolicyName = "DefaultCors";
var builder = WebApplication.CreateBuilder(args);

var azureCredential = CloudIdentity.GetCloudIdentity();
try
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(builder.Configuration.GetRequiredValue("AzureAppConfiguration")), azureCredential)
            .ConfigureKeyVault(kv => kv.SetCredential(azureCredential))
            .UseFeatureFlags();
    });
}
catch (Exception ex)
{
    throw new Exception("Configuration failed", ex);
}
// Add services to the container.

builder.Services.AddHexMasterCache(builder.Configuration);

builder.Services.AddTransient<IUsersService, UsersService>();
builder.Services.AddTransient<IUsersRepository, UsersRepository>();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://app.tinylnk.nl")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger("Whack-A-Mole Users API", enableSwagger: !builder.Environment.IsProduction());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseCors(corsPolicyName);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseDefaultHealthChecks();
app.MapControllers();

Console.WriteLine("Starting...");
app.Run();
Console.WriteLine("Stopped");