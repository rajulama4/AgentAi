using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Data;

public class AutoAgentDbContext(DbContextOptions<AutoAgentDbContext> options) : DbContext(options)
{
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<Vehicle> Inventory => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<FinancingOffer> FinancingOffers => Set<FinancingOffer>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StorePolicy> StorePolicies => Set<StorePolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A vehicle is sold at most once (Sales.VehicleId is UNIQUE), so it's one-to-one.
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Sale)
            .WithOne(s => s.Vehicle)
            .HasForeignKey<Sale>(s => s.VehicleId);
    }
}
