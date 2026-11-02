using AutoAgent.Api.Data;
using AutoAgent.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Controllers;

[ApiController]
[Route("api/financing")]
public class FinancingController(AutoAgentDbContext db) : ControllerBase
{
    // GET /api/financing?modelName=Volt-X
    // Returns offers for that model PLUS offers for all models (ModelId IS NULL).
    // This is the homework #2 query, done correctly.
    [HttpGet]
    public async Task<ActionResult<List<FinancingOfferDto>>> GetOffers(
        [FromQuery] string? modelName, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = db.FinancingOffers.AsNoTracking().Where(o => o.ValidUntil >= today);

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            var modelId = await db.CarModels
                .Where(m => m.ModelName == modelName)
                .Select(m => (int?)m.ModelId)
                .FirstOrDefaultAsync(ct);

            if (modelId is null) return NotFound(new { message = $"No model named '{modelName}'." });

            query = query.Where(o => o.ModelId == null || o.ModelId == modelId);
        }

        return await query
            .OrderBy(o => o.TermMonths).ThenBy(o => o.AprPercent)
            .Select(o => new FinancingOfferDto(
                o.OfferId,
                o.Model == null ? "All models" : o.Model.ModelName,
                o.TermMonths, o.AprPercent, o.MinDownPercent, o.MinCreditScore, o.ValidUntil))
            .ToListAsync(ct);
    }

    // GET /api/financing/estimate?vin=1LKBRZ26A00000002&offerId=2&downPayment=5000
    // LLMs are unreliable at arithmetic, so the math lives HERE, in deterministic code.
    [HttpGet("estimate")]
    public async Task<ActionResult<PaymentEstimateDto>> Estimate(
        [FromQuery] string vin, [FromQuery] int offerId, [FromQuery] decimal? downPayment,
        CancellationToken ct)
    {
        var vehicle = await db.Inventory.AsNoTracking()
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Vin == vin && v.Status != "Sold", ct);
        if (vehicle is null) return NotFound(new { message = "That car isn't in our available inventory." });

        var today = DateOnly.FromDateTime(DateTime.Today);
        var offer = await db.FinancingOffers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OfferId == offerId && o.ValidUntil >= today, ct);
        if (offer is null) return NotFound(new { message = "That financing offer doesn't exist or has expired." });

        if (offer.ModelId is not null && offer.ModelId != vehicle.ModelId)
            return BadRequest(new { message = $"Offer {offerId} doesn't apply to the {vehicle.Model.ModelName}." });

        var price = vehicle.ListPrice;
        var minDown = Math.Round(price * offer.MinDownPercent / 100m, 2);
        var down = downPayment ?? minDown;

        if (down < minDown)
            return BadRequest(new { message = $"Minimum down payment for this offer is {minDown:C} ({offer.MinDownPercent}%)." });
        if (down >= price)
            return BadRequest(new { message = "Down payment covers the full price, so no financing is needed." });

        var financed = price - down;
        var n = offer.TermMonths;
        var r = (double)offer.AprPercent / 100.0 / 12.0;   // monthly interest rate

        // Standard loan payment formula: P * r / (1 - (1 + r)^-n)
        var monthly = r == 0
            ? financed / n
            : (decimal)((double)financed * r / (1 - Math.Pow(1 + r, -n)));
        monthly = Math.Round(monthly, 2);

        var total = monthly * n;

        return new PaymentEstimateDto(
            vehicle.Vin, vehicle.Model.ModelName, price, offer.OfferId, n, offer.AprPercent,
            down, minDown, financed, monthly, total, total - financed,
            "Estimate only. Final APR depends on credit approval. Taxes, title and fees not included.");
    }
}
