using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Shouldly;
using Weasel.Core;
using Weasel.SqlServer;

namespace Tests;

public class UnitTest1
{
    private const string SqlServerConnectionString = "Server=localhost,1434;User Id=sa;Password=P@55w0rd;Timeout=5;MultipleActiveResultSets=True;Initial Catalog=master;Encrypt=False;TrustServerCertificate=True";
    
    [Fact]
    public async Task Without_Library()
    {
        var sqlConn = new SqlConnection(SqlServerConnectionString);
        var sqlComm = new SqlCommand("select 1", sqlConn);
        await sqlConn.OpenAsync();
        var obj = await sqlComm.ExecuteScalarAsync();
        
        Assert.Equal(1, obj);
    }

    [Fact]
    public async Task With_Weasel_Library()
    {
        var settings = new SqlServerSettings();

        await using (var conn1 = new SqlConnection(SqlServerConnectionString))
        await using (var conn2 = new SqlConnection(SqlServerConnectionString))
        await using (var conn3 = new SqlConnection(SqlServerConnectionString))
        {
            await conn1.OpenAsync();
            await conn2.OpenAsync();
            await conn3.OpenAsync();


            await settings.GetGlobalLockAsync(conn1, 1);


            // Cannot get the lock here
            (await settings.TryGetGlobalLockAsync(conn2, null, 1)).ShouldBeFalse();


            await settings.ReleaseGlobalLockAsync(conn1, 1);


            for (var j = 0; j < 5; j++)
            {
                if (await settings.TryGetGlobalLockAsync(conn2, null, 1))
                {
                    return;
                }

                await Task.Delay(250);
            }

            throw new Exception("Advisory lock was not released");
        }
    }
}

public class SqlServerSettings : DatabaseSettings
{
    public SqlServerSettings() : base("dbo", new SqlServerMigrator())
    {
    }


    /// <summary>
    ///     The value of the 'database_principal' parameter in calls to APPLOCK_TEST
    /// </summary>
    public string DatabasePrincipal { get; set; } = "dbo";

    public override DbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public override Task GetGlobalTxLockAsync(DbConnection conn, DbTransaction tx, int lockId,
        CancellationToken cancellation = default)
    {
        return getLockAsync(conn, lockId, "Transaction", tx, cancellation);
    }

    private static async Task getLockAsync(DbConnection conn, int lockId, string owner, DbTransaction? tx,
        CancellationToken cancellation)
    {
        var returnValue = await tryGetLockAsync(conn, lockId, owner, tx, cancellation);

        if (returnValue < 0)
        {
            throw new Exception($"sp_getapplock failed with errorCode '{returnValue}'");
        }
    }

    private static async Task<int> tryGetLockAsync(DbConnection conn, int lockId, string owner, DbTransaction? tx,
        CancellationToken cancellation)
    {
        var cmd = conn.CreateCommand("sp_getapplock");
        cmd.Transaction = tx;

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.With("Resource", lockId.ToString());
        cmd.With("LockMode", "Exclusive");

        cmd.With("LockOwner", owner);
        cmd.With("LockTimeout", 1000);

        var returnValue = cmd.CreateParameter();
        returnValue.ParameterName = "ReturnValue";
        returnValue.DbType = DbType.Int32;
        returnValue.Direction = ParameterDirection.ReturnValue;
        cmd.Parameters.Add(returnValue);

        await cmd.ExecuteNonQueryAsync(cancellation);

        return (int)returnValue.Value!;
    }

    public override async Task<bool> TryGetGlobalTxLockAsync(DbConnection conn, DbTransaction tx, int lockId,
        CancellationToken cancellation = default)
    {
        return await tryGetLockAsync(conn, lockId, "Transaction", tx, cancellation) >= 0;
    }


    public override Task GetGlobalLockAsync(DbConnection conn, int lockId, CancellationToken cancellation = default,
        DbTransaction? transaction = null)
    {
        return getLockAsync(conn, lockId, "Session", transaction, cancellation);
    }

    public override async Task<bool> TryGetGlobalLockAsync(DbConnection conn, DbTransaction? tx, int lockId,
        CancellationToken cancellation = default)
    {
        return await tryGetLockAsync(conn, lockId, "Session", tx, cancellation) >= 0;
    }

    public override async Task<bool> TryGetGlobalLockAsync(DbConnection conn, int lockId, DbTransaction tx,
        CancellationToken cancellation = default)
    {
        return await tryGetLockAsync(conn, lockId, "Session", tx, cancellation) >= 0;
    }

