using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebApi.Repositories;
using WebApi.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("webhooks/github")]
    public class WebhooksController(
        GitHubWebhookService webhookService,
        BuildRepository builds,
        ILogger<WebhooksController> logger) : ControllerBase
    {
        private readonly GitHubWebhookService webhookService = webhookService;
        private readonly BuildRepository builds = builds;
        private readonly ILogger<WebhooksController> logger = logger;

        [HttpPost]
        public async Task<IActionResult> Receive(CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, cancellationToken);
            var payload = buffer.ToArray();

            var signature = Request.Headers["X-Hub-Signature-256"].ToString();
            if (!webhookService.VerifySignature(payload, signature))
            {
                return Unauthorized();
            }

            var eventType = Request.Headers["X-GitHub-Event"].ToString();
            if (eventType == "ping")
            {
                return Ok();
            }
            if (eventType != "workflow_run")
            {
                // Acknowledge unhandled events so GitHub stops retrying them.
                return NoContent();
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(payload);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON payload.");
            }

            using (document)
            {
                var build = webhookService.ParseWorkflowRun(document.RootElement);
                if (build is null)
                {
                    return BadRequest("Unsupported workflow_run payload.");
                }
                var stored = await builds.UpsertAsync(build);
                logger.LogInformation(
                    "Recorded build {RunId} for {Repository}: {Status}/{Conclusion}",
                    stored.RunId, stored.Repository, stored.Status, stored.Conclusion);
                return NoContent();
            }
        }
    }
}
