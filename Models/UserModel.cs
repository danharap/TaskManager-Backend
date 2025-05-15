namespace BackendW2Proj.Models
{
    public class UserModel
    {
        public int id { get; set; } // Unique identifier for the user
        public string username { get; set; } = string.Empty; // Username
        public string passwordHash { get; set; } = string.Empty; // Hashed password
        public string role { get; set; } = "User"; // Role (default is "User")
    }

}
