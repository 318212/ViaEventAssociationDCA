using System.Windows.Input;
using ViaEventAssociation.Core.Tools.OperationResult;

namespace Application.AppEntry.CommandDispatching;

public class CommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result> Dispatch<TCommand>(TCommand command) where TCommand : ICommand
    {
        Type serviceType = typeof(ICommandHandler<TCommand>);
        var service = _serviceProvider.GetService(serviceType);

        if (service == null)
        {
            throw new InvalidOperationException($"Handle for {typeof(TCommand).Name} not found");
        }

        ICommandHandler<TCommand> handler = (ICommandHandler<TCommand>)service;
        return handler.Handle(command);
    }
}