namespace AutoAgent.Api.Dtos;

// DTOs = exactly what the API is allowed to reveal.
// The AI agent only ever sees these shapes, never the raw database rows.

// ---------- Public: anyone may ask ----------
public record CarModelDto(
    string ModelName, int ModelYear, string BodyType, string FuelType,
    string? Engine, string? Transmission, int Seats, string? RangeOrMpg,
    decimal BasePrice, string? Description,
    int FactoryOrderLeadTimeWeeks, int FreeServiceMonths, int AvailableInStock);

public record VehicleDto(
    string Vin, string ModelName, int ModelYear, string BodyType, string FuelType,
    int Seats, string Trim, string Color, int Mileage, string Condition,
    decimal ListPrice, string Status);

public class InventorySearch
{
    public string? ModelName { get; set; }
    public string? BodyType { get; set; }
    public string? FuelType { get; set; }
    public string? Condition { get; set; }
    public string? Color { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinSeats { get; set; }
}

public record FinancingOfferDto(
    int OfferId, string AppliesTo, int TermMonths, decimal AprPercent,
    decimal MinDownPercent, int MinCreditScore, DateOnly ValidUntil);

public record PaymentEstimateDto(
    string Vin, string ModelName, decimal Price, int OfferId, int TermMonths,
    decimal AprPercent, decimal DownPayment, decimal MinDownPayment,
    decimal AmountFinanced, decimal MonthlyPayment, decimal TotalOfPayments,
    decimal TotalInterest, string Note);

public record PartDto(
    string PartNumber, string PartName, string FitsModel, decimal Price,
    int QtyInStock, string Availability);

public record PolicyDto(string Topic, string PolicyText);

// ---------- Private: only after the customer is verified ----------
public record VerifyRequest(string Phone, string VinLast4);

public record VerifyResponse(string Token, string FirstName, DateTimeOffset ExpiresAt);

public record ServiceRecordDto(DateOnly ServiceDate, string Work, decimal Cost);

public record OwnedVehicleDto(
    string Vin, string ModelName, int ModelYear, string Trim, string Color,
    DateOnly PurchaseDate, decimal PurchasePrice, DateOnly? WarrantyEnds,
    DateOnly FreeServiceUntil, List<ServiceRecordDto> ServiceHistory);
