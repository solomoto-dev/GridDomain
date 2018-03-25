using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;

namespace GridDomain.Node.AkkaMessaging.Waiting
{

    public class CommandConditionBuilder<TCommand> : ICommandConditionBuilder where TCommand : ICommand
    {
        private readonly TCommand _command;
        private readonly IMessageMetadata _commandMetadata;
        private readonly ICommandExecutor _executorActorRef;
        public readonly ConditionBuilder<Task<IWaitResult>> ConditionaBuilder;

        public CommandConditionBuilder(TCommand command,
                                       IMessageMetadata commandMetadata,
                                       ICommandExecutor executorActorRef,
                                       ConditionBuilder<Task<IWaitResult>> conditionaBuilder = null)
        {
            ConditionaBuilder = conditionaBuilder ?? new LocalCorrelationConditionBuilder<Task<IWaitResult>>(commandMetadata.CorrelationId);
            _commandMetadata = commandMetadata;
            _executorActorRef = executorActorRef;
            _command = command;
        }

        public async Task<IWaitResult> Execute(TimeSpan? timeout = null, bool failOnAnyFault = true)
        {
            //we can launch any command with CommandConditionBuilder<ICommand>
            //and TCommand will resolve to ICommand leading to incorect subscription
            if(failOnAnyFault)
                ConditionaBuilder.Or(Fault.TypeFor(_command), IsFaultFromExecutingCommand);

            var task = ConditionaBuilder.Create(timeout);

            //will wait later in task; 
#pragma warning disable 4014
            _executorActorRef.Execute(_command, _commandMetadata,CommandConfirmationMode.None);
#pragma warning restore 4014

            var res = await task;

            if (!failOnAnyFault)
                return res;
            var faults = res.All.OfType<IMessageMetadataEnvelop>().Select(env => env.Message).OfType<IFault>().ToArray();
            if (faults.Any())
                throw new AggregateException(faults.Select(f => f.Exception));

            return res;
        }

        private bool IsFaultFromExecutingCommand(object o)
        {
            return (((o as IMessageMetadataEnvelop)?.Message as IFault)?.Message as ICommand)?.Id == _command.Id;
        }

        public ICommandConditionBuilder And<TMsg>(Predicate<TMsg> filter) where TMsg : class
        {
            ConditionaBuilder.And(filter);
            return this;
        }

        public ICommandConditionBuilder Or<TMsg>(Predicate<TMsg> filter) where TMsg : class
        {
            ConditionaBuilder.Or(filter);
            return this;
        }
    }
}