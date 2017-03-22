using System;
using CommonDomain.Persistence;
using GridDomain.Common;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node.Actors;
using GridDomain.Node.Configuration.Composition;
using Microsoft.Practices.Unity;

namespace GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain
{
    public class SoftwareProgrammingSagaContainerConfiguration : IContainerConfiguration
    {
        private readonly IContainerConfiguration _sagaConfiguration =
            new SagaConfiguration<SoftwareProgrammingSaga, SoftwareProgrammingSagaState, SoftwareProgrammingSagaFactory>(SoftwareProgrammingSaga.Descriptor, null, null);

        public void Register(IUnityContainer container)
        {
            _sagaConfiguration.Register(container);
        }
    }
}