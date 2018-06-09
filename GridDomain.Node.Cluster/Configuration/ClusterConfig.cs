using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Node.Cluster.Configuration.Hocon;
using GridDomain.Node.Configuration;
using Serilog;

namespace GridDomain.Node.Cluster.Configuration
{
    public class ClusterConfig
    {
        private readonly List<ActorSystemConfigBuilder> _seedNodes = new List<ActorSystemConfigBuilder>();
        private readonly List<ActorSystemConfigBuilder> _autoSeedNodes = new List<ActorSystemConfigBuilder>();
        private readonly List<ActorSystemConfigBuilder> _workerNodes = new List<ActorSystemConfigBuilder>();
        public readonly ILogger Logger;
        
        private Func<ActorSystem, Task> _onMemberUp = s => Task.CompletedTask;
        private Func<ActorSystem, Task> _additionalInit = s => Task.CompletedTask;

        public ClusterConfig(string name, ILogger log)
        {
            Logger = log;
            Name = name;
        }

        public string Name { get; }
        public IReadOnlyCollection<ActorSystemConfigBuilder> SeedNodes => _seedNodes;
        public IReadOnlyCollection<ActorSystemConfigBuilder> AutoSeedNodes => _autoSeedNodes;
        public IReadOnlyCollection<ActorSystemConfigBuilder> WorkerNodes => _workerNodes;

        public void AddAutoSeed(params ActorSystemConfigBuilder[] configBuilder)
        {
            _autoSeedNodes.AddRange(configBuilder);
        }

        public void AddSeed(params ActorSystemConfigBuilder[] configBuilder)
        {
            _seedNodes.AddRange(configBuilder);
        }

        public void AddWorker(params ActorSystemConfigBuilder[] configBuilder)
        {
            _workerNodes.AddRange(configBuilder);
        }

        public ClusterConfig OnClusterUp(Func<ActorSystem, Task> sys)
        {
            _onMemberUp += sys;
            return this;
        }

        public ClusterConfig AdditionalInit(Func<ActorSystem, Task> sys)
        {
            _additionalInit += sys;
            return this;
        }

        public async Task<ClusterInfo> Create()
        {
            var actorSystemBuilders = SeedNodes.Concat(WorkerNodes)
                                               .Concat(AutoSeedNodes)
                                               .ToArray();

            foreach (var cfg in SeedNodes.Concat(AutoSeedNodes))
            {
                cfg.Add(new MinMembersInCluster(actorSystemBuilders.Length));
            }


            Func<ActorSystem, Task<ActorSystem>> init = (async s =>
                                                         {
                                                             await _additionalInit(s);
                                                             return s;
                                                         });


            var seedSystems = await CreateSystems(SeedNodes, init);
            var seedSystemAddresses = seedSystems.Select(s => s.GetAddress())
                                                 .ToArray();

            var autoSeedSystems = await CreateSystems(AutoSeedNodes, init);
            var autoSeedAddresses = autoSeedSystems.Select(s => s.GetAddress())
                                                   .ToArray();

            var workerSystems = await CreateSystems(WorkerNodes, init);
            var workerSystemAddresses = workerSystems.Select(s => s.GetAddress())
                                                     .ToArray();

            var allActorSystems = seedSystems.Concat(autoSeedSystems)
                                             .Concat(workerSystems);

            var leader = seedSystems.FirstOrDefault() ?? throw new CannotDetermineLeaderException();

            bool clusterReady = false;

            var akkaCluster = Akka.Cluster.Cluster.Get(leader);
            akkaCluster.RegisterOnMemberUp(async () =>
                                           {
                                               foreach (var systemBuilder in allActorSystems)
                                               {
                                                   await _onMemberUp(systemBuilder);
                                               }

                                               clusterReady = true;
                                           });

            foreach (var address in autoSeedAddresses)
                await akkaCluster.JoinSeedNodesAsync(new[] {address});

            foreach (var address in workerSystemAddresses)
                await akkaCluster.JoinAsync(address);

            foreach (var systemn in allActorSystems)
            {
                Logger.Information(systemn.Settings.ToString());
            }

            while (!clusterReady)
                await Task.Delay(500);

            return new ClusterInfo(akkaCluster,
                                   seedSystemAddresses.Concat(autoSeedAddresses)
                                                      .Concat(workerSystemAddresses)
                                                      .ToArray(),Logger);
        }

        private async Task<ActorSystem[]> CreateSystems(IReadOnlyCollection<ActorSystemConfigBuilder> actorSystemBuilders, Func<ActorSystem, Task<ActorSystem>> init)
        {
            var systems = actorSystemBuilders.Select(s => s.BuildClusterSystemFactory(Name)
                                                           .CreateSystem())
                                             .ToArray();
            foreach (var s in systems)
                await init(s);

            return systems;
        }
    }
}