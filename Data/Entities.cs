using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Data;

// These classes map 1:1 to the tables created by the Lesson 1 SQL scripts.
// The database already exists, so we never run migrations (database-first).

[Table("CarModels")]
public class CarModel
{
    [Key] public int ModelId { get; set; }
    public string ModelName { get; set; } = "";
    public int ModelYear { get; set; }
    public string BodyType { get; set; } = "";
    public string FuelType { get; set; } = "";
    public string? Engine { get; set; }
    public string? Transmission { get; set; }
    public int Seats { get; set; }
    public string? RangeOrMpg { get; set; }
    [Precision(10, 2)] public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public int LeadTimeWeeks { get; set; }
    public int FreeServiceMonths { get; set; }

    public List<Vehicle> Vehicles { get; set; } = new();
}

[Table("Inventory")]
public class Vehicle
{
    [Key] public int VehicleId { get; set; }
    [Column("VIN")] public string Vin { get; set; } = "";
    public int ModelId { get; set; }
    [ForeignKey(nameof(ModelId))] public CarModel Model { get; set; } = null!;
    public string Color { get; set; } = "";
    public string Trim { get; set; } = "";
    public int Mileage { get; set; }
    public string Condition { get; set; } = "";      // New / Used
    [Precision(10, 2)] public decimal ListPrice { get; set; }
    public string Status { get; set; } = "";         // Available / Reserved / Sold

    public Sale? Sale { get; set; }
    public List<ServiceRecord> ServiceRecords { get; set; } = new();
}

[Table("Customers")]
public class Customer
{
    [Key] public int CustomerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Sale> Sales { get; set; } = new();
}

[Table("Sales")]
public class Sale
{
    [Key] public int SaleId { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
    public DateOnly SaleDate { get; set; }
    [Precision(10, 2)] public decimal SalePrice { get; set; }
    public DateOnly? WarrantyEnds { get; set; }
}

[Table("ServiceRecords")]
public class ServiceRecord
{
    [Key] public int ServiceId { get; set; }
    public int VehicleId { get; set; }
    [ForeignKey(nameof(VehicleId))] public Vehicle Vehicle { get; set; } = null!;
    public DateOnly ServiceDate { get; set; }
    public string Work { get; set; } = "";
    [Precision(10, 2)] public decimal Cost { get; set; }
}

[Table("FinancingOffers")]
public class FinancingOffer
{
    [Key] public int OfferId { get; set; }
    public int? ModelId { get; set; }                 // NULL = applies to every model
    [ForeignKey(nameof(ModelId))] public CarModel? Model { get; set; }
    public int TermMonths { get; set; }
    [Precision(5, 2)] public decimal AprPercent { get; set; }
    [Precision(5, 2)] public decimal MinDownPercent { get; set; }
    public int MinCreditScore { get; set; }
    public DateOnly ValidUntil { get; set; }
}

[Table("Parts")]
public class Part
{
    [Key] public int PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public int? ModelId { get; set; }                 // NULL = fits every model
    [ForeignKey(nameof(ModelId))] public CarModel? Model { get; set; }
    [Precision(10, 2)] public decimal Price { get; set; }
    public int QtyInStock { get; set; }
    public int RestockDays { get; set; }
}

[Table("StorePolicies")]
public class StorePolicy
{
    [Key] public int PolicyId { get; set; }
    public string Topic { get; set; } = "";
    public string PolicyText { get; set; } = "";
}
