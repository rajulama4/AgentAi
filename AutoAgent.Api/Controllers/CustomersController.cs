using AutoAgent.Api.Data;
using AutoAgent.Api.Dtos;
using AutoAgent.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(AutoAgentDbContext db, CustomerSessionService sessions) : ControllerBase
{
    // POST /api/customers/verify   { "phone": "616-555-0101", "vinLast4": "0006" }
    // The customer proves who they are with two things: their phone AND the
    // last 4 digits of a VIN they bought. On success they get a 30-minute token.
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyResponse>> Verify([FromBody] VerifyRequest request, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(request.Phone ?? "");
        var last4 = (request.VinLast4 ?? "").Trim().ToUpperInvariant();

        if (last4.Length != 4)
            return BadRequest(new { message = "Please provide the last 4 characters of the VIN." });

        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Phone == phone && c.Sales.Any(s => s.Vehicle.Vin.EndsWith(last4)))
            .Select(c => new { c.CustomerId, c.FirstName })
            .FirstOrDefaultAsync(ct);

        // Same message whether the phone or the VIN was wrong, so an attacker
        // can't learn which phone numbers belong to customers.
        if (customer is null)
            return Unauthorized(new { message = "We couldn't verify those details." });

        var (token, expiresAt) = sessions.Create(customer.CustomerId);
        return new VerifyResponse(token, customer.FirstName, expiresAt);
    }

    // GET /api/customers/me/vehicles   (header  X-Customer-Token: <token>)
    // "me" = whoever the token belongs to. There is no customerId parameter
    // the agent could change to read someone else's cars.
    [HttpGet("me/vehicles")]
    public async Task<ActionResult<List<OwnedVehicleDto>>> MyVehicles(
        [FromHeader(Name = "X-Customer-Token")] string? token, CancellationToken ct)
    {
        if (!sessions.TryGetCustomerId(token, out var customerId))
            return Unauthorized(new { message = "Please verify your identity first." });

        var sales = await db.Sales.AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .Include(s => s.Vehicle).ThenInclude(v => v.Model)
            .Include(s => s.Vehicle).ThenInclude(v => v.ServiceRecords)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);

        return sales.Select(s => new OwnedVehicleDto(
            s.Vehicle.Vin,
            s.Vehicle.Model.ModelName,
            s.Vehicle.Model.ModelYear,
            s.Vehicle.Trim,
            s.Vehicle.Color,
            s.SaleDate,
            s.SalePrice,
            s.WarrantyEnds,
            s.SaleDate.AddMonths(s.Vehicle.Model.FreeServiceMonths),
            s.Vehicle.ServiceRecords
                .OrderByDescending(r => r.ServiceDate)
                .Select(r => new ServiceRecordDto(r.ServiceDate, r.Work, r.Cost))
                .ToList()
        )).ToList();
    }
}
