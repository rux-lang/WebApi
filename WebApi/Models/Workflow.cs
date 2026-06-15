namespace WebApi.Models
{
    public class Workflow
    {
        public required string Name { get; set; }

        public string? BuildConclusion { get; set; }

        public DateTime? BuildCompleted { get; set; }

        public string? TestConclusion { get; set; }

        public DateTime? TestCompleted { get; set; }

        public string? DeployConclusion { get; set; }

        public DateTime? DeployCompleted { get; set; }
    }
}
