using Hangfire;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Api.DependencyInjection;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.RegisterJwtAuthentication(builder.Configuration);
builder.Services.AddDbContext(builder.Configuration);
builder.Services.RegisterRepositories();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

await DatabaseInitializer.Initialize(app.Services);

using (var scope = app.Services.CreateScope())
{
    var jobScheduler = scope.ServiceProvider.GetRequiredService<IHangfireJobScheduler>();
    await jobScheduler.RegisterRecurringJobsAsync();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();

var hangfireOptions = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
var hangfireDashboardAuthorization = app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>();
app.UseHangfireDashboard(
    hangfireOptions.DashboardPath,
    new DashboardOptions
    {
        Authorization = [hangfireDashboardAuthorization],
    });

app.MapControllers();

app.Run();
