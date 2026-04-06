using System.Security.Claims;
using EfCoreKit.Sample.WebApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EfCoreKit.Sample.WebApi.Controllers;

/// <summary>
/// Nested resource under posts.
/// Demonstrates IUnitOfWork for a multi-repo operation: verify the parent Post
/// exists and create the Comment atomically in one CommitAsync call.
/// Comments use AuditableEntity — audit fields only, no soft-delete.
/// The authenticated user (from JWT) is set as the comment author.
/// </summary>
[Authorize]
[ApiController]
[Route("api/posts/{postId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _context;

    public CommentsController(IUnitOfWork uow, ICurrentUserService currentUser, AppDbContext context)
    {
        _uow = uow;
        _currentUser = currentUser;
        _context = context;
    }

    // GET /api/posts/{postId}/comments
    [HttpGet]
    public async Task<IActionResult> GetAll(int postId, CancellationToken ct)
    {
        var postExists = await _context.Posts.AnyAsync(p => p.Id == postId, ct);
        if (!postExists) return NotFound(new { error = $"Post {postId} not found." });

        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(comments.Select(ToResponse));
    }

    // GET /api/posts/{postId}/comments/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int postId, int id, CancellationToken ct)
    {
        var comment = await _context.Comments
            .Include(c => c.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.PostId == postId, ct);

        if (comment is null)
            return NotFound(new { error = $"Comment {id} on post {postId} not found." });

        return Ok(ToResponse(comment));
    }

    // POST /api/posts/{postId}/comments
    // IUnitOfWork: verify post + create comment in one atomic commit.
    // UserId is taken from the JWT — the client never sends it.
    [HttpPost]
    public async Task<IActionResult> Create(int postId, CreateCommentRequest request, CancellationToken ct)
    {
        var postRepo = _uow.Repository<Post>();
        var commentRepo = _uow.Repository<Comment>();

        // Throws EntityNotFoundException → 404 if post doesn't exist
        await postRepo.GetByIdOrThrowAsync(postId, ct);

        var comment = new Comment
        {
            Body = request.Body,
            PostId = postId,
            UserId = _currentUser.GetUserId()!.Value  // guaranteed by [Authorize]
        };

        await commentRepo.AddAsync(comment, ct);
        await _uow.CommitAsync(ct);

        // Reload with User for the response
        var created = await _context.Comments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id, ct);

        return CreatedAtAction(nameof(GetById), new { postId, id = comment.Id }, ToResponse(created));
    }

    // DELETE /api/posts/{postId}/comments/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int postId, int id, CancellationToken ct)
    {
        var commentRepo = _uow.Repository<Comment>();

        var comment = await commentRepo.FirstOrDefaultAsync(
            c => c.Id == id && c.PostId == postId, ct);

        if (comment is null)
            return NotFound(new { error = $"Comment {id} on post {postId} not found." });

        commentRepo.Remove(comment);
        await _uow.CommitAsync(ct);
        return NoContent();
    }

    private static CommentResponse ToResponse(Comment c) =>
        new(c.Id, c.Body, c.UserId, c.User?.FullName ?? "", c.PostId, c.CreatedAt, c.CreatedBy);
}
