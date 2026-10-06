using Azure;
using Azure.Data.Tables;

namespace UserFunctions.Models
{
    /// <summary>
    /// User entity stored in Azure Table storage.
    /// PartitionKey = "User", RowKey = the user's id.
    /// </summary>
    public class UserEntity : ITableEntity
    {
        public const string Partition = "User";

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PartitionKey { get; set; } = Partition;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
