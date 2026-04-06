using EfCoreKit.Exceptions;
using EfCoreKit.Sample.WebApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EfCoreKit.Sample.WebApi.Controllers;

/// <summary>
/// The "full-featured" controller — exercises every EfCoreKit concept:
/// <list type="bullet">
///   <item><b>Repository pattern</b>  — <see cref="IRepository{T}"/> for CRUD</item>
///   <item><b>Unit of Work</b>        — <see cref="IUnitOfWork"/> for multi-entity transactions</item>
///   <item><b>Specifications</b>      — typed classes + <see cref="SpecificationBuilder{T}"/> inline</item>
///   <item><b>Pagination</b>          — <c>ToPagedAsync</c> for offset paging</item>
///   <item><b>Soft delete / restore</b> — IgnoreQueryFilters to find &amp; restore deleted rows</item>
///   <item><b>Multi-tenancy</b>       — TenantId automatically scoped via X-Tenant-Id header</item>
///   <item><b>Optimistic concurrency</b> — RowVersion / ConcurrencyConflictException</item>
///   <item><b>Spec combinators</b>    — <c>.And()</c> to compose two specs</item>
/// </list>
/// </summary>
// Require a valid JWT on all post endpoints.
// The token's tenant_id claim is what HttpContextTenantProvider reads —
// so every query is automatically scoped to the caller's tenant.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IRepository<Post> _postRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _context;

    public PostsController(
        IRepository<Post> postRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        AppDbContext context)
    {
        _postRepo = postRepo;
        _uow = uow;
        _currentUser = currentUser;
        _context = context;
    }

    // ── GET /api/posts?page=1&pageSize=10 ──────────────────────────────────────
    // Uses SpecificationBuilder inline for optional category filtering + ordering.
    // Uses ToPagedAsync on the filtered IQueryable for efficient offset pagination.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? categoryId = null,
        CancellationToken ct = default)
    {
        var query = _context.Posts
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .AsNoTracking();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var paged = await query.ToPagedAsync(page, pageSize, ct);
        return Ok(paged.Map(ToSummaryResponse));
    }

    // ── GET /api/posts/published ───────────────────────────────────────────────
    // Demonstrates a typed Specification class.
    [HttpGet("published")]
    public async Task<IActionResult> GetPublished(CancellationToken ct)
    {
        var spec = new PublishedPostsSpecification();
        var posts = await _postRepo.FindAsync(spec, ct);
        return Ok(posts.Select(ToSummaryResponse));
    }

    // ── GET /api/posts/published/category/{categoryId} ────────────────────────
    // Demonstrates the .And() specification combinator.
    [HttpGet("published/category/{categoryId:int}")]
    public async Task<IActionResult> GetPublishedByCategory(int categoryId, CancellationToken ct)
    {
        // Compose two specifications with the And() combinator
        var spec = new PublishedPostsSpecification()
            .And(new PostsByCategorySpecification(categoryId));

        var posts = await _postRepo.FindAsync(spec, ct);
        return Ok(posts.Select(ToSummaryResponse));
    }

    // ── GET /api/posts/recent?days=7 ──────────────────────────────────────────
    // Demonstrates a parameterised Specification.
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var spec = new RecentPostsSpecification(days);
        var posts = await _postRepo.FindAsync(spec, ct);
        return Ok(posts.Select(ToSummaryResponse));
    }

    // ── GET /api/posts/{id} ────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var post = await _context.Posts
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post is null)
            return NotFound(new { error = $"Post {id} not found." });

        return Ok(ToDetailResponse(post));
    }

    // ── POST /api/posts ────────────────────────────────────────────────────────
    // Demonstrates IUnitOfWork: Post and PostTag entries are written atomically
    // in a single CommitAsync call.
    // TenantInterceptor automatically stamps TenantId from the X-Tenant-Id header.
    // AuditInterceptor automatically stamps CreatedAt/CreatedBy.
    [HttpPost]
    public async Task<IActionResult> Create(CreatePostRequest request, CancellationToken ct)
    {
        var postRepo = _uow.Repository<Post>();
        var postTagRepo = _uow.Repository<PostTag>();

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            Slug = request.Slug,
            CategoryId = request.CategoryId,
            IsPublished = false,
            UserId = _currentUser.GetUserId()!.Value   // guaranteed by [Authorize]
        };

        // EF Core resolves FK order automatically — PostTags are saved after Post gets its Id
        foreach (var tagId in request.TagIds.Distinct())
            post.PostTags.Add(new PostTag { TagId = tagId });

        await postRepo.AddAsync(post, ct);
        await _uow.CommitAsync(ct); // single atomic commit for post + all tags

        // Reload for the response (includes navigations)
        var created = await _context.Posts
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .FirstAsync(p => p.Id == post.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, ToDetailResponse(created));
    }

    // ── PUT /api/posts/{id} ────────────────────────────────────────────────────
    // Demonstrates optimistic concurrency with RowVersion.
    // The client sends back the RowVersion it received on the last GET.
    // If another update occurred in between, EfCoreKit raises
    // ConcurrencyConflictException which the global handler maps to HTTP 409.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePostRequest request, CancellationToken ct)
    {
        var post = await _postRepo.GetByIdOrThrowAsync(id, ct);

        // Override the tracked RowVersion with the client's original value so EF Core
        // includes it in the WHERE clause and detects stale updates.
        _context.Entry(post).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        post.Title = request.Title;
        post.Content = request.Content;
        post.IsPublished = request.IsPublished;

        _postRepo.Update(post);
        // Throws ConcurrencyConflictException if DB RowVersion != request.RowVersion
        await _postRepo.SaveChangesAsync(ct);

        return Ok(ToDetailResponse(post));
    }

    // ── DELETE /api/posts/{id} ─────────────────────────────────────────────────
    // SoftDeleteInterceptor sets IsDeleted=true, DeletedAt, DeletedBy instead of
    // issuing a DELETE statement. The global query filter hides it from future queries.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var post = await _postRepo.GetByIdOrThrowAsync(id, ct);
        _postRepo.Remove(post);
        await _postRepo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── POST /api/posts/{id}/restore ──────────────────────────────────────────
    // Demonstrates IgnoreQueryFilters() to bypass the soft-delete global filter
    // and restore a logically-deleted entity.
    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, ct);

        if (post is null)
            return NotFound(new { error = $"No soft-deleted post found with ID {id}." });

        post.IsDeleted = false;
        post.DeletedAt = null;
        post.DeletedBy = null;

        await _context.SaveChangesAsync(ct);
        return Ok(new { message = $"Post {id} restored successfully." });
    }

    // ── POST /api/posts/{id}/publish ──────────────────────────────────────────
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> TogglePublish(int id, CancellationToken ct)
    {
        var post = await _postRepo.GetByIdOrThrowAsync(id, ct);
        post.IsPublished = !post.IsPublished;
        _postRepo.Update(post);
        await _postRepo.SaveChangesAsync(ct);
        return Ok(new { id = post.Id, isPublished = post.IsPublished });
    }

    // ── Mapping helpers ────────────────────────────────────────────────────────

    private static PostSummaryResponse ToSummaryResponse(Post p) => new(
        p.Id, p.Title, p.Slug, p.IsPublished,
        p.Category?.Name, p.CreatedAt, p.TenantId);

    private static PostResponse ToDetailResponse(Post p) => new(
        p.Id, p.Title, p.Content, p.Slug, p.IsPublished,
        p.CategoryId, p.Category?.Name,
        p.PostTags.Select(pt => new TagResponse(pt.TagId, pt.Tag?.Name ?? "")).ToList(),
        p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy,
        p.TenantId, p.RowVersion);
}
