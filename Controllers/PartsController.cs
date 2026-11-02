using AutoAgent.Api.Data;
using AutoAgent.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsController(AutoAgentDbContext db) : ControllerBase
{
    // GET /api/parts?search=brake&modelName=Summit
    [HttpGet]
    public async Task<ActionResult<List<PartDto>>> Search(
        [FromQuery] string? search, [FromQuery] string? modelName, CancellationToken ct)
    {
        var query = db.Parts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            var modelId = await db.CarModels
                .Where(m => m.ModelName == modelName)
                .Select(m => (int?)m.ModelId)
                .FirstOrDefaultAsync(ct);

            if (modelId is null) return NotFound(new { message = $"No model named '{modelName}'." });

            // Same NULL rule as financing: a part with no model fits every model.
            query = query.Where(p => p.ModelId == null || p.ModelId == modelId);
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.PartName.Contains(search) || p.PartNumber.Contains(search));

        var parts = await query
            .OrderBy(p => p.PartName)
            .Select(p => new
            {
                p.PartNumber, p.PartName,
                FitsModel = p.Model == null ? "All models" : p.Model.ModelName,
                p.Price, p.QtyInStock, p.RestockDays
            })
            .ToListAsync(ct);

        return parts.Select(p => new PartDto(
            p.PartNumber, p.PartName, p.FitsModel, p.Price, p.QtyInStock,
            p.QtyInStock > 0
                ? "In stock"
                : $"Out of stock, arrives in about {p.RestockDays} days")).ToList();
    }
}
