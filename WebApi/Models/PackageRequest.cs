using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class PackageRequest
    {
        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public required string Repository { get; set; }

        [Required]
        public required string License { get; set; }
    }
}
