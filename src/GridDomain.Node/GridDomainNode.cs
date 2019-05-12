﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Autofac;
using DotNetty.Common.Concurrency;
using GridDomain.Aggregates;
using GridDomain.Domains;
using GridDomain.Node.Akka;
using GridDomain.Node.Akka.Cluster;
using GridDomain.Node.Akka.Configuration;
using GridDomain.Node.Tests;
using Microsoft.Extensions.Logging;

namespace GridDomain.Node
{
    public class GridDomainNode : INode, IExtension
    {
        public ActorSystem System;
        private IContainer Container { get; set; }
        private readonly IDomainConfiguration[] _domainConfigurations;
        public TimeSpan DefaultTimeout { get; }
        public string Name;

        public GridDomainNode(ActorSystem system, params IDomainConfiguration[] domains) : this(system,
            TimeSpan.FromSeconds(5), domains)
        {
        }

        public GridDomainNode(ActorSystem actorSystem,
                              TimeSpan defaultTimeout,
                              params IDomainConfiguration[] domains)
        {
            _domainConfigurations = domains;
            if (!_domainConfigurations.Any())
                throw new NoDomainConfigurationException();
            if (_domainConfigurations.Any(d => d == null))
                throw new InvalidDomainConfigurationException();

            DefaultTimeout = defaultTimeout;
            System = actorSystem;
        }


        public void Dispose()
        {
            Container?.Dispose();
        }

        public async Task<IDomain> Start()
        {
            Address = System.GetAddress().ToString();
            Name = System.Name;

            System.Log.Info("Starting GridDomain node {Id}", Name);

            var containerBuilder = new ContainerBuilder();
            var domainBuilder = new ClusterDomainBuilder(System, containerBuilder);
            foreach (var configuration in _domainConfigurations)
            {
                await configuration.Register(domainBuilder);
            }

            var domain = await domainBuilder.Build();
            return domain;
        }

        public string Address { get; private set; }


        internal class NoDomainConfigurationException : Exception
        {
        }

        public class InvalidDomainConfigurationException : Exception
        {
        }
    }
}