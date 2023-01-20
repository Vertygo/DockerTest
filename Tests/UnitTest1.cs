using System.Data.SqlClient;

namespace Tests;

public class UnitTest1
{
    private const string ConnectionString = "Server=localhost,1434;User Id=sa;Password=P@55w0rd;Timeout=5;MultipleActiveResultSets=True;Initial Catalog=master;Encrypt=False;TrustServerCertificate=True";
    
    [Fact]
    public async Task Test1()
    {
        var sqlConn = new SqlConnection(ConnectionString);
        var sqlComm = new SqlCommand("select 1", sqlConn);
        await sqlConn.OpenAsync();
        var obj = await sqlComm.ExecuteScalarAsync();
        
        Assert.Equal(1, obj);
    }
}