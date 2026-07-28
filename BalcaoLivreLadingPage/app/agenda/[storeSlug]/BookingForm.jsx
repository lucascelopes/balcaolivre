"use client";

import { useMemo, useState } from "react";
import { formatMoneyFromCents } from "../stores";

function pad(value) {
  return String(value).padStart(2, "0");
}

function dateKey(date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function addDays(date, days) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function formatDateLabel(date) {
  return new Intl.DateTimeFormat("pt-BR", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit"
  })
    .format(date)
    .replace(".", "");
}

function buildDates(store) {
  const today = new Date();
  const dates = [];

  for (let offset = 0; dates.length < 8 && offset < 18; offset++) {
    const current = addDays(today, offset);
    const weekday = current.getDay();
    if (store.availableWeekdays.includes(weekday)) {
      dates.push({
        key: dateKey(current),
        label: offset === 0 ? "Hoje" : formatDateLabel(current),
        short: `${pad(current.getDate())}/${pad(current.getMonth() + 1)}`
      });
    }
  }

  return dates;
}

function buildSlots(store, selectedDate, professionalId) {
  const slots = [];
  const booked = new Set(
    store.bookedSlots.map((slot) => {
      const bookedDate = dateKey(addDays(new Date(), slot.dateOffset || 0));
      return `${bookedDate}|${slot.time}|${slot.professionalId}`;
    })
  );

  for (let hour = store.workdayStartHour; hour < store.workdayEndHour; hour++) {
    for (let minute = 0; minute < 60; minute += store.slotMinutes) {
      const time = `${pad(hour)}:${pad(minute)}`;
      const key = `${selectedDate}|${time}|${professionalId}`;
      if (!booked.has(key)) {
        slots.push(time);
      }
    }
  }

  return slots;
}

