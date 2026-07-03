namespace WebApi.Services
{
    public class PlaygroundOptions
    {
        public string Image { get; set; } = "ghcr.io/rux-lang/rux-playground:latest";

        // Base directory for per-run sandbox folders. Empty uses the system temp
        // dir (fine for native Docker). A leading "~" expands to the home dir —
        // useful with Docker Desktop, which only bind-mounts shared host paths.
        public string WorkRoot { get; set; } = "";

        public int TimeoutSeconds { get; set; } = 10;

        public string MemoryLimit { get; set; } = "256m";

        public double Cpus { get; set; } = 1.0;

        public int PidsLimit { get; set; } = 128;

        // Maximum number of containers allowed to run concurrently. Additional
        // requests wait for a slot (or the request's own cancellation).
        public int MaxConcurrency { get; set; } = 4;
    }
}
