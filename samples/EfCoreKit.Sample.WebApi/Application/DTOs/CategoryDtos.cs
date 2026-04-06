namespace EfCoreKit.Sample.WebApi.Application.DTOs;

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record UpdateCategoryRequest(string Name, string? Description);

public sealed record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    // Audit fields — populated automatically by AuditInterceptor
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);
