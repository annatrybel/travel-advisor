using Travel_Advisor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddDbContext<TravelAdvisorContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient(); 
builder.Services.AddTransient<DestinationSeeder>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

async Task SeedDatabaseAsync(IHost appHost)
{
    using (var scope = appHost.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var destinationSeeder = services.GetRequiredService<DestinationSeeder>();
            await destinationSeeder.SeedDestinationsAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Wyst¹pi³ b³¹d podczas seedingu bazy danych.");
        }
    }
}

await SeedDatabaseAsync(app);


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var ctx = scope.ServiceProvider.GetRequiredService<TravelAdvisorContext>();
        ctx.Database.Migrate();
    }
}

app.Run();
