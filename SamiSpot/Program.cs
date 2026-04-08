using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Services;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CityImportService>();
builder.Services.AddScoped<RedAlertService>();
builder.Services.AddHostedService<RedAlertBackgroundService>();
builder.Services.AddScoped<CityCoordinateService>();

if (!isTesting)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddDistributedMemoryCache();
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
app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

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
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ContributorShelters' AND xtype='U')
CREATE TABLE ContributorShelters (
    Id INT IDENTITY PRIMARY KEY,

    Name NVARCHAR(MAX) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL,

    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,

    Description NVARCHAR(MAX),
    Size INT,
    IsAvailable BIT NOT NULL,

    UserId NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(MAX) NOT NULL,

    CreatedAt DATETIME NOT NULL
)");
        context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ContributorShelterImages' AND xtype='U')
CREATE TABLE ContributorShelterImages (
    Id INT IDENTITY PRIMARY KEY,

    ContributorShelterId INT NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,

    FOREIGN KEY (ContributorShelterId) REFERENCES ContributorShelters(Id)
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

        context.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FeedbackReplies' AND xtype='U')
        CREATE TABLE FeedbackReplies (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            FeedbackId INT NOT NULL,
            ParentReplyId INT NULL,
            UserName NVARCHAR(100) NOT NULL,
            ReplyText NVARCHAR(MAX) NOT NULL,
            CreatedAt DATETIME NOT NULL,

            CONSTRAINT FK_FeedbackReplies_Feedbacks
                FOREIGN KEY (FeedbackId) REFERENCES Feedbacks(Id),

            CONSTRAINT FK_FeedbackReplies_ParentReply
                FOREIGN KEY (ParentReplyId) REFERENCES FeedbackReplies(Id)
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

        if (!context.CityLocations.Any())
        {
            var cityImportService = services.GetRequiredService<CityImportService>();
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var filePath = Path.Combine(env.ContentRootPath, "Data", "city.csv");

            cityImportService.ImportCitiesFromCsv(filePath);

            var cityCoordinateService = services.GetRequiredService<CityCoordinateService>();
            cityCoordinateService.UpdateCityLatLng();
        }
    }
}

app.Run();
public partial class Program { }