using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Rezerv.Payroll.Api.Infrastructure;
using Rezerv.Payroll.Application;
using Rezerv.Payroll.Infrastructure;

// Payroll is a financial system: formatting/parsing must not depend on the host's locale
// (e.g. a Thai-locale machine would otherwise print Buddhist-era years like 2569).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PayrollExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rezerv Payroll API",
        Version = "v1",
        Description = "Instructor payroll for studio businesses: fixed class fees, booking-based payout with " +
                      "studio no-show policy, attendance bonuses, sales commission with refund reversal, and manual adjustments.",
    });
    var xml = Path.Combine(AppContext.BaseDirectory, "Rezerv.Payroll.Api.xml");
    if (File.Exists(xml))
    {
        c.IncludeXmlComments(xml);
    }
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger is enabled in every environment on purpose: the assessment asks for it as a deliverable.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rezerv Payroll API v1");
    c.DocumentTitle = "Rezerv Payroll API";
});

app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow })).ExcludeFromDescription();

if (app.Configuration.GetValue("Database:MigrateAndSeedOnStartup", true))
{
    await app.Services.InitialiseDatabaseAsync();
}

app.Run();

// Exposed for integration tests (WebApplicationFactory).
public partial class Program;
