using System;
using System.Threading.Tasks;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tests.XUnit.BalloonDomain;
using GridDomain.Tests.XUnit.BalloonDomain.Events;
using GridDomain.Tools.Repositories;
using GridDomain.Tools.Repositories.AggregateRepositories;
using GridDomain.Tools.Repositories.EventRepositories;
using Xunit;

namespace GridDomain.Tests.Acceptance.XUnit.Tools
{
    public class Aggregate_repository_tests_given_persisted_events
    {
        private static readonly AutoTestAkkaConfiguration AutoTestAkkaConfiguration = new AutoTestAkkaConfiguration();

        private static readonly string AkkaWriteDbConnectionString =
            AutoTestAkkaConfiguration.Persistence.JournalConnectionString;

        public static readonly object[] EventRepositories =
        {
            new object[]
            {
                ActorSystemJournalRepository.New(
                                                 AutoTestAkkaConfiguration,
                                                 new EventsAdaptersCatalog()),
                new AggregateRepository(
                                        ActorSystemJournalRepository.New(
                                                                         new AutoTestAkkaConfiguration(),
                                                                         new EventsAdaptersCatalog()))
            },
            new object[]
            {
                ActorSystemJournalRepository.New(
                                                 AutoTestAkkaConfiguration,
                                                 new EventsAdaptersCatalog()),
                AggregateRepository.New(
                                        AkkaWriteDbConnectionString)
            },
            new object[]
            {
                DomainEventsRepository.New(
                                           AkkaWriteDbConnectionString),
                AggregateRepository.New(
                                        AkkaWriteDbConnectionString)
            },
            new object[]
            {
                DomainEventsRepository.New(
                                           AkkaWriteDbConnectionString),
                new AggregateRepository(
                                        ActorSystemJournalRepository.New(
                                                                         new AutoTestAkkaConfiguration(),
                                                                         new EventsAdaptersCatalog()))
            }
        };

        private Balloon _aggregate;
        private BalloonTitleChanged _changed;
        private BalloonCreated _created;
        private Guid _sourceId;

        [Theory]
        [MemberData(nameof(EventRepositories))]
        public async Task Given_only_aggregate_events_persisted_it_can_be_loaded(IRepository<DomainEvent> eventRepo,
                                                                                 AggregateRepository aggrRepo)
        {
            try
            {
                _sourceId = Guid.NewGuid();
                _created = new BalloonCreated("initial value", _sourceId);
                _changed = new BalloonTitleChanged("changed value", _sourceId);

                var persistenceId = AggregateActorName.New<Balloon>(_sourceId).ToString();
                await eventRepo.Save(persistenceId, _created, _changed);
                _aggregate = await aggrRepo.LoadAggregate<Balloon>(_sourceId);

                Assert.Equal(_sourceId, _aggregate.Id);
                Assert.Equal(_changed.Value, _aggregate.Title);
            }
            finally
            {
                eventRepo.Dispose();
                aggrRepo.Dispose();
            }
        }
    }
}