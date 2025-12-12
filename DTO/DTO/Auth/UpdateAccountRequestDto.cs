using System.ComponentModel.DataAnnotations;

namespace DTO.DTO.Auth
{
    public class UpdateAccountRequestDto
    {
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 255 characters")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string? Email { get; set; }

        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)\d{8}$", 
            ErrorMessage = "Phone number must be a valid Vietnamese phone number (10 digits, starting with 0 or +84, followed by 3, 5, 7, 8, or 9)")]
        [StringLength(12, ErrorMessage = "Phone number cannot exceed 12 characters")]
        public string? Phone { get; set; }

        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string? ImageAccountUrl { get; set; }
    }
}

