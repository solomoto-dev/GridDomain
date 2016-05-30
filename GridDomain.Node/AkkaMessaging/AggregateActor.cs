using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Persistence;
using CommonDomain;
using CommonDomain.Core;
using GridDomain.Balance.Domain.BalanceAggregate;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing;

namespace GridDomain.Node.AkkaMessaging
{
    /// <summary>
    /// Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public class AggregateActor<TAggregate>: ReceivePersistentActor where TAggregate : AggregateBase
    {
        private TAggregate _aggregate;
        private readonly IPublisher _publisher;
        private readonly IAggregateCommandsHandler<TAggregate> _handler;
        public override string PersistenceId { get; }

        public AggregateActor(IAggregateCommandsHandler<TAggregate> handler, AggregateFactory factory, IPublisher publisher)
        {
            _handler = handler;
            _publisher = publisher;
            PersistenceId = Self.Path.Name;
            _aggregate = factory.Build<TAggregate>(AggregateActorName.Parse<TAggregate>(Self.Path.Name).Id);

            CommandAny(cmd => {
                                  var events = _handler.Execute(_aggregate, (ICommand) cmd);
                                  PersistAll(events, e => _publisher.Publish(e));
            });
            Recover<SnapshotOffer>(offer => _aggregate = (TAggregate)offer.Snapshot);
            Recover<DomainEvent>(e => ((IAggregate)_aggregate).ApplyEvent(e));

            //var plugin = Persistence.Instance.Apply(Context.System).JournalFor(null);
            //plugin.Ask(new object());
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            base.OnPersistRejected(cause, @event, sequenceNr);
        }
    }

}