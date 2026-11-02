using AutoAgent.Api.Data;
using AutoAgent.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AutoAgentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AutoAgentDb")));

// Holds short-lived tokens for customers who passed identity verification.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CustomerSessionService>();

var app = builder.Build();

app.MapControllers();

app.Run();
