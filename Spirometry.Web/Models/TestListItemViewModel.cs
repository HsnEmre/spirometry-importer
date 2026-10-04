namespace Spirometry.Web.Models
{
    public sealed class TestListItemViewModel
    {
        public Guid Id { get; init; }
        public required string TenantId { get; init; }

        public required string OrderId { get; init; }

        public required string EmployeeRef { get; init; }
        public DateTime TestDate { get; init; }

        public required string TestType { get; init; }

        public string? QualityGrade { get; init; }


        public string[] Warnings { get; init; } = [];
    }
}
