namespace ASI.Basecode.Services.ServiceModels
{
    public class ProfileViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string BorrowerStatus { get; set; }
        public bool IsActive { get; set; }
    }
}