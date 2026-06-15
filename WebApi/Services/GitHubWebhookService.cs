using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebApi.Models;

namespace WebApi.Services
{
    public class GitHubWebhookService(IOptions<GitHubWebhookOptions> options)
    {
        private const string SignaturePrefix = "sha256=";

        private readonly GitHubWebhookOptions options = options.Value;

        // Validates the X-Hub-Signature-256 header GitHub computes as an
        // HMAC-SHA256 of the raw request body using the shared secret.
        public bool VerifySignature(ReadOnlySpan<byte> payload, string? signatureHeader)
        {
            if (string.IsNullOrEmpty(options.Secret)
                || signatureHeader is null
                || !signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var provided = signatureHeader[SignaturePrefix.Length..];
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Secret), payload);
            var expected = Convert.ToHexStringLower(hash);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided));
        }

        // Maps a workflow_run webhook payload to a Build, or null when the
        // payload is not a workflow run event we can persist.
        public Build? ParseWorkflowRun(JsonElement root)
        {
            if (!root.TryGetProperty("workflow_run", out var run)
                || run.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var runId = GetInt64(run, "id");
            var url = GetString(run, "html_url");
            var status = GetString(run, "status");
            var repository = root.TryGetProperty("repository", out var repo)
                && repo.ValueKind == JsonValueKind.Object
                ? GetString(repo, "html_url")
                : null;
            if (runId is null || repository is null || url is null || status is null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            return new Build
            {
                Repository = repository,
                RunId = runId.Value,
                RunNumber = (int)(GetInt64(run, "run_number") ?? 0),
                Workflow = GetString(run, "name") ?? "",
                Branch = GetString(run, "head_branch") ?? "",
                Commit = GetString(run, "head_sha") ?? "",
                Status = status,
                Conclusion = GetString(run, "conclusion"),
                Url = url,
                Updated = now
            };
        }

        private static string? GetString(JsonElement element, string property)
            => element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static long? GetInt64(JsonElement element, string property)
            => element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt64(out var number)
                ? number
                : null;
    }
}
