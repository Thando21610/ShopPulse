using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace ShopPulse.Client;

public static class ApiErrorFormatter
{
    public static async Task<string> FormatAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return "You do not have permission for this action.";

        JsonDocument? document = null;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if ((int)response.StatusCode >= 500)
            {
                var reference = TryString(root, "correlationId") ?? TryString(root, "traceId");
                return string.IsNullOrWhiteSpace(reference)
                    ? "Something went wrong. Please try again or contact support."
                    : $"Something went wrong. Reference: {reference}.";
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray().Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value))
                        : [])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (messages.Length > 0) return string.Join(" ", messages);
            }

            return TryString(root, "message")
                ?? TryString(root, "detail")
                ?? TryString(root, "title")
                ?? fallback;
        }
        catch (JsonException) { return fallback; }
        catch (IOException) { return fallback; }
        finally { document?.Dispose(); }
    }

    public static async Task<bool> ApplyValidationErrorsAsync(
        HttpResponseMessage response,
        EditContext editContext,
        CancellationToken cancellationToken = default)
    {
        if (response.StatusCode != HttpStatusCode.BadRequest) return false;

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Object) return false;

            var store = new ValidationMessageStore(editContext);
            store.Clear();
            var properties = editContext.Model.GetType().GetProperties();
            var applied = false;

            foreach (var error in errors.EnumerateObject())
            {
                var fieldName = error.Name.Split('.').Last();
                var property = properties.FirstOrDefault(x =>
                    string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase));
                if (property is null || error.Value.ValueKind != JsonValueKind.Array) continue;

                var field = new FieldIdentifier(editContext.Model, property.Name);
                foreach (var message in error.Value.EnumerateArray())
                {
                    if (message.ValueKind != JsonValueKind.String) continue;
                    var value = message.GetString();
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    store.Add(field, value);
                    applied = true;
                }
            }

            if (applied) editContext.NotifyValidationStateChanged();
            return applied;
        }
        catch (JsonException) { return false; }
        catch (IOException) { return false; }
    }

    private static string? TryString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
