using Microsoft.OpenApi.Models;
using TechnicalTest;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Version = "v1",
    Title = "Implement Swagger UI",
    Description = "A simple example to Implement Swagger UI"
}));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<BusinessNumberDelay>(() => Task.Delay(TimeSpan.FromSeconds(1)));
builder.Services.AddScoped<ILoanApplicationAssessmentService, LoanApplicationAssessmentService>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Showing API V1"));
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
