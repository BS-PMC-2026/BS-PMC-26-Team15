namespace SamiSpot.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string RoleType { get; set; } //User/Contributor

        public bool IsActive { get; set; } = true;   

    }
}