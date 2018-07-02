﻿using System;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Configuration;
using GridDomain.Node;
using GridDomain.Node.Cluster;
using GridDomain.Node.Cluster.Transport;
using GridDomain.Node.Configuration;
using Serilog;

namespace GridDomain.Tests.Unit.Cluster
{
    public static class NodeTestFixtureExtensions
    {
        public static NodeTestFixture Clustered(this NodeTestFixture fxt)
        {
            fxt.ActorSystemConfigBuilder = fxt.ActorSystemConfigBuilder.ConfigureCluster(fxt.Name);
            fxt.NodeBuilder = new ClusterNodeBuilder((GridNodeBuilder)fxt.NodeBuilder);
            fxt.NodeBuilder.Transport(sys => sys.InitDistributedTransport());
            fxt.TestNodeBuilder = (n, kit) => new TestClusterNode((GridClusterNode) n, kit);

            return fxt;
        }
    }

    public class ClusterNodeBuilder : IGridNodeBuilder
    {
        private readonly GridNodeBuilder _builder;
        public ClusterNodeBuilder(GridNodeBuilder builder)
        {
            _builder = builder;
        }

        public IGridDomainNode Build()
        {
            var factory = new DelegateActorSystemFactory(_builder.ActorProducers, _builder.ActorInit);

            return new GridClusterNode(_builder.Configurations, factory, _builder.Logger, _builder.DefaultTimeout);
        }

        public GridNodeBuilder ActorSystem(Func<ActorSystem> sys)
        {
            return _builder.ActorSystem(sys);
        }

        public GridNodeBuilder Initialize(Action<ActorSystem> sys)
        {
            return _builder.Initialize(sys);
        }

        public GridNodeBuilder Transport(Action<ActorSystem> sys)
        {
            return _builder.Transport(sys);
        }

        public GridNodeBuilder Log(ILogger log)
        {
            return _builder.Log(log);
        }

        public ILogger Logger => _builder.Logger;

        public GridNodeBuilder DomainConfigurations(params IDomainConfiguration[] domainConfigurations)
        {
            return _builder.DomainConfigurations(domainConfigurations);
        }

        public GridNodeBuilder Timeout(TimeSpan timeout)
        {
            return _builder.Timeout(timeout);
        }
    }
}