using AutoAgent.Api.Data;
using AutoAgent.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController(AutoAgentDbContext db) : ControllerBase
{
    // GET /api/cars/models  -> every model the company produces
    [HttpGet("models")]
    public async Task<ActionResult<List<CarModelDto>>> GetModels(CancellationToken ct)
    {
        return await db.CarModels.AsNoTracking()
            .OrderBy(m => m.BasePrice)
            .Select(m => ToDto(m, m.Vehicles.Count(v => v.Status == "Available")))
            .ToListAsync(ct);
    }

    // GET /api/cars/models/Summit
    [HttpGet("models/{modelName}")]
    public async Task<ActionResult<CarModelDto>> GetModel(string modelName, CancellationToken ct)
    {
        var model = await db.CarModels.AsNoTracking()
            .Where(m => m.ModelName == modelName)
            .Select(m => ToDto(m, m.Vehicles.Count(v => v.Status == "Available")))
            .FirstOrDefaultAsync(ct);

        return model is null ? NotFound(new { message = $"No model named '{modelName}'." }) : model;
    }

    // GET /api/cars/inventory?maxPrice=45000&condition=New&minSeats=5
    // Only AVAILABLE cars are ever returned. This is the fix from your Lesson 1 query.
    [HttpGet("inventory")]
    public async Task<ActionResult<List<VehicleDto>>> SearchInventory(
        [FromQuery] InventorySearch q, CancellationToken ct)
    {
        var query = db.Inventory.AsNoTracking().Where(v => v.Status == "Available");

        if (!string.IsNullOrWhiteSpace(q.ModelName)) query = query.Where(v => v.Model.ModelName == q.ModelName);
        if (!string.IsNullOrWhiteSpace(q.BodyType))  query = query.Where(v => v.Model.BodyType == q.BodyType);
        if (!string.IsNullOrWhiteSpace(q.FuelType))  query = query.Where(v => v.Model.FuelType == q.FuelType);
        if (!string.IsNullOrWhiteSpace(q.Condition)) query = query.Where(v => v.Condition == q.Condition);
        if (!string.IsNullOrWhiteSpace(q.Color))     query = query.Where(v => v.Color.Contains(q.Color));
        if (q.MinPrice is not null) query = query.Where(v => v.ListPrice >= q.MinPrice);
        if (q.MaxPrice is not null) query = query.Where(v => v.ListPrice <= q.MaxPrice);
        if (q.MinSeats is not null) query = query.Where(v => v.Model.Seats >= q.MinSeats);

        return await query
            .OrderBy(v => v.ListPrice)
            .Select(v => new VehicleDto(
                v.Vin, v.Model.ModelName, v.Model.ModelYear, v.Model.BodyType, v.Model.FuelType,
                v.Model.Seats, v.Trim, v.Color, v.Mileage, v.Condition, v.ListPrice, v.Status))
            .ToListAsync(ct);
    }

    // GET /api/cars/inventory/1LKBRZ26A00000002
    // Sold cars return 404: once sold, that car's details belong to its owner.
    [HttpGet("inventory/{vin}")]
    public async Task<ActionResult<VehicleDto>> GetVehicle(string vin, CancellationToken ct)
    {
        var vehicle = await db.Inventory.AsNoTracking()
            .Where(v => v.Vin == vin && v.Status != "Sold")
            .Select(v => new VehicleDto(
                v.Vin, v.Model.ModelName, v.Model.ModelYear, v.Model.BodyType, v.Model.FuelType,
                v.Model.Seats, v.Trim, v.Color, v.Mileage, v.Condition, v.ListPrice, v.Status))
            .FirstOrDefaultAsync(ct);

        return vehicle is null ? NotFound(new { message = "That car isn't in our available inventory." }) : vehicle;
    }

    private static CarModelDto ToDto(CarModel m, int availableCount) => new(
        m.ModelName, m.ModelYear, m.BodyType, m.FuelType, m.Engine, m.Transmission,
        m.Seats, m.RangeOrMpg, m.BasePrice, m.Description,
        m.LeadTimeWeeks, m.FreeServiceMonths, availableCount);
}
