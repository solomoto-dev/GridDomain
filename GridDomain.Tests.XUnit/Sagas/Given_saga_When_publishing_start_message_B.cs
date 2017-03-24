using System;
using System.Threading.Tasks;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit.Sagas
{
    public class Given_saga_When_publishing_start_message_B : SoftwareProgrammingSagaTest
    {
        public Given_saga_When_publishing_start_message_B(ITestOutputHelper helper) : base(helper) {}

        [Fact]
        public async Task When_publishing_start_message()
        {
            var startMessage = new SleptWellEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            Node.NewDebugWaiter()
                .Expect<SagaCreatedEvent<SoftwareProgrammingSagaState>>()
                .Create()
                .SendToSagas(startMessage)
                .Wait();

            var sagaData = await this.LoadAggregate<SagaStateAggregate<SoftwareProgrammingSagaState>>(startMessage.SagaId);
            // Saga_has_correct_data()
            Assert.Equal(startMessage.SofaId, sagaData.SagaState.SofaId);
            //Saga_has_correct_state()
            Assert.Equal(nameof(SoftwareProgrammingSaga.Coding), sagaData.SagaState.CurrentStateName);
        }
    }
}