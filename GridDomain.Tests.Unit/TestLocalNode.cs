using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.State;
using GridDomain.Tests.Common;
using GridDomain.Transport;
using Serilog;

namespace GridDomain.Tests.Unit {
    public class TestLocalNode : ITestGridDomainNode
    {
        private TestKit _testKit;
        public IExtendedGridDomainNode Node { get; }

        public TestLocalNode(IExtendedGridDomainNode node, TestKit kit)
        {
            _testKit = kit;
            Node = node;
        }

        public Task Execute<T>(T command, IMessageMetadata metadata = null, CommandConfirmationMode confirm = CommandConfirmationMode.Projected) where T : ICommand
        {
            return Node.Execute(command, metadata, confirm);
        }

        public ICommandExpectationBuilder Prepare<U>(U cmd, IMessageMetadata metadata = null) where U : ICommand
        {
            return Node.Prepare(cmd, metadata);
        }

        public IMessageWaiter<Task<IWaitResult>> NewExplicitWaiter(TimeSpan? defaultTimeout = null)
        {
            return Node.NewExplicitWaiter(defaultTimeout);
        }

        public IMessageWaiter<Task<IWaitResult>> NewWaiter(TimeSpan? defaultTimeout = null)
        {
            return Node.NewWaiter(defaultTimeout);
        }

        public void Dispose()
        {
            Node.Dispose();
        }

        public ActorSystem System => Node.System;

        public TimeSpan DefaultTimeout => Node.DefaultTimeout;
        public IActorTransport Transport => Node.Transport;

        public IActorCommandPipe Pipe => Node.Pipe;
        public Task Start()
        {
            return Node.Start();
        }

        public Task Stop()
        {
            return Node.Stop();
        }

        public ILogger Log => Node.Log;

        public EventsAdaptersCatalog EventsAdaptersCatalog => Node.EventsAdaptersCatalog;

        public async Task<T> LoadAggregateByActor<T>(string id) where T : Aggregate
        {
            var name = EntityActorName.New<T>(id)
                                      .ToString();
            var actor = await _testKit.LoadActor<AggregateActor<T>>(name);

            return actor.State;
        }
                                                                  
        public async Task<TState> LoadProcess<TState>(string id) where TState : class, IProcessState
        {
            return (await LoadAggregateByActor<ProcessStateAggregate<TState>>(id)).State;
        }

        public IProcessManagerExpectationBuilder PrepareForProcessManager(DomainEvent msg, MessageMetadata metadata = null)
        {
            //var res = await NewLocalDebugWaiter(Node, timeout)
            //                .Expect<TExpect>()
            //                .Create()
            //                .SendToProcessManagers(msg);
            //
            //return res.Message<TExpect>();
            throw new NotImplementedException();

        }

        public IProcessManagerExpectationBuilder PrepareForProcessManager(IFault msg, MessageMetadata metadata = null)
        {
            throw new NotImplementedException();

        }

        class ProcessManagerExpectationBuilder : IProcessManagerExpectationBuilder
        {
            private IExtendedGridDomainNode _extendedGridDomainNode;

            public ProcessManagerExpectationBuilder(IExtendedGridDomainNode node)
            {
                _extendedGridDomainNode = node;
            }
            public IConditionedProcessManagerSender<TMsg> Expect<TMsg>(Predicate<TMsg> filter = null) where TMsg : class
            {
                var waiter = NewLocalDebugWaiter(_extendedGridDomainNode);
                throw new NotImplementedException();
            }

            public Task<IWaitResult> Send(TimeSpan? timeout = null, bool failOnAnyFault = true)
            {
                throw new NotImplementedException();
            }

            class ConditionedProcessManagerSender<T> : IConditionedProcessManagerSender<T>
            {
                private ConditionFactory<Task<IWaitResult>> _conditionFactory;
                private object _msg;

                public ConditionedProcessManagerSender(object msg, ConditionFactory<Task<IWaitResult>> conditionFactory)
                {
                    _msg = msg;
                    _conditionFactory = conditionFactory;
                }
                public IConditionedProcessManagerSender<T> And<TMsg>(Predicate<TMsg> filter = null) where TMsg : class
                {
                    _conditionFactory.And(filter);
                    return this;
                }

                public IConditionedProcessManagerSender<T> Or<TMsg>(Predicate<TMsg> filter = null) where TMsg : class
                {
                    _conditionFactory.Or(filter);
                    return this;
                }

                public IReadOnlyCollection<Type> KnownMessageTypes { get; }
                public bool Check(params object[] messages)
                {
                    throw new NotImplementedException();
                }

                public Task<IWaitResult<T>> Send(TimeSpan? timeout = null, bool failOnAnyFault = true)
                {
                    var task = _conditionFactory.Create(timeout);
                    throw new NotImplementedException();

//                    //will wait later in task; 
//#pragma warning disable 4014
//                    _executorActorRef.Execute(_command, _commandMetadata, CommandConfirmationMode.None);
//#pragma warning restore 4014
//
//                    var res = await task;
//
//                    if (!failOnAnyFault)
//                        return res;
//                    
//                    var faults = res.All.OfType<IMessageMetadataEnvelop>()
//                                    .Select(env => env.Message)
//                                    .OfType<IFault>()
//                                    .ToArray();
//                    if (faults.Any())
//                        throw new AggregateException(faults.Select(f => f.Exception));
//
//                    return task;
                }
            }
        }
        
//        public IProcessManagerExpectationBuilder PrepareForProcessManager(object msg, MessageMetadata metadata = null)
//        {
//            var res = await NewLocalDebugWaiter(Node)
//                            .Expect<TExpect>()
//                            .Create()
//                            .SendToProcessManagers(msg);
//           
//            return res.Message<TExpect>();
//            throw new NotImplementedException();
//
//        }

        public IMessageWaiter<AnyMessagePublisher> NewTestWaiter(TimeSpan? timeout = null)
        {
            return NewLocalDebugWaiter(Node, timeout);
        }

        static IMessageWaiter<AnyMessagePublisher> NewLocalDebugWaiter(IExtendedGridDomainNode node, TimeSpan? timeout = null)
        {
            var conditionBuilder = new LocalMetadataConditionFactory<AnyMessagePublisher>();
            var waiter = new MessagesWaiter<AnyMessagePublisher>(node.System, node.Transport, timeout ?? node.DefaultTimeout, conditionBuilder);
            conditionBuilder.CreateResultFunc = t => new AnyMessagePublisher(node.Pipe, waiter);
            return waiter;
        }
    }
}