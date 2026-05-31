using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Services;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CityImportService>();
builder.Services.AddScoped<RedAlertService>();

//if (!isTesting)
//{
   // builder.Services.AddHostedService<RedAlertBackgroundService>();
//}

builder.Services.AddScoped<CityCoordinateService>();

if (!isTesting)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            )));
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<OpenAiService>();

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
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();

            // USERS
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    UserName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Password NVARCHAR(MAX),
    RoleType NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1
)");

            context.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Users', 'IsActive') IS NULL
ALTER TABLE Users
ADD IsActive BIT NOT NULL DEFAULT 1
");

            // SHELTERS
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Shelters' AND xtype='U')
CREATE TABLE Shelters (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(MAX) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL,
    City NVARCHAR(MAX),
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Description NVARCHAR(MAX),
    Source NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1
)");

            context.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Shelters', 'Capacity') IS NULL ALTER TABLE Shelters ADD Capacity INT NULL;
IF COL_LENGTH('Shelters', 'ExternalId') IS NULL ALTER TABLE Shelters ADD ExternalId NVARCHAR(MAX) NULL;
IF COL_LENGTH('Shelters', 'GovMapMiklatId') IS NULL ALTER TABLE Shelters ADD GovMapMiklatId NVARCHAR(MAX) NULL;
IF COL_LENGTH('Shelters', 'IsAccessible') IS NULL ALTER TABLE Shelters ADD IsAccessible BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Shelters', 'IsPublic') IS NULL ALTER TABLE Shelters ADD IsPublic BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Shelters', 'LastSyncedAt') IS NULL ALTER TABLE Shelters ADD LastSyncedAt DATETIME2 NULL;
IF COL_LENGTH('Shelters', 'RemoteOpen') IS NULL ALTER TABLE Shelters ADD RemoteOpen BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Shelters', 'ShelterType') IS NULL ALTER TABLE Shelters ADD ShelterType NVARCHAR(MAX) NULL;
IF COL_LENGTH('Shelters', 'SourceUrl') IS NULL ALTER TABLE Shelters ADD SourceUrl NVARCHAR(MAX) NULL;
");

            // ALERTS
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Alerts' AND xtype='U')
CREATE TABLE Alerts (
    Id INT IDENTITY PRIMARY KEY,
    CityHebrew NVARCHAR(MAX) NOT NULL,
    AlertTimeUtc DATETIME2 NOT NULL,
    Threat INT NOT NULL,
    IsDrill BIT NOT NULL
)");

            // CITY LOCATIONS
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CityLocations' AND xtype='U')
CREATE TABLE CityLocations (
    Id INT IDENTITY PRIMARY KEY,
    HebrewName NVARCHAR(MAX) NOT NULL,
    EnglishName NVARCHAR(MAX) NOT NULL,
    X FLOAT NOT NULL,
    Y FLOAT NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL
)");

            // CONTRIBUTOR SHELTERS
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

            // CONTRIBUTOR SHELTER IMAGES
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ContributorShelterImages' AND xtype='U')
CREATE TABLE ContributorShelterImages (
    Id INT IDENTITY PRIMARY KEY,
    ContributorShelterId INT NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    FOREIGN KEY (ContributorShelterId)
    REFERENCES ContributorShelters(Id)
)");

            // FEEDBACKS
            context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Feedbacks' AND xtype='U')
CREATE TABLE Feedbacks (
    Id INT IDENTITY PRIMARY KEY,
    ShelterId INT NOT NULL,
    UserName NVARCHAR(MAX),
    Comment NVARCHAR(MAX),
    CreatedAt DATETIME
)");

            // FEEDBACK REPLIES
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
        FOREIGN KEY (ParentReplyId)
        REFERENCES FeedbackReplies(Id)
)");

            // ADMIN USER
            var adminExists = context.Users.Any(u => u.Email == "admin@sami.com");

            if (!adminExists)
            {
                context.Database.ExecuteSqlRaw(@"
INSERT INTO Users (UserName, Email, Password, RoleType)
VALUES ('Admin', 'admin@sami.com', 'Admin123', 'Admin')
");
            }


            // LOAD CITY DATA
            if (!context.CityLocations.Any())
            {
                var cityImportService =
                    services.GetRequiredService<CityImportService>();

                var env =
                    services.GetRequiredService<IWebHostEnvironment>();

                var filePath = Path.Combine(
                    env.ContentRootPath,
                    "Data",
                    "city.csv");

                cityImportService.ImportCitiesFromCsv(filePath);

                var cityCoordinateService =
                    services.GetRequiredService<CityCoordinateService>();

                cityCoordinateService.UpdateCityLatLng();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ DB initialization failed, app will still start: {ex.Message}");
        // App continues running — DB init will retry on next request
    }
}

app.Run();

public partial class Program { }
