using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace WebApi.Services
{
    public class GitHubWebhookService(IOptions<GitHubWebhookOptions> options)
    {
        private const string SignaturePrefix = "sha256=";

        private readonly GitHubWebhookOptions options = options.Value;

        public bool VerifySignature(byte[] payload, string? signatureHeader)
        {
            if (string.IsNullOrEmpty(signatureHeader)
                || !signatureHeader.StartsWith(SignaturePrefix, StringComparison.Ordinal))
            {
                return false;
            }
            var key = Encoding.UTF8.GetBytes(options.Secret);
            var hash = HMACSHA256.HashData(key, payload);
            var expected = SignaturePrefix + Convert.ToHexStringLower(hash);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signatureHeader));
        }
    }
}
