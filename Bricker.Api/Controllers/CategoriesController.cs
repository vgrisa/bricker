using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(BrickerDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> Get(CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Slug))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
