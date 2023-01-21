using System;
using System.Threading.Tasks;
using IntegrationTests;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestingSupport;
using TestingSupport.Sagas;
using Weasel.Core;
using Wolverine.Marten;

namespace PersistenceTests.Marten.Persistence.Sagas;

public class MartenSagaHost : ISagaHost
{
    private IHost _host;

    public async Task<IHost> BuildHostAsync<TSaga>()
    {
        _host = await WolverineHost.ForAsync(opts =>
        {
            opts.Handlers.DisableConventionalDiscovery().IncludeType<TSaga>();

            opts.Services.AddMarten(x =>
            {
                x.Connection(Servers.PostgresConnectionString);
                x.DatabaseSchemaName = "sagas";
                x.AutoCreateSchemaObjects = AutoCreate.All;
            }).IntegrateWithWolverine();

            opts.PublishAllMessages().Locally();
        });

        return _host;
    }

    public Task<T> LoadState<T>(Guid id) where T : class
    {
        return _host.Services.GetRequiredService<IQuerySession>().LoadAsync<T>(id);
    }

    public Task<T> LoadState<T>(int id) where T : class
    {
        return _host.Services.GetRequiredService<IQuerySession>().LoadAsync<T>(id);
    }

    public Task<T> LoadState<T>(long id) where T : class
    {
        return _host.Services.GetRequiredService<IQuerySession>().LoadAsync<T>(id);
    }

    public Task<T> LoadState<T>(string id) where T : class
    {
        return _host.Services.GetRequiredService<IQuerySession>().LoadAsync<T>(id);
    }
}