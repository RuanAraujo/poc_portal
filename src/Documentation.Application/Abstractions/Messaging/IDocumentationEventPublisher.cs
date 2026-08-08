using Documentation.Contracts;

namespace Documentation.Application.Abstractions.Messaging;

public interface IDocumentationEventPublisher
{
    Task PublishAsync(DocumentationPublished integrationEvent, CancellationToken cancellationToken = default);
}
