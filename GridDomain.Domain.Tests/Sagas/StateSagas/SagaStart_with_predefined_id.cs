﻿using System;
using System.Diagnostics;
using System.Threading;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node;
using GridDomain.Node.Configuration.Akka;
using GridDomain.Node.Configuration.Persistence;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.Framework;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tests.FutureEvents;
using GridDomain.Tests.Sagas.StateSagas.SampleSaga;
using GridDomain.Tests.Sagas.StateSagas.SampleSaga.Events;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace GridDomain.Tests.Sagas.StateSagas
{
    [TestFixture]
    public class SagaStart_with_predefined_id : NodeCommandsTest
    {

        [Test]
        public void When_start_message_has_saga_id_Saga_starts_with_it()
        {
            var publisher = GridNode.Container.Resolve<IPublisher>();
            var sagaId = Guid.NewGuid();

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            publisher.Publish(new GotTiredEvent(Guid.NewGuid()).CloneWithSaga(sagaId));

            Thread.Sleep(Debugger.IsAttached ? TimeSpan.FromSeconds(1000): TimeSpan.FromSeconds(1));

            var sagaState = LoadSagaState<SoftwareProgrammingSaga,
                                          SoftwareProgrammingSagaState,
                                          GotTiredEvent>(sagaId);

            Assert.AreEqual(sagaId,sagaState.Id);
            Assert.AreEqual(SoftwareProgrammingSaga.States.DrinkingCoffe, sagaState.MachineState);
        }

        public SagaStart_with_predefined_id() : base(new AutoTestAkkaConfiguration().ToStandAloneInMemorySystemConfig(),"TestSagaStart", false)
        {
        }

        protected override TimeSpan Timeout { get; }
        protected override GridDomainNode CreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {

            var config = new CustomContainerConfiguration(container => { 
                    container.RegisterType<ISagaFactory<SoftwareProgrammingSaga, SoftwareProgrammingSagaState>, SoftwareProgrammingSagaFactory>();
                    container.RegisterType<ISagaFactory<SoftwareProgrammingSaga, GotTiredEvent>, SoftwareProgrammingSagaFactory>();
                    container.RegisterType<IEmptySagaFactory<SoftwareProgrammingSaga>, SoftwareProgrammingSagaFactory>();
                    container.RegisterInstance<IQuartzConfig>(new InMemoryQuartzConfig());
             });

            return new GridDomainNode(config, new SoftwareProgrammingSagaRoutemap(),TransportMode.Standalone, Sys);
        }
    }
}
