namespace EfCoreKit.Sample.WebApi.Controllers;

/// <summary>
/// Demonstrates direct use of <see cref="IRepository{T}"/> for simple CRUD.
/// Tags use the minimal <see cref="BaseEntity"/> — no audit, no soft-delete.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly IRepository<Tag> _repo;

    public TagsController(IRepository<Tag> repo) => _repo = repo;

    // GET /api/tags
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tags = await _repo.GetAllAsync(ct);
        var response = tags.Select(t => new TagResponse(t.Id, t.Name));
        return Ok(response);
    }

    // GET /api/tags/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        // GetByIdOrThrowAsync raises EntityNotFoundException → global handler returns 404
        var tag = await _repo.GetByIdOrThrowAsync(id, ct);
        return Ok(new TagResponse(tag.Id, tag.Name));
    }

    // GET /api/tags/exists?name=csharp
    [HttpGet("exists")]
    public async Task<IActionResult> Exists([FromQuery] string name, CancellationToken ct)
    {
        // Demonstrates ExistsAsync with a predicate expression
        var exists = await _repo.ExistsAsync(t => t.Name == name, ct);
        return Ok(new { exists, name });
    }

    // GET /api/tags/count
    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _repo.CountAsync(ct: ct);
        return Ok(new { count });
    }

    // POST /api/tags
    [HttpPost]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken ct)
    {
        var tag = new Tag { Name = request.Name };
        await _repo.AddAsync(tag, ct);
        await _repo.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = tag.Id }, new TagResponse(tag.Id, tag.Name));
    }

    // PUT /api/tags/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTagRequest request, CancellationToken ct)
    {
        var tag = await _repo.GetByIdOrThrowAsync(id, ct);
        tag.Name = request.Name;
        _repo.Update(tag);
        await _repo.SaveChangesAsync(ct);
        return Ok(new TagResponse(tag.Id, tag.Name));
    }

    // DELETE /api/tags/{id}  — hard delete (Tag has no soft-delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.RemoveByIdAsync(id, ct);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }
}
