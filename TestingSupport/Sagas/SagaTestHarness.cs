using System;
using System.Threading.Tasks;
using JasperFx.Core;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Runtime.Handlers;
using Wolverine.Tracking;
using Xunit;

namespace TestingSupport.Sagas;

public class SagaTestHarness<T> : IAsyncLifetime
    where T : Saga
{
    private IHost _host;

    public SagaTestHarness(ISagaHost sagaHost)
    {
        SagaHost = sagaHost;
    }

    public ISagaHost SagaHost { get; }
    
    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.SafeDispose();
        }
    }

    protected async Task withApplication()
    {
        _host = await SagaHost.BuildHostAsync<T>();
    }

    protected string codeFor<T>()
    {
        return _host.Get<HandlerGraph>().HandlerFor<T>().Chain.SourceCode;
    }

    protected async Task invoke<T>(T message)
    {
        if (_host == null)
        {
            await withApplication();
        }

        await _host.Get<IMessageBus>().InvokeAsync(message);
    }

    protected async Task send<T>(T message)
    {
        if (_host == null)
        {
            await withApplication();
        }

        await _host.ExecuteAndWaitValueTaskAsync(x => x.SendAsync(message));
    }

    protected Task send<T>(T message, object sagaId)
    {
        return _host.SendMessageAndWaitAsync(message, new DeliveryOptions { SagaId = sagaId.ToString() }, 10000);
    }

    protected Task<T> LoadState(Guid id)
    {
        return SagaHost.LoadState<T>(id);
    }

    protected Task<T> LoadState(string id)
    {
        return SagaHost.LoadState<T>(id);
    }

    protected Task<T> LoadState(int id)
    {
        return SagaHost.LoadState<T>(id);
    }

    protected Task<T> LoadState(long id)
    {
        return SagaHost.LoadState<T>(id);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
    
}