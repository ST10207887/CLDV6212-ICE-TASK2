using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserFunctions.Models;

namespace UserFunctions.Functions
{
    /// <summary>
    /// Three HTTP-triggered functions that store and read users in Azure Table storage.
    ///   POST /api/users        -> CreateUser
    ///   GET  /api/users/{id}   -> GetUserById
    ///   GET  /api/users        -> GetAllUsers
    /// Adapted from the Azure Functions HTTP trigger and Azure.Data.Tables documentation.
    /// </summary>
    public class UserFunctions
    {
        private readonly ILogger<UserFunctions> _logger;
        private readonly TableClient _table;

        public UserFunctions(ILogger<UserFunctions> logger, TableServiceClient tableService, IConfiguration config)
        {
            _logger = logger;
            _table = tableService.GetTableClient(config["UsersTableName"] ?? "Users");
            _table.CreateIfNotExists();
        }

        // ---------------------------------------------------------------------
        // Function 1: accept user information as JSON and store it in Table storage
        // ---------------------------------------------------------------------
        [Function("CreateUser")]
        public async Task<IActionResult> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequest req)
        {
            _logger.LogInformation("CreateUser called");

            CreateUserRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<CreateUserRequest>(req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new ErrorResponse { Error = "Request body must be valid JSON." });
            }

            if (body == null)
                return new BadRequestObjectResult(new ErrorResponse { Error = "Request body is required." });

            // Data-annotation validation
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(body, new ValidationContext(body), results, validateAllProperties: true))
            {
                return new BadRequestObjectResult(new ErrorResponse
                {
                    Error = "Validation failed.",
                    Details = results.Select(r => r.ErrorMessage ?? "Invalid value")
                });
            }

            var email = body.Email.Trim().ToLowerInvariant();

            // Email must be unique
            await foreach (var _ in _table.QueryAsync<UserEntity>(u => u.PartitionKey == UserEntity.Partition && u.Email == email, maxPerPage: 1))
            {
                return new ConflictObjectResult(new ErrorResponse { Error = "A user with this email already exists." });
            }

            var entity = new UserEntity
            {
                RowKey = string.IsNullOrWhiteSpace(body.Id) ? Guid.NewGuid().ToString() : body.Id.Trim(),
                FirstName = body.FirstName.Trim(),
                LastName = body.LastName.Trim(),
                Email = email,
                PhoneNumber = body.PhoneNumber?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _table.AddEntityAsync(entity);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return new ConflictObjectResult(new ErrorResponse { Error = $"A user with id '{entity.RowKey}' already exists." });
            }

            var dto = UserResponse.From(entity);
            return new CreatedResult($"/api/users/{dto.Id}", dto);
        }

        // ---------------------------------------------------------------------
        // Function 2: accept a user id and return the user as JSON if it exists
        // ---------------------------------------------------------------------
        [Function("GetUserById")]
        public async Task<IActionResult> GetUserById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("GetUserById called for {Id}", id);

            var result = await _table.GetEntityIfExistsAsync<UserEntity>(UserEntity.Partition, id);
            if (!result.HasValue || result.Value == null)
                return new NotFoundObjectResult(new ErrorResponse { Error = $"User '{id}' was not found." });

            return new OkObjectResult(UserResponse.From(result.Value));
        }

        // ---------------------------------------------------------------------
        // Function 3: return all users in the table as JSON
        // ---------------------------------------------------------------------
        [Function("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req)
        {
            _logger.LogInformation("GetAllUsers called");

            var users = new List<UserResponse>();
            await foreach (var e in _table.QueryAsync<UserEntity>(u => u.PartitionKey == UserEntity.Partition))
                users.Add(UserResponse.From(e));

            return new OkObjectResult(users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName));
        }
    }
}
