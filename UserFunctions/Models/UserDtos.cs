using System.ComponentModel.DataAnnotations;

namespace UserFunctions.Models
{
    /// <summary>JSON body accepted by POST /api/users.</summary>
    public class CreateUserRequest
    {
        /// <summary>Optional. A GUID is generated when no id is supplied.</summary>
        [StringLength(50)]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Id may only contain letters, numbers, '-' and '_'.")]
        public string? Id { get; set; }

        [Required, StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>JSON returned to clients (no PartitionKey/RowKey/ETag exposed).</summary>
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public static UserResponse From(UserEntity e) => new()
        {
            Id = e.RowKey,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            CreatedAt = e.CreatedAt
        };
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public IEnumerable<string>? Details { get; set; }
    }
}
