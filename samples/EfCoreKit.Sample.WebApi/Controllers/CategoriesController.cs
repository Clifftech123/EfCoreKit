namespace EfCoreKit.Sample.WebApi.Controllers;

/// <summary>
/// Demonstrates <see cref="IRepository{T}"/> with a <see cref="SoftDeletableEntity"/>.
/// DELETE does a logical delete (sets IsDeleted = true), not a hard SQL DELETE.
/// The soft-delete global query filter automatically hides deleted rows from all queries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IRepository<Category> _repo;

    public CategoriesController(IRepository<Category> repo) => _repo = repo;

    // GET /api/categories?page=1&pageSize=10
    // Demonstrates offset-based pagination via GetPagedAsync → PagedResult<T>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var paged = await _repo.GetPagedAsync(page, pageSize, ct);
        var response = paged.Map(c => new CategoryResponse(
            c.Id, c.Name, c.Description,
            c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.UpdatedBy));
        return Ok(response);
    }

    // GET /api/categories/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var cat = await _repo.GetByIdOrThrowAsync(id, ct);
        return Ok(ToResponse(cat));
    }

    // GET /api/categories/search?name=tech
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name, CancellationToken ct)
    {
        // Demonstrates FindAsync with a predicate
        var cats = await _repo.FindAsync(c => c.Name.Contains(name), ct);
        return Ok(cats.Select(ToResponse));
    }

    // GET /api/categories/count
    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var total = await _repo.CountAsync(ct: ct);
        return Ok(new { total });
    }

    // POST /api/categories
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new Category { Name = request.Name, Description = request.Description };
        await _repo.AddAsync(category, ct);
        await _repo.SaveChangesAsync(ct);
        // CreatedAt/CreatedBy are now populated by AuditInterceptor
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category));
    }

    // PUT /api/categories/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await _repo.GetByIdOrThrowAsync(id, ct);
        category.Name = request.Name;
        category.Description = request.Description;
        _repo.Update(category);
        await _repo.SaveChangesAsync(ct);
        // UpdatedAt/UpdatedBy are now set by AuditInterceptor
        return Ok(ToResponse(category));
    }

    // DELETE /api/categories/{id}
    // SoftDeleteInterceptor intercepts the Remove call and sets IsDeleted = true instead
    // of issuing a SQL DELETE. The row remains in the database and can be queried
    // using IgnoreQueryFilters() if needed.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var category = await _repo.GetByIdOrThrowAsync(id, ct);
        _repo.Remove(category);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CategoryResponse ToResponse(Category c) =>
        new(c.Id, c.Name, c.Description, c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.UpdatedBy);
}
