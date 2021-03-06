using System.Threading.Tasks;
using GridDomain.Abstractions;

namespace GridDomain.EventHandlers
{
    public interface IEventHandler<TEvent>
    {
        Task Handle(Sequenced<TEvent>[] evt);
    }

    public interface IEventHandlersDomainBuilder : IDomainPartBuilder
    {
        void RegisterEventHandler<TEvent, THandler>(string name = null, string nodeRole = null)
            where THandler : IEventHandler<TEvent> where TEvent : class;

        void RegisterEventHandler<TEventA, TEventB, THandler>(string name = null, string nodeRole = null)
            where THandler : IEventHandler<TEventA>, IEventHandler<TEventB>
            where TEventA : class
            where TEventB : class;
    }
}