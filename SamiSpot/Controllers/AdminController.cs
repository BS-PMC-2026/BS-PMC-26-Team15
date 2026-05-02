using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Models;
using System.Data;

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
            var shelter = _context.ContributorShelters.FirstOrDefault(s => s.Id == id);

            if (shelter == null)
                return NotFound();

            return View(shelter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditShelter(int id, ContributorShelter shelter)
        {
            if (id != shelter.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(shelter);

            _context.ContributorShelters.Update(shelter);
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

            // delete images first (VERY IMPORTANT)
            if (shelter.Images != null && shelter.Images.Any())
            {
                _context.ContributorShelterImages.RemoveRange(shelter.Images);
            }

            // then delete shelter
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
    }
}