namespace EfCoreKit.Sample.WebApi.Application.DTOs;

public sealed record CreateTagRequest(string Name);

public sealed record UpdateTagRequest(string Name);

public sealed record TagResponse(int Id, string Name);
