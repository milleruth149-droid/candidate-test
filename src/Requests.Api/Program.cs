using System.Text.Json.Serialization;
using Requests.Infrastructure;
using Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// שם קבוע להגדרת ה-CORS, כדי שנוכל להפנות אליה בהמשך.
const string AngularCorsPolicy = "AngularDev";

builder.Services
    .AddControllers()
    
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure();


builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // מאפשר לכל כתובת פיתוח של Codespaces להתחבר
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors(AngularCorsPolicy);

app.MapControllers();

app.Run();

public partial class Program { }