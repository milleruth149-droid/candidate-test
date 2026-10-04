using System.Text.Json.Serialization;
using Requests.Infrastructure;
using Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// שם קבוע להגדרת ה-CORS, כדי שנוכל להפנות אליה בהמשך.
const string AngularCorsPolicy = "AngularDev";

builder.Services
    .AddControllers()
    // מציג enums כטקסט ב-JSON ("Completed") במקום כמספר (3).
    // זה משפיע גם על התשובה וגם על ה-Swagger, שיציג רשימה נפתחת עם שמות.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure();

// CORS: מאשר רק לכתובת של Angular (ולא לכל אתר בעולם) לפנות לשרת.
// הכתובת http://localhost:4200 היא ברירת המחדל של ng serve.
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
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

// חשוב: UseCors חייב לבוא לפני MapControllers, אחרת ההגדרה לא תחול.
app.UseCors(AngularCorsPolicy);

app.MapControllers();

app.Run();

public partial class Program { }