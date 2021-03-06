using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Autofac;
using GridDomain.Abstractions;
using GridDomain.Aggregates;
using GridDomain.Aggregates.Abstractions;
using GridDomain.Common;
using GridDomain.Domains;
using GridDomain.Node.Akka.Actors.Aggregates;
using GridDomain.Node.Akka.Cluster;
using GridDomain.Node.Akka.Cluster.CommandGrouping;

namespace GridDomain.Node.Akka.Extensions.Aggregates {

    
    public class AggregatesDomainExtension : IExtension,IAggregatesDomainBuilder
    {
        
        readonly Dictionary<string, IActorRef> _aggregatesRegions = new Dictionary<string, IActorRef>();
        private readonly Dictionary<Type, object> _commandHandlerProxies = new Dictionary<Type,object>();
        private IContainer _aggregateConfigurationsContainer;
        private readonly ContainerBuilder _aggregateConfigurationsContainerBuilder;
        private readonly ActorSystem _system;
        public IAggregatesController Controller { get; private set; }
        private static readonly OneForOneStrategy SupervisorStrategy
            = new OneForOneStrategy(ex =>
            {
                switch (ex)
                {
                    case AggregateActor.CommandExecutionException cf:
                        return Directive.Restart;
                    case CommandAlreadyExecutedException cae:
                        return Directive.Restart;
                    default:
                        return Directive.Stop;
                }
            });

        
        public AggregatesDomainExtension(ActorSystem system, ContainerBuilder builder = null)
        {
            _system = system;
            _aggregateConfigurationsContainerBuilder = builder ?? new ContainerBuilder();
        }

        public IAggregateConfiguration<T> GetConfiguration<T>() where T : IAggregate
        {
            return _aggregateConfigurationsContainer.Resolve<IAggregateConfiguration<T>>();
        }
        
        public async Task RegisterAggregate<TAggregate>(IAggregateConfiguration<TAggregate> configuration)
            where TAggregate : class, IAggregate
        {
            _aggregateConfigurationsContainerBuilder.RegisterInstance(configuration);


            var clusterSharding = ClusterSharding.Get(_system);
            var regionName = AggregateAddress.New<TAggregate>("regionOnly").Name;

            if (_aggregatesRegions.ContainsKey(regionName))
                throw new InvalidOperationException("Cannot add duplicate region with name: " + regionName);

            var aggregateProps = Props.Create<AggregateActor<TAggregate>>()
                .WithSupervisorStrategy(SupervisorStrategy);

            _system.Log.Info("Starting new shard region {regionName}", regionName);

            var setting = ClusterShardingSettings.Create(_system);
            if (!string.IsNullOrEmpty(configuration.Settings.HostRole))
                setting = setting.WithRole(configuration.Settings.HostRole);
            
            var region = await clusterSharding.StartAsync(regionName,
                aggregateProps,setting,
                new ShardedMessageMetadataExtractor()); 

            _aggregatesRegions.Add(regionName, region);
        }
        
        public ICommandsResultAdapter GetAdapter<T>() where T : IAggregate
        {
            _aggregateConfigurationsContainer.TryResolveNamed(typeof(T).BeautyName(),typeof(ICommandsResultAdapter), out var adapter);
            return (adapter as ICommandsResultAdapter) ?? CommandsResultNullAdapter.Instance;
        }
        
        public void RegisterCommandHandler<T>(Func<ICommandHandler<ICommand>, T> proxyBuilder)
        {
            _commandHandlerProxies[typeof(T)] = proxyBuilder;
        }

        public void FinishRegistration()
        {
            _aggregateConfigurationsContainer = _aggregateConfigurationsContainerBuilder.Build();
        }

        public async Task<IDomainPart> Build()
        {
            FinishRegistration();
            
            var routingGroup = new ConsistentMapGroup(_aggregatesRegions)
                .WithMapping(m =>
                {
                    if (!(m is IShardEnvelop env)) throw new UnknownShardMessageException();
                    if (!_aggregatesRegions.ContainsKey(env.Region)) throw new CannotFindRequestedRegion();
                    return env.Region;
                });


            var commandActor = _system.ActorOf(Props.Empty.WithRouter(routingGroup), "Aggregates");
            Controller = new ClusterAggregatesController(_system, commandActor);
            var aggregatesGateway = new AggregatesGateway(new ActorCommandExecutor(commandActor, _system.Log),_commandHandlerProxies);
            return await Task.FromResult<IAggregatesGateway>(aggregatesGateway);
        }


        public void RegisterCommandsResultAdapter<TAggregate>(ICommandsResultAdapter adapter) where TAggregate : IAggregate
        {
            _aggregateConfigurationsContainerBuilder.RegisterInstance(adapter).Named<ICommandsResultAdapter>(typeof(TAggregate).BeautyName());
        }
        
        
        public class UnknownShardMessageException : Exception
        {
        }

        public class CannotFindRequestedRegion : Exception
        {
        }
    }
}