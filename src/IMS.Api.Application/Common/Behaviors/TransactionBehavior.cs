using IMS.Api.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Api.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that wraps write operations in database transactions
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only wrap commands (write operations) in transactions, not queries
        var isCommand = typeof(TRequest).Name.EndsWith("Command");
        
        if (!isCommand)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Starting transaction for {RequestName}", requestName);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var response = await next();
            
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Transaction committed for {RequestName}", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}