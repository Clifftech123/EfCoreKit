namespace EfCoreKit.Sample.WebApi.Application.DTOs;

// AuthorName is resolved from the authenticated User — not sent by the client
public sealed record CreateCommentRequest(string Body);

public sealed record CommentResponse(
    int Id,
    string Body,
    Guid UserId,
    string AuthorName,   // populated from User.FullName
    int PostId,
    DateTime CreatedAt,
    string? CreatedBy);
