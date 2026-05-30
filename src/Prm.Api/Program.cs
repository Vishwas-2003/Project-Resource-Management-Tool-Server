using Prm.Api.DependencyInjection;
using Prm.Api.Infrastructure;
using Prm.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.RegisterJwtAuthentication(builder.Configuration);
builder.Services.AddDbContext(builder.Configuration);
builder.Services.RegisterRepositories();
builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

await DatabaseInitializer.Initialize(app.Services);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
