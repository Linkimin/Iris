namespace Iris.Application.Memory.Queries;

public sealed record RetrieveRelevantMemoriesQuery(string Query, int? Limit = null);
