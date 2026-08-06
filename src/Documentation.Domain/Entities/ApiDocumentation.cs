namespace Documentation.Domain.Entities;

public sealed class ApiDocumentation
{
    private ApiDocumentation()
    {
        // Required by Entity Framework Core.
    }

    public ApiDocumentation(Guid id, string apiId, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ApiId = apiId;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string ApiId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<DocumentationVersion> Versions { get; private set; } = new List<DocumentationVersion>();

    public void AddVersion(DocumentationVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        Versions.Add(version);
    }
}
