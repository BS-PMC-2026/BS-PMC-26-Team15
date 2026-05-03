using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Models;
using System.Data;
using System.IO;

namespace SamiSpot.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult AllShelters()
        {
            var shelters = _context.ContributorShelters
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            return View(shelters);
        }

        public IActionResult ViewShelter(int id)
        {
            var shelter = _context.ContributorShelters
                .Include(s => s.Images)
                .FirstOrDefault(s => s.Id == id);

            if (shelter == null)
                return NotFound();

            return View(shelter);
        }

        public IActionResult EditShelter(int id)
        {
            var shelter = _context.ContributorShelters
                .Include(s => s.Images)
                .FirstOrDefault(s => s.Id == id);

            if (shelter == null)
                return NotFound();

            ViewBag.ShelterId = shelter.Id;
            ViewBag.ExistingImages = shelter.Images;

            var model = new ContributorShelterFormViewModel
            {
                Name = shelter.Name,
                Address = shelter.Address,
                Latitude = shelter.Latitude,
                Longitude = shelter.Longitude,
                Description = shelter.Description,
                Size = shelter.Size,
                IsAvailable = shelter.IsAvailable
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditShelter(int id, ContributorShelterFormViewModel model)
        {
            var shelter = _context.ContributorShelters
                .Include(s => s.Images)
                .FirstOrDefault(s => s.Id == id);

            if (shelter == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ShelterId = id;
                ViewBag.ExistingImages = shelter.Images;
                return View(model);
            }

            shelter.Name = model.Name;
            shelter.Address = model.Address;
            shelter.Latitude = model.Latitude;
            shelter.Longitude = model.Longitude;
            shelter.Description = model.Description;
            shelter.Size = model.Size;
            shelter.IsAvailable = model.IsAvailable;

            if (!string.IsNullOrWhiteSpace(model.DeletedImageIds))
            {
                var ids = model.DeletedImageIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

                var imagesToDelete = _context.ContributorShelterImages
                    .Where(i => ids.Contains(i.Id))
                    .ToList();

                _context.ContributorShelterImages.RemoveRange(imagesToDelete);
            }

            if (model.Images != null && model.Images.Count > 0)
            {
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                foreach (var image in model.Images)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var path = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }

                    shelter.Images.Add(new ContributorShelterImage
                    {
                        ContributorShelterId = shelter.Id,
                        ImageUrl = "/uploads/" + fileName
                    });
                }
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Shelter updated successfully.";
            return RedirectToAction("AllShelters");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteShelter(int id)
        {
            var shelter = _context.ContributorShelters
                .Include(s => s.Images)
                .FirstOrDefault(s => s.Id == id);

            if (shelter == null)
                return NotFound();

            if (shelter.Images != null && shelter.Images.Any())
            {
                _context.ContributorShelterImages.RemoveRange(shelter.Images);
            }

            _context.ContributorShelters.Remove(shelter);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Shelter deleted successfully.";
            return RedirectToAction("AllShelters");
        }

        [HttpPost]
        public IActionResult ApproveShelter(int id)
        {
            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE ContributorShelters SET Status = 'Approved' WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Shelter approved successfully.";
            return RedirectToAction("PendingShelters");
        }
        public IActionResult ShelterDetails(int id)
        {
            ContributorShelter shelter = null;
            List<ContributorShelterImage> images = new List<ContributorShelterImage>();

            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string shelterQuery = "SELECT * FROM ContributorShelters WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(shelterQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

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

                string imagesQuery = "SELECT * FROM ContributorShelterImages WHERE ContributorShelterId = @Id";

                using (SqlCommand imageCommand = new SqlCommand(imagesQuery, connection))
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
        [HttpPost]
        public IActionResult RejectShelter(int id)
        {
            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE ContributorShelters SET Status = 'Rejected' WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Shelter rejected successfully.";
            return RedirectToAction("PendingShelters");
        }

        public IActionResult PendingShelters()
        {
            List<ContributorShelter> pendingShelters = new List<ContributorShelter>();

            string connectionString = _context.Database.GetConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT * FROM ContributorShelters WHERE Status = 'Pending' ORDER BY CreatedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pendingShelters.Add(new ContributorShelter
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
                        });
                    }
                }
            }

            return View(pendingShelters);
        }

        public IActionResult ManageUsers()
        {
            var users = _context.Users
                .Where(u => u.RoleType == "User")   
                .OrderBy(u => u.Id)                 
                .ToList();

            return View(users);
        }

        public IActionResult Contributors()
        {
            var contributors = _context.Users
                .Where(u => u.RoleType == "Contributor")   
                .OrderBy(u => u.Id)                        
                .ToList();

            return View(contributors);
        }

        private bool IsValidPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;

            return true;
        }


        public IActionResult AddUser(string role)
        {
            var user = new User
            {
                RoleType = role
            };

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user, string ConfirmPassword)
        {
            // ✅ Password match
            if (user.Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match ❌");
                return View(user);
            }

            // ✅ Email duplicate
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("", "This Gmail is already registered ❌");
                return View(user);
            }

            // ✅ Username duplicate
            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                ModelState.AddModelError("", "Username already exists ❌");
                return View(user);
            }

            // ✅ Gmail check
            if (!user.Email.EndsWith("@gmail.com"))
            {
                ModelState.AddModelError("", "Email must be a Gmail address ❌");
                return View(user);
            }

            // ✅ Password validation (same as Register)
            if (!IsValidPassword(user.Password))
            {
                ModelState.AddModelError("", "Password must be at least 8 chars, include upper, lower, number ❌");
                return View(user);
            }

            // ✅ Save
            _context.Users.Add(user);
            _context.SaveChanges();

            // ✅ Redirect based on role
            if (user.RoleType == "Contributor")
                return RedirectToAction("Contributors");

            return RedirectToAction("ManageUsers");
        }

    }
}