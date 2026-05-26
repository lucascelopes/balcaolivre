using System.Globalization;
using System.Text.Json;

namespace BalcaoLivre.Online.Windows;

public static class IFoodOrderMapper
{
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public static IFoodImportedOrder FromOrder(JsonElement order)
    {
        var imported = new IFoodImportedOrder
        {
            OrderId = GetString(order, "id"),
            DisplayId = GetString(order, "displayId"),
            OrderType = GetString(order, "orderType", "DELIVERY")
        };

        if (order.TryGetProperty("customer", out var customer))
        {
            imported.CustomerName = GetString(customer, "name", "CLIENTE IFOOD").ToUpperInvariant();
            imported.CustomerDocument = GetString(customer, "documentNumber");
            if (customer.TryGetProperty("phone", out var phone))
            {
                var number = GetString(phone, "number");
                var localizer = GetString(phone, "localizer");
                imported.Phone = string.IsNullOrWhiteSpace(localizer) ? number : $"{number} cod. {localizer}";
            }
        }

        if (order.TryGetProperty("delivery", out var delivery) &&
            delivery.TryGetProperty("deliveryAddress", out var address))
        {
            imported.Address = BuildAddress(address);
            imported.District = FirstNotEmpty(
                GetString(address, "neighborhood"),
                GetString(address, "district"),
                GetString(address, "reference"));
        }

        if (order.TryGetProperty("takeout", out var takeout))
        {
            imported.Notes = AppendLine(imported.Notes, $"Retirada: {GetString(takeout, "mode")}");
        }

        imported.Notes = AppendLine(imported.Notes, GetString(order, "extraInfo"));

        if (order.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var quantity = Math.Max(1, (int)Math.Round(GetDecimal(item, "quantity", 1m), MidpointRounding.AwayFromZero));
                var total = FirstPositive(
                    GetNestedDecimal(item, "totalPrice", "value"),
                    GetNestedDecimal(item, "price", "value"),
                    GetNestedDecimal(item, "unitPrice", "value") * quantity);
                var unit = quantity > 0 ? total / quantity : total;
                var notes = GetString(item, "observations");

                if (item.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
                {
                    var optionNames = options.EnumerateArray()
                        .Select(option => GetString(option, "name"))
                        .Where(value => !string.IsNullOrWhiteSpace(value));
                    notes = AppendLine(notes, string.Join(", ", optionNames));
                }

                imported.Items.Add(new IFoodImportedItem
                {
                    Code = FirstNotEmpty(GetString(item, "externalCode"), GetString(item, "id"), "IFOOD"),
                    Name = GetString(item, "name", "ITEM IFOOD").ToUpperInvariant(),
                    Quantity = quantity,
                    UnitPrice = unit,
                    Notes = notes
                });
            }
        }

        imported.Total = FirstPositive(
            GetNestedDecimal(order, "total", "orderAmount", "value"),
            GetNestedDecimal(order, "total", "subTotal", "value"),
            imported.Items.Sum(item => item.Quantity * item.UnitPrice));

        return imported;
    }

    private static string BuildAddress(JsonElement address)
    {
        var formatted = GetString(address, "formattedAddress");
        if (!string.IsNullOrWhiteSpace(formatted))
        {
            return formatted;
        }

        var street = GetString(address, "streetName");
        var number = GetString(address, "streetNumber");
        var complement = GetString(address, "complement");
        var reference = GetString(address, "reference");
        return string.Join(", ", new[] { $"{street} {number}".Trim(), complement, reference }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string GetString(JsonElement element, string propertyName, string fallback = "")
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString().Trim()
            : fallback;
    }

    private static decimal GetDecimal(JsonElement element, string propertyName, decimal fallback = 0m)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value))
        {
            return value;
        }

        return decimal.TryParse(property.ToString(), NumberStyles.Any, Brazil, out var parsed)
            || decimal.TryParse(property.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : fallback;
    }

    private static decimal GetNestedDecimal(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var item in path)
        {
            if (!current.TryGetProperty(item, out current))
            {
                return 0m;
            }
        }

        if (current.ValueKind == JsonValueKind.Number && current.TryGetDecimal(out var value))
        {
            return value;
        }

        return decimal.TryParse(current.ToString(), NumberStyles.Any, Brazil, out var parsed)
            || decimal.TryParse(current.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : 0m;
    }

    private static decimal FirstPositive(params decimal[] values) => values.FirstOrDefault(value => value > 0m);

    private static string FirstNotEmpty(params string[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";

    private static string AppendLine(string current, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return current;
        }

        return string.IsNullOrWhiteSpace(current) ? value.Trim() : $"{current.Trim()}\n{value.Trim()}";
    }
}
