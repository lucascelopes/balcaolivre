import { NextResponse } from "next/server";
import { findProfessional, findService, getAgendaStore } from "../../../agenda/stores";

export const dynamic = "force-dynamic";

function onlyDigits(value) {
  return String(value || "").replace(/\D/g, "");
}

function validDate(value) {
  return /^\d{4}-\d{2}-\d{2}$/.test(String(value || ""));
}

function validTime(value) {
  return /^\d{2}:\d{2}$/.test(String(value || ""));
}

function protocol() {
  return `AG${Date.now().toString(36).toUpperCase()}`;
}

function supabaseConfig() {
  const url =
    process.env.SUPABASE_URL ||
    process.env.BVPDV_SUPABASE_URL ||
    process.env.NEXT_PUBLIC_SUPABASE_URL;
  const serviceKey =
    process.env.SUPABASE_SERVICE_ROLE_KEY ||
    process.env.SUPABASE_SECRET_KEY ||
    process.env.BVPDV_SUPABASE_SERVICE_ROLE_KEY ||
    process.env.BVPDV_SUPABASE_SECRET_KEY;

  if (!url || !serviceKey) {
    return null;
  }

  return {
    url: url.replace(/\/$/, ""),
    serviceKey
  };
}

async function saveBooking(row) {
  const config = supabaseConfig();
  const tableName =
    process.env.AGENDA_PUBLIC_BOOKING_TABLE || "agenda_public_booking_requests";

  if (!/^[a-zA-Z0-9_]+$/.test(tableName)) {
    throw new Error("Tabela de agendamento invalida.");
  }

  if (!config) {
    return { stored: false };
  }

  const response = await fetch(
    `${config.url}/rest/v1/${tableName}`,
    {
      method: "POST",
      headers: {
        apikey: config.serviceKey,
        Authorization: `Bearer ${config.serviceKey}`,
        "Content-Type": "application/json",
        Prefer: "return=representation"
      },
      body: JSON.stringify(row)
    }
  );

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || "Falha ao gravar o agendamento.");
  }

  return { stored: true };
}

export async function POST(request) {
  try {
    const body = await request.json();
    const store = getAgendaStore(body.storeSlug);
    const service = findService(store, body.serviceId);
    const professional = findProfessional(store, body.professionalId);
    const customerName = String(body.customerName || "").trim();
    const customerPhone = onlyDigits(body.customerPhone);
    const customerNotes = String(body.customerNotes || "").trim();

    if (!service) {
      return NextResponse.json({ error: "Servico invalido." }, { status: 400 });
    }

    if (!professional || !service.professionalIds.includes(professional.id)) {
      return NextResponse.json({ error: "Profissional invalido." }, { status: 400 });
    }

    if (!validDate(body.date) || !validTime(body.time)) {
      return NextResponse.json({ error: "Data ou horario invalido." }, { status: 400 });
    }

    if (customerName.length < 3) {
      return NextResponse.json({ error: "Informe seu nome." }, { status: 400 });
    }

    if (customerPhone.length < 10 || customerPhone.length > 13) {
      return NextResponse.json({ error: "Informe um WhatsApp valido." }, { status: 400 });
    }

    const bookingProtocol = protocol();
    const sourceHost = request.headers.get("host") || "";
    const userAgent = request.headers.get("user-agent") || "";
    const scheduledStart = `${body.date}T${body.time}:00-03:00`;
    const row = {
      protocol: bookingProtocol,
      status: "novo",
      store_slug: store.slug,
      store_name: store.name,
      service_id: service.id,
      service_name: service.name,
      professional_id: professional.id,
      professional_name: professional.name,
      scheduled_date: body.date,
      scheduled_time: `${body.time}:00`,
      scheduled_start: scheduledStart,
      duration_minutes: service.durationMinutes,
      price_cents: service.priceCents,
      customer_name: customerName,
      customer_phone: customerPhone,
      customer_notes: customerNotes,
      source_host: sourceHost,
      user_agent: userAgent,
      metadata: {
        segment: store.segment,
        source: "agenda_public_site"
      }
    };

    const saved = await saveBooking(row);

    return NextResponse.json({
      ok: true,
      protocol: bookingProtocol,
      stored: saved.stored
    });
  } catch (error) {
    return NextResponse.json(
      { error: error.message || "Nao foi possivel enviar o agendamento." },
      { status: 500 }
    );
  }
}
