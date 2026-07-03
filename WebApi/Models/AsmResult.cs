using System.Text.Json.Serialization;

namespace WebApi.Models
{
    public class AsmResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        // User-only and full assembly. The compiler emits a single listing, so
        // for now both fields carry the same full output.
        [JsonPropertyName("asm_user")]
        public string AsmUser { get; set; } = "";

        [JsonPropertyName("asm_full")]
        public string AsmFull { get; set; } = "";

        [JsonPropertyName("user_lines")]
        public int UserLines { get; set; }

        [JsonPropertyName("total_lines")]
        public int TotalLines { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("stdout")]
        public string Stdout { get; set; } = "";

        [JsonPropertyName("stderr")]
        public string Stderr { get; set; } = "";

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }
    }
}
