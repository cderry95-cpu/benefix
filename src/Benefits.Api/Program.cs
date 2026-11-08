using Benefits.Application.Employees;
using Benefits.Api.Json;
using Benefits.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: new TitleCaseEnumNamingPolicy()));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAddStarterService, AddStarterService>();
builder.Services.AddScoped<IListEmployeesService, ListEmployeesService>();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Resolve the in-memory SQLite database singleton so its schema is created
// before the server starts accepting requests.
app.Services.GetRequiredService<EmployeeDatabase>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Benefits API v1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