export default function BookingForm({ store }) {
  const dates = useMemo(() => buildDates(store), [store]);
  const [serviceId, setServiceId] = useState(store.services[0]?.id || "");
  const selectedService =
    store.services.find((service) => service.id === serviceId) || store.services[0];
  const availableProfessionals = store.professionals.filter((professional) =>
    selectedService?.professionalIds.includes(professional.id)
  );
  const [professionalId, setProfessionalId] = useState(
    availableProfessionals[0]?.id || store.professionals[0]?.id || ""
  );
  const resolvedProfessionalId =
    availableProfessionals.some((professional) => professional.id === professionalId)
      ? professionalId
      : availableProfessionals[0]?.id || "";
  const [selectedDate, setSelectedDate] = useState(dates[0]?.key || "");
  const slots = useMemo(
    () => buildSlots(store, selectedDate, resolvedProfessionalId),
    [store, selectedDate, resolvedProfessionalId]
  );
  const [selectedTime, setSelectedTime] = useState(slots[0] || "");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [status, setStatus] = useState({ type: "idle", message: "" });

  function changeService(nextServiceId) {
    const nextService = store.services.find((service) => service.id === nextServiceId);
    const nextProfessionalId =
      store.professionals.find((professional) =>
        nextService?.professionalIds.includes(professional.id)
      )?.id || "";
    setServiceId(nextServiceId);
    setProfessionalId(nextProfessionalId);
    setSelectedTime("");
  }

  function changeDate(nextDate) {
    setSelectedDate(nextDate);
    setSelectedTime("");
  }

  async function submitBooking(event) {
    event.preventDefault();
    setStatus({ type: "loading", message: "Enviando agendamento..." });

    try {
      const response = await fetch("/api/agenda/appointments", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          storeSlug: store.slug,
          serviceId,
          professionalId: resolvedProfessionalId,
          date: selectedDate,
          time: selectedTime,
          customerName,
          customerPhone,
          customerNotes
        })
      });
      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.error || "Nao foi possivel enviar o agendamento.");
      }

      setStatus({
        type: "success",
        message: `Pedido recebido. Protocolo ${data.protocol}.`
      });
      setCustomerName("");
      setCustomerPhone("");
      setCustomerNotes("");
    } catch (error) {
      setStatus({
        type: "error",
        message: error.message || "Nao foi possivel enviar o agendamento."
      });
    }
  }

  return (
    <form className="agendaBookingForm" onSubmit={submitBooking}>
      <div className="agendaFormStep">
        <div className="agendaStepTitle">
          <b>1</b>
          <span>Atendimento</span>
        </div>

        <fieldset className="agendaServicePicker">
          <legend>Servico</legend>
          <div>
            {store.services.map((service) => (
              <button
                type="button"
                className={service.id === serviceId ? "active" : ""}
                key={service.id}
                aria-pressed={service.id === serviceId}
                onClick={() => changeService(service.id)}
              >
                <span>{service.category}</span>
                <strong>{service.name}</strong>
                <small>
                  {service.durationMinutes} min | {formatMoneyFromCents(service.priceCents)}
                </small>
              </button>
            ))}
          </div>
        </fieldset>

        <div className="agendaServiceSummary">
          <strong>{selectedService?.name}</strong>
          <span>{selectedService?.description}</span>
        </div>
      </div>

      <div className="agendaFormStep">
        <div className="agendaStepTitle">
          <b>2</b>
          <span>Dia e horario</span>
        </div>

        <div className="agendaFieldGroup">
          <label htmlFor="agenda-professional">Profissional</label>
          <select
            id="agenda-professional"
            value={resolvedProfessionalId}
            onChange={(event) => {
              setProfessionalId(event.target.value);
              setSelectedTime("");
            }}
            required
          >
            {availableProfessionals.map((professional) => (
              <option value={professional.id} key={professional.id}>
                {professional.name} - {professional.role}
              </option>
            ))}
          </select>
        </div>

        <fieldset className="agendaDatePicker">
          <legend>Data</legend>
          <div>
            {dates.map((date) => (
              <button
                type="button"
                className={date.key === selectedDate ? "active" : ""}
                key={date.key}
                onClick={() => changeDate(date.key)}
              >
                <span>{date.label}</span>
                <strong>{date.short}</strong>
              </button>
            ))}
          </div>
        </fieldset>

        <fieldset className="agendaTimePicker">
          <legend>Horario</legend>
          <div>
            {slots.map((slot) => (
              <button
                type="button"
                className={slot === selectedTime ? "active" : ""}
                key={slot}
                onClick={() => setSelectedTime(slot)}
              >
                {slot}
              </button>
            ))}
          </div>
        </fieldset>
      </div>

      <div className="agendaFormStep">
        <div className="agendaStepTitle">
          <b>3</b>
          <span>Seus dados</span>
        </div>

        <div className="agendaFormGrid">
          <div className="agendaFieldGroup">
            <label htmlFor="agenda-name">Nome</label>
            <input
              id="agenda-name"
              value={customerName}
              onChange={(event) => setCustomerName(event.target.value)}
              placeholder="Seu nome"
              minLength={3}
              required
            />
          </div>
          <div className="agendaFieldGroup">
            <label htmlFor="agenda-phone">WhatsApp</label>
            <input
              id="agenda-phone"
              value={customerPhone}
              onChange={(event) => setCustomerPhone(event.target.value)}
              placeholder="(00) 00000-0000"
              inputMode="tel"
              minLength={10}
              required
            />
          </div>
        </div>

        <div className="agendaFieldGroup">
          <label htmlFor="agenda-notes">Observacao</label>
          <textarea
            id="agenda-notes"
            value={customerNotes}
            onChange={(event) => setCustomerNotes(event.target.value)}
            placeholder="Preferencias, recado ou detalhe importante"
            rows={3}
          />
        </div>
      </div>

      <button
        className="agendaSubmitButton"
        type="submit"
        disabled={!selectedTime || status.type === "loading"}
      >
        {status.type === "loading" ? "Enviando..." : "Solicitar agendamento"}
      </button>

      {status.message ? (
        <p className={`agendaFormStatus ${status.type}`}>{status.message}</p>
      ) : null}
    </form>
  );
}
