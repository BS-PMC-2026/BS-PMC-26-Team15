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

builder.Services.AddSession();

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
app.UseSession();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    // 🔥 CREATE Users table if it does NOT exist
    context.Database.ExecuteSqlRaw(@"
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
    CREATE TABLE Users (
        Id INT IDENTITY PRIMARY KEY,
        UserName NVARCHAR(MAX),
        Email NVARCHAR(MAX),
        Password NVARCHAR(MAX),
        RoleType NVARCHAR(MAX)
    )");
    context.Database.ExecuteSqlRaw(@"
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Feedbacks' AND xtype='U')
    CREATE TABLE Feedbacks (
        Id INT IDENTITY PRIMARY KEY,
        ShelterId INT NOT NULL,
        UserName NVARCHAR(MAX),
        Comment NVARCHAR(MAX),
        CreatedAt DATETIME
    )");

    var adminExists = context.Users.Any(u => u.Email == "admin@sami.com");

    if (!adminExists)
    {
        context.Database.ExecuteSqlRaw(@"
    INSERT INTO Users (UserName, Email, Password, RoleType)
    VALUES ('Admin', 'admin@sami.com', 'Admin123', 'Admin')
    ");
    }


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