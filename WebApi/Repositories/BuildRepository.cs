using Npgsql;
using WebApi.Models;

namespace WebApi.Repositories
{
    public class BuildRepository(NpgsqlDataSource dataSource)
    {
        private const string Columns =
            "id, package_id, repository, run_id, run_number, workflow, " +
            "branch, commit, status, conclusion, url, created, updated";

        private readonly NpgsqlDataSource dataSource = dataSource;

        // GitHub delivers several events for a single run (requested,
        // in_progress, completed), so upsert keyed on the run id.
        public async Task<Build> UpsertAsync(Build build)
        {
            await using var command = dataSource.CreateCommand(
                "INSERT INTO builds " +
                "(id, package_id, repository, run_id, run_number, workflow, " +
                "branch, commit, status, conclusion, url, created, updated) " +
                "VALUES ($1, " +
                "(SELECT id FROM packages WHERE repository = $2), " +
                "$2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $11) " +
                "ON CONFLICT (run_id) DO UPDATE SET " +
                "package_id = EXCLUDED.package_id, " +
                "run_number = EXCLUDED.run_number, " +
                "workflow = EXCLUDED.workflow, " +
                "branch = EXCLUDED.branch, " +
                "commit = EXCLUDED.commit, " +
                "status = EXCLUDED.status, " +
                "conclusion = EXCLUDED.conclusion, " +
                "url = EXCLUDED.url, " +
                "updated = EXCLUDED.updated " +
                $"RETURNING {Columns}");
            command.Parameters.AddWithValue(Guid.CreateVersion7());
            command.Parameters.AddWithValue(build.Repository);
            command.Parameters.AddWithValue(build.RunId);
            command.Parameters.AddWithValue(build.RunNumber);
            command.Parameters.AddWithValue(build.Workflow);
            command.Parameters.AddWithValue(build.Branch);
            command.Parameters.AddWithValue(build.Commit);
            command.Parameters.AddWithValue(build.Status);
            command.Parameters.AddWithValue((object?)build.Conclusion ?? DBNull.Value);
            command.Parameters.AddWithValue(build.Url);
            command.Parameters.AddWithValue(build.Updated);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return ReadBuild(reader);
        }

        public async Task<List<Build>> GetByPackageAsync(Guid packageId, int limit)
        {
            await using var command = dataSource.CreateCommand(
                $"SELECT {Columns} FROM builds WHERE package_id = $1 " +
                "ORDER BY updated DESC LIMIT $2");
            command.Parameters.AddWithValue(packageId);
            command.Parameters.AddWithValue(limit);
            var builds = new List<Build>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                builds.Add(ReadBuild(reader));
            }
            return builds;
        }

        private static Build ReadBuild(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetGuid(0),
            PackageId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            Repository = reader.GetString(2),
            RunId = reader.GetInt64(3),
            RunNumber = reader.GetInt32(4),
            Workflow = reader.GetString(5),
            Branch = reader.GetString(6),
            Commit = reader.GetString(7),
            Status = reader.GetString(8),
            Conclusion = reader.IsDBNull(9) ? null : reader.GetString(9),
            Url = reader.GetString(10),
            Created = reader.GetFieldValue<DateTime>(11),
            Updated = reader.GetFieldValue<DateTime>(12)
        };
    }
}
