using AutoAgent.Api.Data;
using AutoAgent.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoAgent.Api.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController(AutoAgentDbContext db) : ControllerBase
{
    // GET /api/policies -> list of topics, so the agent knows what it can look up
    [HttpGet]
    public async Task<ActionResult<List<string>>> GetTopics(CancellationToken ct)
    {
        return await db.StorePolicies.AsNoTracking()
            .OrderBy(p => p.Topic)
            .Select(p => p.Topic)
            .ToListAsync(ct);
    }

    // GET /api/policies/insurance
    [HttpGet("{topic}")]
    public async Task<ActionResult<PolicyDto>> GetPolicy(string topic, CancellationToken ct)
    {
        var policy = await db.StorePolicies.AsNoTracking()
            .Where(p => p.Topic == topic)
            .Select(p => new PolicyDto(p.Topic, p.PolicyText))
            .FirstOrDefaultAsync(ct);

        if (policy is not null) return policy;

        var topics = await db.StorePolicies.Select(p => p.Topic).ToListAsync(ct);
        return NotFound(new { message = $"No policy for '{topic}'.", availableTopics = topics });
    }
}
