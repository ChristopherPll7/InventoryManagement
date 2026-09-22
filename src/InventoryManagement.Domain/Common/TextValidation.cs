namespace InventoryManagement.Domain.Common;

internal static class TextValidation
{
    public static string Required(string? value, int maximumLength, string code, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(code, $"{field} is required.");
        return Optional(value, maximumLength, code, field)!;
    }

    public static string? Optional(string? value, int maximumLength, string code, string field)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > maximumLength)
            throw new DomainException(code, $"{field} cannot exceed {maximumLength} characters.");
        return normalized;
    }
}
