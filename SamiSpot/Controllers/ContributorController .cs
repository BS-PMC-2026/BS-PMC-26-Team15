using Microsoft.AspNetCore.Mvc;
using SamiSpot.Data;
using SamiSpot.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SamiSpot.Controllers
{
    public class ContributorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ContributorController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult AddShelter()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MyShelters()
        {
            List<ContributorShelter> myShelters = new List<ContributorShelter>();

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT Id, Name, CreatedAt, Status
            FROM ContributorShelters
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            myShelters.Add(new ContributorShelter
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return View(myShelters);
        }

        public IActionResult ShelterDetails(int id)
        {
            ContributorShelter shelter = null;
            List<ContributorShelterImage> images = new List<ContributorShelterImage>();

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string shelterQuery = @"
            SELECT * FROM ContributorShelters
            WHERE Id = @Id AND UserId = @UserId";

                using (SqlCommand command = new SqlCommand(shelterQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@UserId", userName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            shelter = new ContributorShelter
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Address = reader["Address"].ToString(),
                                Latitude = Convert.ToDouble(reader["Latitude"]),
                                Longitude = Convert.ToDouble(reader["Longitude"]),
                                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                                Size = reader["Size"] == DBNull.Value ? null : Convert.ToInt32(reader["Size"]),
                                IsAvailable = Convert.ToBoolean(reader["IsAvailable"]),
                                UserId = reader["UserId"].ToString(),
                                Status = reader["Status"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            };
                        }
                    }
                }

                if (shelter == null)
                {
                    return NotFound();
                }

                string imageQuery = @"
            SELECT * FROM ContributorShelterImages
            WHERE ContributorShelterId = @Id";

                using (SqlCommand imageCommand = new SqlCommand(imageQuery, connection))
                {
                    imageCommand.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader imageReader = imageCommand.ExecuteReader())
                    {
                        while (imageReader.Read())
                        {
                            images.Add(new ContributorShelterImage
                            {
                                Id = Convert.ToInt32(imageReader["Id"]),
                                ContributorShelterId = Convert.ToInt32(imageReader["ContributorShelterId"]),
                                ImageUrl = imageReader["ImageUrl"].ToString()
                            });
                        }
                    }
                }
            }

            shelter.Images = images;
            return View(shelter);
        }

        [HttpGet]
        [Route("api/contributor-shelters/{id}/images")]
        public IActionResult GetContributorShelterImages(int id)
        {
            var images = new List<string>();

            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT ImageUrl
            FROM ContributorShelterImages
            WHERE ContributorShelterId = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            images.Add(reader["ImageUrl"].ToString());
                        }
                    }
                }
            }

            return Json(images);
        }

        [HttpPost]
        public async Task<IActionResult> AddShelter(ContributorShelterFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Latitude == 0 || model.Longitude == 0)
            {
                ModelState.AddModelError("", "Please choose a location from the map.");
                return View(model);
            }

            if (model.Images != null && model.Images.Count > 10)
            {
                ModelState.AddModelError("", "You can upload up to 10 images only.");
                return View(model);
            }

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var createdAt = DateTime.Now;
            var status = "Pending";

            string connectionString = _context.Database.GetConnectionString();

            int newShelterId;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string insertShelterQuery = @"
                    INSERT INTO ContributorShelters
                    (Name, Address, Latitude, Longitude, Description, Size, IsAvailable, UserId, Status, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES
                    (@Name, @Address, @Latitude, @Longitude, @Description, @Size, @IsAvailable, @UserId, @Status, @CreatedAt)";

                using (SqlCommand command = new SqlCommand(insertShelterQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Address", model.Address);
                    command.Parameters.AddWithValue("@Latitude", model.Latitude);
                    command.Parameters.AddWithValue("@Longitude", model.Longitude);
                    command.Parameters.AddWithValue("@Description", (object?)model.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Size", (object?)model.Size ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);
                    command.Parameters.AddWithValue("@UserId", userName);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@CreatedAt", createdAt);

                    newShelterId = (int)await command.ExecuteScalarAsync();
                }

                if (model.Images != null && model.Images.Any())
                {
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "contributor-shelters");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    foreach (var image in model.Images)
                    {
                        if (image.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            string filePath = Path.Combine(uploadFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            string imageUrl = "/uploads/contributor-shelters/" + uniqueFileName;

                            string insertImageQuery = @"
                                INSERT INTO ContributorShelterImages (ContributorShelterId, ImageUrl)
                                VALUES (@ContributorShelterId, @ImageUrl)";

                            using (SqlCommand imageCommand = new SqlCommand(insertImageQuery, connection))
                            {
                                imageCommand.Parameters.AddWithValue("@ContributorShelterId", newShelterId);
                                imageCommand.Parameters.AddWithValue("@ImageUrl", imageUrl);

                                await imageCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }

            TempData["SuccessMessage"] = "Shelter submitted successfully and is waiting for approval.";
            return RedirectToAction("AddShelter");
        }
    }
}