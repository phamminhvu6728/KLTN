using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using TuTuc.Models;

namespace TuTuc.Services;

public partial class MarketplaceManager
{
    private sealed record PreviewEnvelope(int Owner, BookingTermsInput Terms);

    static string PreviewFingerprint(BookingTermsInput terms) => JsonSerializer.Serialize(new
    {
        terms.Service, terms.Operation, terms.Version, terms.Date, terms.EndDate,
        terms.People, terms.Guests, terms.Total,
        ArrivalDeparture=Clean(terms.ArrivalDeparture), Options=Clean(terms.Options).Replace("\r\n","\n"), Note=Clean(terms.Note).Replace("\r\n","\n")
    });

    string CreatePreviewToken(int owner, BookingTermsInput terms)
    {
        var copy = JsonSerializer.Deserialize<BookingTermsInput>(JsonSerializer.Serialize(terms))!;
        copy.PreviewToken = null;
        return previewProtection.Protect(JsonSerializer.Serialize(new PreviewEnvelope(owner, copy)), TimeSpan.FromMinutes(10));
    }

    public BookingTermsInput? ReadPreview(int owner, string? token)
    {
        try
        {
            if (string.IsNullOrEmpty(token) || token.Length > 30000) return null;
            var envelope = JsonSerializer.Deserialize<PreviewEnvelope>(previewProtection.Unprotect(token));
            if (envelope?.Owner != owner) return null;
            envelope.Terms.PreviewToken = token;
            return envelope.Terms;
        }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException or ArgumentException)
        {
            return null;
        }
    }
}
