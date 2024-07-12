namespace final_backend.Models
{
    public class UserResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }

        public string JWT {get; set; }  

        public int UserRole {get; set; }
    }
}
