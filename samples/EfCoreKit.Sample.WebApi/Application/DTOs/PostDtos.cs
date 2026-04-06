namespace EfCoreKit.Sample.WebApi.Application.DTOs;

public sealed record CreatePostRequest(
    string Title,
    string Content,
    string Slug,
    int CategoryId,
    List<int> TagIds);

public sealed record UpdatePostRequest(
    string Title,
    string Content,
    bool IsPublished,
    // Client sends back the RowVersion received on the last GET.
    // EfCoreKit raises ConcurrencyConflictException (→ 409) if another
    // update happened in between.
    byte[] RowVersion);

public sealed record PostResponse(
    int Id,
    string Title,
    string Content,
    string Slug,
    bool IsPublished,
    int CategoryId,
    string? CategoryName,
    List<TagResponse> Tags,
    // Audit fields
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    // Multi-tenancy — shows which tenant owns this post
    string? TenantId,
    // Concurrency token — include in PUT requests
    byte[] RowVersion);

public sealed record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    bool IsPublished,
    string? CategoryName,
    DateTime CreatedAt,
    string? TenantId);
