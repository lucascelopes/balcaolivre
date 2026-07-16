using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private List<WhatsAppBookingServiceSnapshot> BuildWhatsAppBookingServicesSnapshot(DateTime today)
    {
        return _data.Services
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Take(12)
            .Select(service =>
            {
                var days = Enumerable.Range(0, 14)
                    .Select(offset =>
                    {
                        var date = today.Date.AddDays(offset);
                        return new WhatsAppBookingDaySnapshot(
                            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            DateShortcutLabel(date),
                            BuildWhatsAppBookingSlotsSnapshot(date, service, 8));
                    })
                    .Where(day => day.availableSlots.Count > 0)
                    .Take(7)
                    .ToList();

                return new WhatsAppBookingServiceSnapshot(
                    service.Id,
                    service.Name,
                    Math.Clamp(service.DurationMinutes, 15, 480),
                    service.Price,
                    days);
            })
            .Where(service => service.days.Count > 0)
            .ToList();
    }

    private List<WhatsAppBookingSlotSnapshot> BuildWhatsAppBookingSlotsSnapshot(
        DateTime date,
        ServiceItem service,
        int maxSlots)
    {
        var slots = new List<WhatsAppBookingSlotSnapshot>();
        if (!IsConfiguredWorkday(date))
        {
            return slots;
        }

        var professionals = _data.Professionals
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToList();
        if (professionals.Count == 0)
        {
            return slots;
        }

        var durationMinutes = Math.Clamp(service.DurationMinutes, 15, 480);
        var start = date.Date.AddHours(_data.Settings.WorkdayStartHour);
        var end = date.Date.AddHours(_data.Settings.WorkdayEndHour);
        if (date.Date == DateTime.Today)
        {
            var next = DateTime.Now.AddMinutes(30);
            start = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute < 30 ? 30 : 0, 0);
            if (next.Minute >= 30)
            {
                start = start.AddHours(1);
            }

            var configuredStart = date.Date.AddHours(_data.Settings.WorkdayStartHour);
            if (start < configuredStart)
            {
                start = configuredStart;
            }
        }

        for (var cursor = start;
             cursor.AddMinutes(durationMinutes) <= end && slots.Count < maxSlots;
             cursor = cursor.AddMinutes(30))
        {
            if (OverlapsConfiguredBreak(cursor, cursor.AddMinutes(durationMinutes)))
            {
                continue;
            }

            foreach (var professional in professionals)
            {
                var resourceName = service.DefaultResource?.Trim() ?? "";
                var draft = new AppointmentDraft(
                    FirstFilled(service.Segment, _data.Settings.BusinessSegment),
                    "Cliente WhatsApp",
                    "",
                    "",
                    service.Id,
                    service.Name,
                    professional.Id,
                    professional.Name,
                    resourceName,
                    cursor,
                    durationMinutes,
                    service.Price,
                    "");
                if (FindConflicts(draft, null).Any())
                {
                    continue;
                }

                slots.Add(new WhatsAppBookingSlotSnapshot(
                    $"{date:yyyyMMdd}-{cursor:HHmm}-{professional.Id}",
                    cursor.ToString("HH:mm", CultureInfo.InvariantCulture),
                    new DateTimeOffset(cursor).ToString("O", CultureInfo.InvariantCulture),
                    professional.Id,
                    professional.Name,
                    resourceName));
                break;
            }
        }

        return slots;
    }

    private async Task ProcessWhatsAppBookingRequestAsync(
        WhatsAppBookingRequest booking,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                booking.Instance,
                WhatsAppRealtimeInstanceName(),
                StringComparison.OrdinalIgnoreCase) ||
            !(string.Equals(booking.Status, "pending", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(booking.Status, "requested", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var resolution = await Dispatcher.InvokeAsync(() => CommitWhatsAppBooking(booking));
        await ResolveWhatsAppBookingAsync(booking, resolution, cancellationToken);
    }

    private WhatsAppBookingResolution CommitWhatsAppBooking(WhatsAppBookingRequest booking)
    {
        var existing = _data.Appointments.FirstOrDefault(item =>
            string.Equals(item.ExternalSource, "whatsapp", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.ExternalReference, booking.Id, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return WhatsAppBookingResolution.Confirmed(existing.Id);
        }

        if (booking.Start is null || booking.Start.Value < DateTime.Now.AddMinutes(-5))
        {
            return WhatsAppBookingResolution.Rejected("Esse hor\u00E1rio n\u00E3o est\u00E1 mais dispon\u00EDvel. Envie 1 para consultar novos hor\u00E1rios.");
        }

        var service = _data.Services.FirstOrDefault(item =>
                          item.IsActive &&
                          !string.IsNullOrWhiteSpace(booking.ServiceId) &&
                          string.Equals(item.Id, booking.ServiceId, StringComparison.OrdinalIgnoreCase))
                      ?? _data.Services.FirstOrDefault(item =>
                          item.IsActive &&
                          string.Equals(item.Name, booking.ServiceName, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            return WhatsAppBookingResolution.Rejected("Esse servi\u00E7o n\u00E3o est\u00E1 mais ativo. Envie 1 para escolher outra op\u00E7\u00E3o.");
        }

        var professional = _data.Professionals.FirstOrDefault(item =>
                               item.IsActive &&
                               !string.IsNullOrWhiteSpace(booking.ProfessionalId) &&
                               string.Equals(item.Id, booking.ProfessionalId, StringComparison.OrdinalIgnoreCase))
                           ?? _data.Professionals.FirstOrDefault(item =>
                               item.IsActive &&
                               string.Equals(item.Name, booking.ProfessionalName, StringComparison.OrdinalIgnoreCase));
        if (professional is null)
        {
            return WhatsAppBookingResolution.Rejected("O profissional desse hor\u00E1rio n\u00E3o est\u00E1 mais dispon\u00EDvel. Envie 1 para escolher outro hor\u00E1rio.");
        }

        var start = booking.Start.Value;
        var duration = Math.Clamp(service.DurationMinutes, 15, 480);
        if (!TryValidateConfiguredBusinessWindow(start, start.AddMinutes(duration), out var businessWindowError))
        {
            return WhatsAppBookingResolution.Rejected($"{businessWindowError} Envie 1 para consultar novos hor\u00E1rios.");
        }

        var customerName = NormalizeWhatsAppBookingCustomerName(booking.CustomerName, booking.Phone);
        TryNormalizeCustomerPhone(booking.Phone, out var customerPhone, out _);
        var resourceName = FirstFilled(booking.ResourceName, service.DefaultResource);
        var draft = new AppointmentDraft(
            FirstFilled(service.Segment, _data.Settings.BusinessSegment),
            customerName,
            customerPhone,
            "Lead recebido e agendado pelo WhatsApp",
            service.Id,
            service.Name,
            professional.Id,
            professional.Name,
            resourceName,
            start,
            duration,
            service.Price,
            $"Agendamento autom\u00E1tico do WhatsApp. Lead {booking.LeadId}.");
        if (FindConflicts(draft, null).Any())
        {
            ExportWhatsAppAgendaSnapshot();
            return WhatsAppBookingResolution.SlotConflict(
                "Esse hor\u00E1rio acabou de ser ocupado. Envie 1 para ver os hor\u00E1rios atualizados.");
        }

        var now = DateTime.Now;
        var appointment = new Appointment
        {
            Id = Guid.NewGuid().ToString("N"),
            Segment = draft.Segment,
            CustomerName = draft.CustomerName,
            CustomerPhone = draft.Phone,
            CustomerProfile = draft.Profile,
            ServiceId = draft.ServiceId,
            ServiceName = draft.ServiceName,
            ProfessionalId = draft.ProfessionalId,
            ProfessionalName = draft.ProfessionalName,
            ResourceName = draft.ResourceName,
            Start = draft.Start,
            DurationMinutes = draft.DurationMinutes,
            Price = draft.Price,
            Status = AppointmentStatus.Confirmed,
            Notes = draft.Notes,
            ExternalSource = "whatsapp",
            ExternalReference = booking.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        _data.Appointments.Add(appointment);
        UpsertCustomer(appointment);
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ExportWhatsAppAgendaSnapshot();
        ShowStatus($"WhatsApp agendou {appointment.CustomerName} para {appointment.Start:dd/MM HH:mm}.");
        return WhatsAppBookingResolution.Confirmed(appointment.Id);
    }

    private string NormalizeWhatsAppBookingCustomerName(string value, string phone)
    {
        var name = (value ?? "").Trim();
        var aliases = new[]
        {
            "Voc\u00EA",
            "Voce",
            "Cliente",
            _data.Settings.BusinessName,
            _data.Settings.WhatsAppConnectedName,
            BusinessDisplayName()
        };
        if (string.IsNullOrWhiteSpace(name) || aliases.Any(alias =>
                !string.IsNullOrWhiteSpace(alias) &&
                string.Equals(alias.Trim(), name, StringComparison.OrdinalIgnoreCase)))
        {
            var digits = NormalizeBrazilPhone(phone);
            return digits.Length >= 4 ? $"Cliente {digits[^4..]}" : "Cliente WhatsApp";
        }

        return name.Length > 120 ? name[..120] : name;
    }

    private async Task ResolveWhatsAppBookingAsync(
        WhatsAppBookingRequest booking,
        WhatsAppBookingResolution resolution,
        CancellationToken cancellationToken)
    {
        var token = await ReadWhatsAppLocalApiTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            using var request = CreateWhatsAppLocalApiRequest(
                HttpMethod.Patch,
                $"/api/agenda/bookings/{Uri.EscapeDataString(booking.Id)}?{WhatsAppRealtimeInstanceQuery()}",
                token,
                new
                {
                    status = resolution.Status,
                    appointmentId = resolution.AppointmentId,
                    message = resolution.Message
                });
            try
            {
                using var response = await _whatsAppRealtimeClient.SendAsync(request, timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                Debug.WriteLine($"Agenda booking resolution returned HTTP {(int)response.StatusCode} (attempt {attempt}).");
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or ObjectDisposedException)
            {
                Debug.WriteLine($"Agenda booking resolution failed (attempt {attempt}): {ex.Message}");
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }
    }

    private static WhatsAppBookingRequest? ParseWhatsAppBookingRequest(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = ReadRealtimeString(element, "id", "bookingId");
        var instance = ReadRealtimeString(element, "instance");
        var phone = NormalizeBrazilPhone(ReadRealtimeString(element, "phone", "customerPhone"));
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        return new WhatsAppBookingRequest(
            id,
            instance,
            ReadRealtimeString(element, "leadId"),
            phone,
            ReadRealtimeString(element, "customerName", "name"),
            ReadRealtimeString(element, "serviceId"),
            ReadRealtimeString(element, "serviceName", "service"),
            ReadRealtimeDate(element, "start", "startsAt", "scheduledAt"),
            ReadRealtimeInt32(element, "durationMinutes"),
            ReadRealtimeString(element, "professionalId"),
            ReadRealtimeString(element, "professionalName", "professional"),
            ReadRealtimeString(element, "resourceName", "resource"),
            ReadRealtimeDecimal(element, "price"),
            FirstFilled(ReadRealtimeString(element, "status"), "pending"));
    }

    private static decimal ReadRealtimeDecimal(JsonElement element, string name)
    {
        if (!TryGetRealtimeProperty(element, name, out var value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
    }

    private sealed record WhatsAppBookingServiceSnapshot(
        string id,
        string name,
        int durationMinutes,
        decimal price,
        IReadOnlyList<WhatsAppBookingDaySnapshot> days);

    private sealed record WhatsAppBookingDaySnapshot(
        string date,
        string label,
        IReadOnlyList<WhatsAppBookingSlotSnapshot> availableSlots);

    private sealed record WhatsAppBookingSlotSnapshot(
        string id,
        string time,
        string start,
        string professionalId,
        string professionalName,
        string resourceName);

    private sealed record WhatsAppBookingRequest(
        string Id,
        string Instance,
        string LeadId,
        string Phone,
        string CustomerName,
        string ServiceId,
        string ServiceName,
        DateTime? Start,
        int DurationMinutes,
        string ProfessionalId,
        string ProfessionalName,
        string ResourceName,
        decimal Price,
        string Status);

    private sealed record WhatsAppBookingResolution(
        string Status,
        string AppointmentId,
        string Message)
    {
        public static WhatsAppBookingResolution Confirmed(string appointmentId) =>
            new("confirmed", appointmentId, "");

        public static WhatsAppBookingResolution SlotConflict(string message) =>
            new("slot_conflict", "", message);

        public static WhatsAppBookingResolution Rejected(string message) =>
            new("rejected", "", message);
    }
}
