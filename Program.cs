using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CityImportService>();
builder.Services.AddScoped<RedAlertService>();
builder.Services.AddHostedService<RedAlertBackgroundService>();
builder.Services.AddScoped<CityCoordinateService>();

//builder.Services.AddHostedService<ArcGisShelterSyncService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    if (!context.Shelters.Any())
    {
        var controller = new SamiSpot.Controllers.MapController(context);
        await controller.ScanGovMapSample();
    }

    var cityImportService = services.GetRequiredService<CityImportService>();
    var env = services.GetRequiredService<IWebHostEnvironment>();
    var filePath = Path.Combine(env.ContentRootPath, "Data", "city.csv");

    cityImportService.ImportCitiesFromCsv(filePath);
    var cityCoordinateService = services.GetRequiredService<CityCoordinateService>();
    cityCoordinateService.UpdateCityLatLng();

    // ✅ NO RedAlertService call here
}

app.Run();