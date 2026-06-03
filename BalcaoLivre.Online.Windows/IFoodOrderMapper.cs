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
            Status = GetString(order, "status"),
            CreatedAt = GetDateTime(order, "createdAt"),
            OrderTiming = FirstNotEmpty(GetString(order, "orderTiming"), GetString(order, "timing")),
            OrderType = GetString(order, "orderType", "DELIVERY")
        };

        if (order.TryGetProperty("scheduled", out var scheduled) || order.TryGetProperty("scheduling", out scheduled))
        {
            imported.OrderTiming = "SCHEDULED";

            imported.PreparationStartDateTime = FirstDateTime(
                GetDateTime(scheduled, "preparationStartDateTime"),
                GetDateTime(scheduled, "preparation_start_date_time"),
                GetDateTime(scheduled, "preparationStart"),
                GetDateTime(scheduled, "deliveryDateTimeStart"),
                GetDateTime(scheduled, "startDateTime"),
                GetDateTime(scheduled, "scheduledDateTime"));
            imported.DeliveryExpectedAt = FirstDateTime(
                GetDateTime(scheduled, "deliveryDateTimeStart"),
                GetDateTime(scheduled, "scheduledDateTime"),
                GetDateTime(scheduled, "deliveryDateTime"),
                GetDateTime(scheduled, "startDateTime"),
                GetDateTime(scheduled, "deliveryDateTimeEnd"),
                GetDateTime(scheduled, "endDateTime"));
        }

        if (order.TryGetProperty("preparation", out var preparation))
        {
            imported.PreparationStartDateTime ??= FirstDateTime(
                GetDateTime(preparation, "start"),
                GetDateTime(preparation, "Start"),
                GetDateTime(preparation, "startDateTime"),
                GetDateTime(preparation, "preparationStartDateTime"));
        }

        imported.PreparationStartDateTime ??= FirstDateTime(
            GetDateTime(order, "preparationStartDateTime"),
            GetDateTime(order, "preparationStart"));
        imported.ConfirmationDeadlineAt = FirstDateTime(
            GetDateTime(order, "confirmationDeadlineAt"),
            ConfirmationDeadlineFrom(imported));

        if (order.TryGetProperty("delivery", out var delivery))
        {
            imported.DeliveryExpectedAt = FirstDateTime(
                imported.DeliveryExpectedAt,
                GetDateTime(delivery, "estimatedDeliveryDateTime"),
                GetDateTime(delivery, "estimatedDeliveryTime"),
                GetDateTime(delivery, "deliveryEstimateDateTime"),
                GetDateTime(delivery, "deliveryEstimate"),
                GetDateTime(delivery, "deliveryDateTimeStart"),
                GetDateTime(delivery, "deliveryDateTime"),
                GetDateTime(order, "estimatedDeliveryDateTime"),
                GetDateTime(order, "estimatedDeliveryTime"),
                GetDateTime(order, "deliveryDateTimeStart"));
            imported.DeliveredAt = FirstDateTime(
                GetDateTime(delivery, "deliveredAt"),
                GetDateTime(delivery, "deliveredDateTime"),
                GetDateTime(order, "deliveredAt"),
                GetDateTime(order, "deliveredDateTime"));
            imported.CollectedAt = FirstDateTime(
                GetDateTime(delivery, "collectedAt"),
                GetDateTime(delivery, "pickupAt"),
                GetDateTime(delivery, "pickupDateTime"),
                GetDateTime(order, "collectedAt"));
        }

        if (order.TryGetProperty("customer", out var customer))
        {
            imported.CustomerName = GetString(customer, "name", "CLIENTE IFOOD").ToUpperInvariant();
            imported.CustomerDocument = FirstNotEmpty(
                GetString(customer, "documentNumber"),
                GetString(customer, "document"),
                GetString(customer, "cpf"),
                GetString(customer, "cnpj"),
                GetString(customer, "taxPayerIdentificationNumber"),
                GetString(customer, "taxpayerIdentificationNumber"));
            if (customer.TryGetProperty("phone", out var phone))
            {
                var number = GetString(phone, "number");
                var localizer = GetString(phone, "localizer");
                imported.Phone = string.IsNullOrWhiteSpace(localizer) ? number : $"{number} cod. {localizer}";
            }
        }

        if (order.TryGetProperty("delivery", out delivery) &&
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
            imported.OrderType = "TAKEOUT";
            imported.Notes = AppendLine(imported.Notes, $"Retirada: {GetString(takeout, "mode")}");
        }

        imported.Notes = AppendLine(imported.Notes, FirstNotEmpty(
            GetString(order, "observations"),
            GetString(order, "observation"),
            GetString(order, "note")));
        imported.Notes = AppendLine(imported.Notes, GetString(order, "extraInfo"));
        imported.PaymentMethod = BuildPaymentMethod(order);
        imported.PaymentSummary = BuildPaymentSummary(order);
        imported.ChangeFor = ExtractChangeFor(order);
        imported.VoucherSummary = BuildVoucherSummary(order);
        imported.CancellationInfo = BuildCancellationInfo(order);

        if (order.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var quantity = Math.Max(1, (int)Math.Round(GetDecimal(item, "quantity", 1m), MidpointRounding.AwayFromZero));
                var total = FirstPositive(
                    GetNestedDecimal(item, "totalPrice", "value"),
                    GetNestedDecimal(item, "totalPrice"),
                    GetNestedDecimal(item, "price", "value"),
                    GetNestedDecimal(item, "price"),
                    GetNestedDecimal(item, "unitPrice", "value") * quantity,
                    GetNestedDecimal(item, "unitPrice") * quantity);
                var unit = FirstPositive(
                    GetNestedDecimal(item, "unitPrice", "value"),
                    GetNestedDecimal(item, "unitPrice"),
                    quantity > 0 ? total / quantity : total);
                var notes = FirstNotEmpty(
                    GetString(item, "observations"),
                    GetString(item, "observation"),
                    GetString(item, "note"));

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
                    ProductId = FirstNotEmpty(GetString(item, "productId"), GetString(item, "catalogItemId"), GetString(item, "id")),
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

    private static string BuildPaymentMethod(JsonElement order)
    {
        return PaymentElements(order)
            .Select(BuildPaymentLabel)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string BuildPaymentSummary(JsonElement order)
    {
        var parts = new List<string>();
        foreach (var payment in PaymentElements(order))
        {
            var method = BuildPaymentLabel(payment);
            if (string.IsNullOrWhiteSpace(method))
            {
                continue;
            }

            var amount = FirstPositive(
                GetNestedDecimal(payment, "value"),
                GetNestedDecimal(payment, "amount"),
                GetNestedDecimal(payment, "price", "value"));
            var type = FirstNotEmpty(GetString(payment, "paymentType"), GetString(payment, "prepaid"), GetString(payment, "liability"));
            var delivery = IsPaymentOnDelivery(payment, type) ? "na entrega" : IsPrepaidPayment(payment, type) ? "online" : "";
            parts.Add(string.Join(" ", new[]
            {
                method,
                amount > 0m ? Money(amount) : "",
                delivery
            }.Where(value => !string.IsNullOrWhiteSpace(value))));
        }

        var summary = string.Join(" / ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
        var changeFor = ExtractChangeFor(order);
        if (changeFor > 0m)
        {
            summary = string.IsNullOrWhiteSpace(summary)
                ? $"Troco para {Money(changeFor)}"
                : $"{summary} | Troco para {Money(changeFor)}";
        }

        return summary;
    }

    private static string BuildPaymentLabel(JsonElement payment)
    {
        var method = NormalizePaymentLabel(FirstNotEmpty(
            GetNestedString(payment, "method", "name"),
            GetNestedString(payment, "paymentMethod", "method"),
            GetNestedString(payment, "paymentMethod", "name"),
            GetString(payment, "method"),
            GetString(payment, "name"),
            GetString(payment, "type")));
        var brand = NormalizeCardBrand(FirstNotEmpty(
            GetString(payment, "brand"),
            GetString(payment, "cardBrand"),
            GetString(payment, "card_brand"),
            GetString(payment, "issuer"),
            GetNestedString(payment, "card", "brand"),
            GetNestedString(payment, "card", "cardBrand"),
            GetNestedString(payment, "credit", "brand"),
            GetNestedString(payment, "debit", "brand"),
            GetNestedString(payment, "paymentMethod", "brand"),
            GetNestedString(payment, "paymentMethod", "cardBrand")));

        if (string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(brand))
        {
            return $"CARTAO - Bandeira {brand}";
        }

        if (!string.IsNullOrWhiteSpace(brand)
            && (method.Contains("CREDITO", StringComparison.OrdinalIgnoreCase)
                || method.Contains("DEBITO", StringComparison.OrdinalIgnoreCase)
                || method.Contains("CARTAO", StringComparison.OrdinalIgnoreCase))
            && !method.Contains(brand, StringComparison.OrdinalIgnoreCase))
        {
            return $"{method} - Bandeira {brand}";
        }

        return method;
    }

    private static IEnumerable<JsonElement> PaymentElements(JsonElement order)
    {
        if (order.TryGetProperty("payments", out var payments))
        {
            foreach (var item in ElementOrChildren(payments, "methods", "paymentMethods", "items"))
            {
                yield return item;
            }
        }

        if (order.TryGetProperty("payment", out var payment))
        {
            foreach (var item in ElementOrChildren(payment, "methods", "paymentMethods", "items"))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<JsonElement> ElementOrChildren(JsonElement element, params string[] childNames)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                yield return item;
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var childName in childNames)
        {
            if (element.TryGetProperty(childName, out var child) && child.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in child.EnumerateArray())
                {
                    yield return item;
                }
            }
        }

        if (childNames.All(name => !element.TryGetProperty(name, out _)))
        {
            yield return element;
        }
    }

    private static string NormalizePaymentLabel(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "CASH" or "MONEY" or "DINHEIRO" => "DINHEIRO",
            "CREDIT" or "CREDIT_CARD" or "CARTAO_CREDITO" => "CARTAO CREDITO",
            "DEBIT" or "DEBIT_CARD" or "CARTAO_DEBITO" => "CARTAO DEBITO",
            "MEAL_VOUCHER" or "VOUCHER" => "VOUCHER",
            _ => normalized.Replace('_', ' ')
        };
    }

    private static string NormalizeCardBrand(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant().Replace('_', ' ');
        return normalized switch
        {
            "VISA" => "Visa",
            "MASTERCARD" or "MASTER CARD" => "Mastercard",
            "ELO" => "Elo",
            "AMEX" or "AMERICAN EXPRESS" => "American Express",
            "HIPERCARD" => "Hipercard",
            "" => "",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant())
        };
    }

    private static bool IsPaymentOnDelivery(JsonElement payment, string type)
    {
        var joined = $"{type} {GetString(payment, "liability")} {GetString(payment, "paymentType")}".ToUpperInvariant();
        return joined.Contains("OFFLINE", StringComparison.Ordinal) ||
               joined.Contains("ON_DELIVERY", StringComparison.Ordinal) ||
               joined.Contains("NA ENTREGA", StringComparison.Ordinal) ||
               joined.Contains("DELIVERY", StringComparison.Ordinal);
    }

    private static bool IsPrepaidPayment(JsonElement payment, string type)
    {
        var joined = $"{type} {GetString(payment, "liability")} {GetString(payment, "paymentType")}".ToUpperInvariant();
        return joined.Contains("PREPAID", StringComparison.Ordinal) ||
               joined.Contains("ONLINE", StringComparison.Ordinal);
    }

    private static decimal ExtractChangeFor(JsonElement order)
    {
        var candidates = new List<decimal>
        {
            GetNestedDecimal(order, "cash", "changeFor", "value"),
            GetNestedDecimal(order, "cash", "changeFor"),
            GetNestedDecimal(order, "cash", "change", "value"),
            GetNestedDecimal(order, "cash", "change"),
            GetNestedDecimal(order, "payment", "cash", "changeFor", "value"),
            GetNestedDecimal(order, "payment", "changeFor", "value"),
            GetNestedDecimal(order, "payment", "cash", "change", "value"),
            GetNestedDecimal(order, "payment", "change", "value"),
            GetNestedDecimal(order, "payments", "cash", "changeFor", "value"),
            GetNestedDecimal(order, "payments", "changeFor", "value"),
            GetNestedDecimal(order, "payments", "cash", "change", "value"),
            GetNestedDecimal(order, "payments", "change", "value")
        };

        foreach (var payment in PaymentElements(order))
        {
            candidates.Add(GetNestedDecimal(payment, "cash", "changeFor", "value"));
            candidates.Add(GetNestedDecimal(payment, "changeFor", "value"));
            candidates.Add(GetNestedDecimal(payment, "changeFor"));
            candidates.Add(GetNestedDecimal(payment, "cash", "change", "value"));
            candidates.Add(GetNestedDecimal(payment, "change", "value"));
            candidates.Add(GetNestedDecimal(payment, "change"));
        }

        return FirstPositive(candidates.ToArray());
    }

    private static string BuildVoucherSummary(JsonElement order)
    {
        var values = new List<string>();
        CollectVoucherValues(order, values);
        return string.Join(" / ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6));
    }

    private static void CollectVoucherValues(JsonElement element, ICollection<string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var code = FirstNotEmpty(
                GetString(element, "code"),
                GetString(element, "voucherCode"),
                GetString(element, "couponCode"),
                GetString(element, "promoCode"),
                GetString(element, "campaignCode"),
                GetString(element, "target"));
            var name = FirstNotEmpty(GetString(element, "name"), GetString(element, "description"), GetString(element, "title"));
            var amount = FirstPositive(
                GetNestedDecimal(element, "value"),
                GetNestedDecimal(element, "amount"),
                GetNestedDecimal(element, "amount", "value"));
            var subsidy = BuildSubsidySummary(element);
            var joined = $"{code} {name}".Trim();
            if (joined.Contains("VOUCHER", StringComparison.OrdinalIgnoreCase) ||
                joined.Contains("ENTGRATIS", StringComparison.OrdinalIgnoreCase) ||
                amount > 0m && ElementNameSuggestsDiscount(element))
            {
                values.Add(string.Join(" | ", new[]
                {
                    string.Join(" ", new[] { code, name }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    amount > 0m ? $"Desconto {Money(amount)}" : "",
                    subsidy
                }.Where(value => !string.IsNullOrWhiteSpace(value))));
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Contains("benefit", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("discount", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("voucher", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("coupon", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("promotion", StringComparison.OrdinalIgnoreCase))
                {
                    CollectVoucherValues(property.Value, values);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectVoucherValues(item, values);
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString() ?? "";
            if (text.Contains("VOUCHER", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ENTGRATIS", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(text);
            }
        }
    }

    private static bool ElementNameSuggestsDiscount(JsonElement element)
    {
        var raw = element.GetRawText();
        return raw.Contains("discount", StringComparison.OrdinalIgnoreCase) ||
               raw.Contains("benefit", StringComparison.OrdinalIgnoreCase) ||
               raw.Contains("voucher", StringComparison.OrdinalIgnoreCase) ||
               raw.Contains("coupon", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSubsidySummary(JsonElement element)
    {
        var parts = new List<string>();
        CollectSubsidyValues(element, parts);
        return parts.Count == 0
            ? ""
            : $"Subsidio: {string.Join(", ", parts.Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}";
    }

    private static void CollectSubsidyValues(JsonElement element, ICollection<string> parts)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var sponsor = FirstNotEmpty(
                GetString(element, "sponsor"),
                GetString(element, "sponsoredBy"),
                GetString(element, "provider"),
                GetString(element, "owner"),
                GetString(element, "name"),
                GetString(element, "description"));
            var amount = FirstPositive(
                GetNestedDecimal(element, "sponsorshipValue"),
                GetNestedDecimal(element, "sponsorshipValue", "value"),
                GetNestedDecimal(element, "subsidy"),
                GetNestedDecimal(element, "subsidy", "value"),
                GetNestedDecimal(element, "value"),
                GetNestedDecimal(element, "amount"),
                GetNestedDecimal(element, "amount", "value"));
            if (!string.IsNullOrWhiteSpace(sponsor)
                && amount > 0m
                && ElementNameSuggestsSubsidy(element))
            {
                parts.Add($"{NormalizeSponsor(sponsor)} {Money(amount)}");
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Contains("sponsor", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("subsid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("liability", StringComparison.OrdinalIgnoreCase))
                {
                    CollectSubsidyValues(property.Value, parts);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectSubsidyValues(item, parts);
            }
        }
    }

    private static bool ElementNameSuggestsSubsidy(JsonElement element)
    {
        var raw = element.GetRawText();
        return raw.Contains("sponsor", StringComparison.OrdinalIgnoreCase) ||
               raw.Contains("subsid", StringComparison.OrdinalIgnoreCase) ||
               raw.Contains("liability", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSponsor(string value)
    {
        var sponsor = (value ?? "").Trim();
        return sponsor.ToUpperInvariant() switch
        {
            "IFOOD" => "iFood",
            "MERCHANT" or "RESTAURANT" => "Loja",
            _ => sponsor
        };
    }

    private static string BuildCancellationInfo(JsonElement order)
    {
        if (order.TryGetProperty("cancellation", out var cancellation))
        {
            return string.Join(" - ", new[]
            {
                GetString(cancellation, "requestedBy"),
                GetString(cancellation, "reason"),
                GetString(cancellation, "cancellationCode"),
                GetString(cancellation, "message")
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return FirstNotEmpty(
            GetString(order, "cancellationReason"),
            GetString(order, "cancelReason"),
            GetString(order, "cancellationMessage"));
    }

    private static string GetString(JsonElement element, string propertyName, string fallback = "")
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString().Trim()
            : fallback;
    }

    private static string GetNestedString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var item in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(item, out current))
            {
                return "";
            }
        }

        return current.ValueKind == JsonValueKind.Null ? "" : current.ToString().Trim();
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

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DateTime.TryParse(
            property.ToString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value)
            ? value
            : null;
    }

    private static decimal GetNestedDecimal(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var item in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(item, out current))
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

    private static DateTime? FirstDateTime(params DateTime?[] values) => values.FirstOrDefault(value => value.HasValue);

    private static DateTime? ConfirmationDeadlineFrom(IFoodImportedOrder order)
    {
        var baseAt = string.Equals(order.OrderTiming, "SCHEDULED", StringComparison.OrdinalIgnoreCase)
            ? order.PreparationStartDateTime ?? order.CreatedAt
            : order.CreatedAt;
        return baseAt?.AddMinutes(8);
    }

    private static string FirstNotEmpty(params string[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";

    private static string Money(decimal value) => value.ToString("C", Brazil);

    private static string AppendLine(string current, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return current;
        }

        return string.IsNullOrWhiteSpace(current) ? value.Trim() : $"{current.Trim()}\n{value.Trim()}";
    }
}
