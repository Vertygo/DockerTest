// <auto-generated/>
#pragma warning disable
using Wolverine.Marten.Publishing;

namespace Internal.Generated.WolverineHandlers
{
    // START: IncrementManyHandler353257707
    public class IncrementManyHandler353257707 : Wolverine.Runtime.Handlers.MessageHandler
    {
        private readonly Wolverine.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;

        public IncrementManyHandler353257707(Wolverine.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory)
        {
            _outboxedSessionFactory = outboxedSessionFactory;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Wolverine.Runtime.MessageContext context, System.Threading.CancellationToken cancellation)
        {
            var letterAggregateHandler = new PersistenceTests.Marten.LetterAggregateHandler();
            var incrementMany = (PersistenceTests.Marten.IncrementMany)context.Envelope.Message;
            await using var documentSession = _outboxedSessionFactory.OpenSession(context);
            var eventStore = documentSession.Events;
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<PersistenceTests.Marten.LetterAggregate>(incrementMany.LetterAggregateId, cancellation).ConfigureAwait(false);

            var outgoing1 = letterAggregateHandler.Handle(incrementMany, eventStream.Aggregate, documentSession);
            if (outgoing1 != null)
            {
                // Capturing any possible events returned from the command handlers
                eventStream.AppendMany(outgoing1);

            }

            await documentSession.SaveChangesAsync(cancellation).ConfigureAwait(false);
        }

    }

    // END: IncrementManyHandler353257707
    
    
}

