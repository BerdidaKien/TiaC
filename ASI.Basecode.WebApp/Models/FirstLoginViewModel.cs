namespace ASI.Basecode.WebApp.Models
{
    /// <summary>First-login setup for accounts created by an admin</summary>
    public class FirstLoginViewModel
    {
        // Shown read-only
        public string FullName { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        // Filled in by the user
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