    public override Task ReleaseGlobalLockAsync(DbConnection conn, int lockId,
        CancellationToken cancellation = default,
        DbTransaction? tx = null)
    {
        var sqlCommand = conn.CreateCommand("sp_releaseapplock");
        sqlCommand.Transaction = tx;
        sqlCommand.CommandType = CommandType.StoredProcedure;

        sqlCommand.With("Resource", lockId.ToString());
        sqlCommand.With("LockOwner", "Session");

        return sqlCommand.ExecuteNonQueryAsync(cancellation);
    }
}

public abstract class DatabaseSettings
{
    private string _schemaName;

    protected DatabaseSettings(string defaultSchema, Migrator migrator)
    {
        _schemaName = defaultSchema;
        Migrator = migrator;

        IncomingFullName = $"{SchemaName}.{DatabaseConstants.IncomingTable}";
        OutgoingFullName = $"{SchemaName}.{DatabaseConstants.OutgoingTable}";
    }

    public string? ConnectionString { get; set; }

    public string SchemaName
    {
        get => _schemaName;
        set
        {
            _schemaName = value;

            IncomingFullName = $"{value}.{DatabaseConstants.IncomingTable}";
            OutgoingFullName = $"{value}.{DatabaseConstants.OutgoingTable}";
        }
    }

    public Migrator Migrator { get; }


    public string OutgoingFullName { get; private set; }

    public string IncomingFullName { get; private set; }

    public abstract DbConnection CreateConnection();

    public DbCommand CreateCommand(string command)
    {
        var cmd = CreateConnection().CreateCommand();
        cmd.CommandText = command;

        return cmd;
    }

    public DbCommand CallFunction(string functionName)
    {
        var cmd = CreateConnection().CreateCommand();
        cmd.CommandText = SchemaName + "." + functionName;

        cmd.CommandType = CommandType.StoredProcedure;

        return cmd;
    }

    public Weasel.Core.DbCommandBuilder ToCommandBuilder()
    {
        return CreateConnection().ToCommandBuilder();
    }


    public abstract Task GetGlobalTxLockAsync(DbConnection conn, DbTransaction tx, int lockId,
        CancellationToken cancellation = default);

    public abstract Task<bool> TryGetGlobalTxLockAsync(DbConnection conn, DbTransaction tx, int lockId,
        CancellationToken cancellation = default);

    public abstract Task GetGlobalLockAsync(DbConnection conn, int lockId, CancellationToken cancellation = default,
        DbTransaction? transaction = null);

    public abstract Task<bool> TryGetGlobalLockAsync(DbConnection conn, DbTransaction? tx, int lockId,
        CancellationToken cancellation = default);

    public abstract Task<bool> TryGetGlobalLockAsync(DbConnection conn, int lockId, DbTransaction tx,
        CancellationToken cancellation = default);

    public abstract Task ReleaseGlobalLockAsync(DbConnection conn, int lockId, CancellationToken cancellation = default,
        DbTransaction? tx = null);
}

public static class DatabaseConstants
{
    public const string Id = "id";
    public const string OwnerId = "owner_id";
    public const string Destination = "destination";
    public const string DeliverBy = "deliver_by";
    public const string Body = "body";
    public const string Status = "status";

    public const string ExecutionTime = "execution_time";
    public const string Attempts = "attempts";
    public const string Source = "source";
    public const string MessageType = "message_type";

    public const string ExceptionType = "exception_type";
    public const string ExceptionMessage = "exception_message";
    public const string Replayable = "replayable";

    public const string OutgoingTable = "wolverine_outgoing_envelopes";
    public const string IncomingTable = "wolverine_incoming_envelopes";
    public const string DeadLetterTable = "wolverine_dead_letters";

    public const string ReceivedAt = "received_at"; // add to all
    public const string SentAt = "sent_at"; // add to all

    public const string KeepUntil = "keep_until";

    public static readonly string IncomingFields =
        $"{Body}, {Id}, {Status}, {OwnerId}, {ExecutionTime}, {Attempts}, {MessageType}, {ReceivedAt}";

    public static readonly string OutgoingFields =
        $"{Body}, {Id}, {OwnerId}, {Destination}, {DeliverBy}, {Attempts}, {MessageType}";

    public static readonly string DeadLetterFields =
        $"{Id}, {ExecutionTime}, {Body}, {MessageType}, {ReceivedAt}, {Source}, {ExceptionType}, {ExceptionMessage}, {SentAt}, {Replayable}";
}