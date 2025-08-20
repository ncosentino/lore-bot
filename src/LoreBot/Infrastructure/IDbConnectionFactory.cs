using System.Data;

namespace LoreBot.Infrastructure;
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    Task<IDbConnection> OpenConnectionAsync(CancellationToken ct = default);
}