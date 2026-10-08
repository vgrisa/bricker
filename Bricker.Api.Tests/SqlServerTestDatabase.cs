using Bricker.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Tests;

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private const string TestDatabasePrefix = "BrickerTests_";
    private readonly string connectionString;

    public SqlServerTestDatabase()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("BRICKER_TEST_CONNECTION_STRING")
            ?? "Server=localhost\\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        var builder = new SqlConnectionStringBuilder(configuredConnection)
        {
            InitialCatalog = $"{TestDatabasePrefix}{Guid.NewGuid():N}"
        };
        connectionString = builder.ConnectionString;
    }

    public BrickerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BrickerDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
            .Options;
        return new BrickerDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (databaseName is null || !databaseName.StartsWith(TestDatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("A limpeza de testes só pode remover bancos temporários da Bricker.");

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
