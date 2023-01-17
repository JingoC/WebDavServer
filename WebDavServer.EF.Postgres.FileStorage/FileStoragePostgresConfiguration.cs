using Npgsql;

namespace WebDavServer.EF.Postgres.FileStorage
{
    public class FileStoragePostgresConfiguration
    {
        public string ConnectionString { get; init; } = null!;
        public string Username { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string Schema { get; init; } = null!;

        public string GetFullConnectionString()
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                Password = Password,
                Username = Username
            };

            return builder.ConnectionString;
        }
    }
}
