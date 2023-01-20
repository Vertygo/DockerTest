using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS;
using Wolverine.RDBMS.Durability;
using Wolverine.Transports;
using Wolverine.Transports.Local;
using Xunit;

namespace PersistenceTests.Durability;

public class RecoverIncomingMessagesTests
{
    public const int theRecoveryBatchSize = 100;
    public const int theBufferedLimit = 500;
    private readonly RecoverIncomingMessages theAction;

    private readonly IListeningAgent theAgent = Substitute.For<IListeningAgent, IListenerCircuit>();

    private readonly IEndpointCollection theEndpoints = Substitute.For<IEndpointCollection>();

    private readonly NodeSettings theSettings = new(null)
    {
        RecoveryBatchSize = theRecoveryBatchSize
    };

    public RecoverIncomingMessagesTests()
    {
        theAction = new RecoverIncomingMessages(NullLogger.Instance, theEndpoints);

        var settings = new LocalQueue("one");
        settings.BufferingLimits = new BufferingLimits(theBufferedLimit, 100);

        theAgent.Endpoint.Returns(settings);
    }

    [Theory]
    [InlineData(ListeningStatus.TooBusy)]
    [InlineData(ListeningStatus.Stopped)]
    [InlineData(ListeningStatus.Unknown)]
    public void not_accepting(ListeningStatus status)
    {
        theAgent.Status.Returns(status);
        theAction.DeterminePageSize(theAgent, new IncomingCount(TransportConstants.DurableLocalUri, 50), theSettings)
            .ShouldBe(0);
    }


    [Theory]
    [InlineData("When only limited by batch size", 0, 5000, theRecoveryBatchSize)]
    [InlineData("Limited by number on server", 0, 8, 8)]
    [InlineData("Limited by number on server 2", 492, 8, 8)]
    [InlineData("Limited by queue count and buffered limit", 433, 300, 66)]
    [InlineData("Already at buffered limit", 505, 300, 0)]
    public void determine_page_size(string description, int queueLimit, int serverCount, int expected)
    {
        theAgent.QueueCount.Returns(queueLimit);
        theAgent.Status.Returns(ListeningStatus.Accepting);

        theAction.DeterminePageSize(theAgent, new IncomingCount(TransportConstants.LocalUri, serverCount), theSettings)
            .ShouldBe(expected);
    }

    [Fact]
    public async Task do_nothing_when_page_size_is_0()
    {
        var action = Substitute.For<RecoverIncomingMessages>(NullLogger.Instance, theEndpoints);
        var count = new IncomingCount(new Uri("stub://one"), 23);

        action.DeterminePageSize(theAgent, count, theSettings).Returns(0);

        var persistence = Substitute.For<IMessageDatabase>();

        theEndpoints.FindListeningAgent(count.Destination)
            .Returns(theAgent);

        var shouldFetchMore = await action.TryRecoverIncomingMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, theSettings);
        shouldFetchMore.ShouldBeFalse();

        await action.DidNotReceive().RecoverMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, Arg.Any<int>(), theAgent, theSettings);
    }

    [Fact]
    public async Task recover_messages_when_page_size_is_non_zero_but_all_were_recovered()
    {
        var action = Substitute.For<RecoverIncomingMessages>(NullLogger.Instance, theEndpoints);
        var count = new IncomingCount(new Uri("stub://one"), 11);

        action.DeterminePageSize(theAgent, count, theSettings).Returns(11);

        var persistence = Substitute.For<IMessageDatabase>();

        theEndpoints.FindListeningAgent(count.Destination)
            .Returns(theAgent);

        var shouldFetchMore = await action.TryRecoverIncomingMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, theSettings);
        shouldFetchMore.ShouldBeFalse();

        await action.Received().RecoverMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, 11, theAgent, theSettings);
    }


    [Fact]
    public async Task recover_messages_when_page_size_is_non_zero_and_not_all_on_server_were_were_recovered()
    {
        var action = Substitute.For<RecoverIncomingMessages>(NullLogger.Instance, theEndpoints);
        var count = new IncomingCount(new Uri("stub://one"), 100);

        action.DeterminePageSize(theAgent, count, theSettings).Returns(11);

        var persistence = Substitute.For<IMessageDatabase>();

        theEndpoints.FindListeningAgent(count.Destination)
            .Returns(theAgent);

        var shouldFetchMore = await action.TryRecoverIncomingMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, theSettings);
        shouldFetchMore.ShouldBeTrue();

        await action.Received().RecoverMessagesAsync((IMessageDatabase)persistence, persistence.Session, count, 11, theAgent, theSettings);
    }
}