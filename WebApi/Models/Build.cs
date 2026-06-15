namespace WebApi.Models
{
    public class Build
    {
        public Guid Id { get; set; }

        public Guid? PackageId { get; set; }

        public required string Repository { get; set; }

        public long RunId { get; set; }

        public int RunNumber { get; set; }

        public required string Workflow { get; set; }

        public required string Branch { get; set; }

        public required string Commit { get; set; }

        public required string Status { get; set; }

        public string? Conclusion { get; set; }

        public required string Url { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }
}
