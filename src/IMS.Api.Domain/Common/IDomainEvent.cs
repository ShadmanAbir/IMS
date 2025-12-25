namespace IMS.Api.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}