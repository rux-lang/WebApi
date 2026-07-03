using System.Text.Json.Serialization;

namespace WebApi.Models
{
    public class RunResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("stdout")]
        public required string Stdout { get; set; }

        [JsonPropertyName("stderr")]
        public required string Stderr { get; set; }

        // Set for timeouts and infrastructure failures; compiler diagnostics are
        // carried in stderr instead.
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }
    }
}
