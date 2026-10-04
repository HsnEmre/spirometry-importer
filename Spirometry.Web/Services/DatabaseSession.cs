namespace Spirometry.Web.Services
{
    public sealed class DatabaseSession
    {
        public required string TenantId { get; init; }
        public required string Username { get; init; }
        public required string Password { get; init; }
    }
}
