using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace AutoAgent.Api.Services;

/// <summary>
/// After a customer proves who they are, we hand out a random token that is
/// valid for 30 minutes. Private endpoints require this token.
///
/// Why not just let the agent pass a CustomerId? Because the LLM writes the
/// tool arguments. If a caller says "I'm customer 2", a CustomerId parameter
/// would let the model fetch someone else's data. A token can only come from
/// a successful verification, so the model can't invent one.
/// </summary>
public class CustomerSessionService(IMemoryCache cache)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public (string Token, DateTimeOffset ExpiresAt) Create(int customerId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.Add(Lifetime);
        cache.Set(Key(token), customerId, expiresAt);
        return (token, expiresAt);
    }

    public bool TryGetCustomerId(string? token, out int customerId)
    {
        customerId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;
        return cache.TryGetValue(Key(token), out customerId);
    }

    private static string Key(string token) => "customer-session:" + token;
}

public static class PhoneNumber
{
    /// <summary>"(616) 555-0101", "616-555-0101", "+1 616 555 0101" all become "+16165550101".</summary>
    public static string Normalize(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 10) digits = "1" + digits;   // assume US number
        return "+" + digits;
    }
}
