"use client";

import Image from "next/image";
import {
  ArrowLeft,
  ArrowRight,
  CalendarDays,
  Check,
  CheckCircle2,
  Clock3,
  LoaderCircle,
  MessageCircle,
  RefreshCw,
  Scissors,
  ShieldCheck,
  Sparkles,
  UserRound,
  WifiOff
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import styles from "./booking.module.css";

const money = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL"
});

function safeHex(value, fallback) {
  return /^#[0-9a-f]{6}$/i.test(String(value || "").trim())
    ? String(value).trim()
    : fallback;
}

function formatPhone(value) {
  const digits = String(value || "").replace(/\D/g, "").slice(0, 11);
  if (digits.length <= 2) return digits ? `(${digits}` : "";
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

function shortDate(dateValue) {
  const date = new Date(`${dateValue}T12:00:00`);
  if (Number.isNaN(date.getTime())) return { weekday: "Dia", day: "--", month: "" };
  return {
    weekday: new Intl.DateTimeFormat("pt-BR", { weekday: "short" })
      .format(date)
      .replace(".", ""),
    day: new Intl.DateTimeFormat("pt-BR", { day: "2-digit" }).format(date),
    month: new Intl.DateTimeFormat("pt-BR", { month: "short" })
      .format(date)
      .replace(".", "")
  };
}

function fullDate(dateValue) {
  const date = new Date(`${dateValue}T12:00:00`);
  if (Number.isNaN(date.getTime())) return dateValue;
  const text = new Intl.DateTimeFormat("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long"
  }).format(date);
  return text.charAt(0).toUpperCase() + text.slice(1);
}

function initials(value) {
  const parts = String(value || "Agenda Livre")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  return parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join("") || "AL";
}

function safeStoreLogoUrl(value) {
  const candidate = String(value || "").trim();
  return candidate.length <= 132000 && /^data:image\/png;base64,[A-Za-z0-9+/]+={0,2}$/.test(candidate)
    ? candidate
    : "";
}

function StoreAvatar({ name, logoUrl }) {
  const safeLogoUrl = safeStoreLogoUrl(logoUrl);
  const [failedLogoUrl, setFailedLogoUrl] = useState("");
  const showLogo = safeLogoUrl && failedLogoUrl !== safeLogoUrl;

  return (
    <span className={styles.storeAvatar}>
      {showLogo ? (
        <img
          className={styles.storeAvatarImage}
          src={safeLogoUrl}
          alt={`Logo de ${name}`}
          width="44"
          height="44"
          decoding="async"
          onError={() => setFailedLogoUrl(safeLogoUrl)}
        />
      ) : (
        initials(name)
      )}
    </span>
  );
}

function newIdempotencyKey() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `web-${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}

function normalizeAvailability(payload, fallbackStoreName, slug) {
  const store = payload?.store || payload?.profile || {};
  const theme = store.theme && typeof store.theme === "object" ? store.theme : {};
  const services = Array.isArray(payload?.services)
    ? payload.services
    : Array.isArray(payload?.bookingServices)
      ? payload.bookingServices
      : [];
  return {
    store: {
      slug: store.slug || slug,
      name: store.name || store.storeName || fallbackStoreName || "Agenda online",
      segment: store.segment || "Atendimento com hora marcada",
      publicUrl: store.publicUrl || `https://${slug}.minhaagendalivre.com.br`,
      generatedAt: store.generatedAt || payload?.generatedAt || "",
      theme,
      logoUrl: safeStoreLogoUrl(store.logoUrl || theme.logoUrl)
    },
    services
  };
}

function StepPill({ number, label, active, completed }) {
  return (
    <div
      className={`${styles.stepPill} ${active ? styles.stepPillActive : ""} ${completed ? styles.stepPillDone : ""}`}
      aria-current={active ? "step" : undefined}
    >
      <span>{completed ? <Check size={14} strokeWidth={3} /> : number}</span>
      <strong>{label}</strong>
    </div>
  );
}

export default function BookingFlow({ slug, fallbackStoreName }) {
  const [availability, setAvailability] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [stage, setStage] = useState(0);
  const [serviceId, setServiceId] = useState("");
  const [date, setDate] = useState("");
  const [slotId, setSlotId] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [booking, setBooking] = useState(null);
  const idempotencyKey = useRef(newIdempotencyKey());

  const loadAvailability = useCallback(async ({ quiet = false } = {}) => {
    quiet ? setRefreshing(true) : setLoading(true);
    setError("");
    try {
      const response = await fetch(`/api/agendar/${encodeURIComponent(slug)}/availability`, {
        cache: "no-store",
        headers: { Accept: "application/json" }
      });
      const payload = await response.json().catch(() => ({}));
      if (!response.ok) {
        throw new Error(
          payload?.message ||
          payload?.error?.message ||
          (typeof payload?.error === "string" ? payload.error : "") ||
          "A agenda não está disponível agora."
        );
      }
      setAvailability(normalizeAvailability(payload, fallbackStoreName, slug));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Não foi possível abrir a agenda.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [fallbackStoreName, slug]);

  useEffect(() => {
    loadAvailability();
  }, [loadAvailability]);

  const selectedService = useMemo(
    () => availability?.services?.find((service) => service.id === serviceId) || null,
    [availability, serviceId]
  );
  const selectedDay = useMemo(
    () => selectedService?.days?.find((item) => item.date === date) || null,
    [selectedService, date]
  );
  const selectedSlot = useMemo(
    () => selectedDay?.availableSlots?.find((slot) => slot.id === slotId) || null,
    [selectedDay, slotId]
  );

  useEffect(() => {
    if (!booking?.id || !booking?.statusToken || !["pending", "requested"].includes(booking.status)) {
      return undefined;
    }

    let cancelled = false;
    let attempts = 0;
    const poll = async () => {
      attempts += 1;
      try {
        const response = await fetch(
          `/api/agendar/${encodeURIComponent(slug)}/appointments/${encodeURIComponent(booking.id)}?token=${encodeURIComponent(booking.statusToken)}`,
          { cache: "no-store" }
        );
        const payload = await response.json().catch(() => ({}));
        if (!cancelled && response.ok && payload?.booking) {
          setBooking((current) => ({ ...current, ...payload.booking }));
        }
      } catch {
        // A confirmação segue no servidor; uma falha curta de rede não perde a reserva.
      }
      if (!cancelled && attempts < 40) {
        window.setTimeout(poll, 3000);
      }
    };
    const timer = window.setTimeout(poll, 1800);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [booking?.id, booking?.status, booking?.statusToken, slug]);

  const store = availability?.store || {
    name: fallbackStoreName,
    segment: "Atendimento com hora marcada",
    theme: {}
  };
  const theme = store.theme || {};
  const pageStyle = {
    "--booking-accent": safeHex(theme.accent, "#c96555"),
    "--booking-accent-deep": safeHex(theme.accentDark || theme.dark, "#a94a3d"),
    "--booking-soft": safeHex(theme.accentSoft || theme.soft, "#fce7e2"),
    "--booking-on-accent": safeHex(theme.onAccent || theme.textOnAccent, "#ffffff")
  };

  const selectService = (service) => {
    setServiceId(service.id);
    setDate("");
    setSlotId("");
    setSubmitError("");
    setStage(1);
  };

  const selectDate = (nextDate) => {
    setDate(nextDate);
    setSlotId("");
    setSubmitError("");
    setStage(2);
  };

  const selectSlot = (slot) => {
    setSlotId(slot.id);
    setSubmitError("");
    setStage(3);
  };

  const goBack = () => {
    setSubmitError("");
    if (stage === 3) {
      setSlotId("");
      setStage(2);
    } else if (stage === 2) {
      setDate("");
      setStage(1);
    } else if (stage === 1) {
      setServiceId("");
      setStage(0);
    }
  };

  const submitBooking = async (event) => {
    event.preventDefault();
    setSubmitError("");
    const phoneDigits = customerPhone.replace(/\D/g, "");
    if (customerName.trim().length < 2) {
      setSubmitError("Digite seu nome para continuar.");
      return;
    }
    if (phoneDigits.length < 10 || phoneDigits.length > 11) {
      setSubmitError("Digite um WhatsApp válido com DDD.");
      return;
    }
    if (!selectedService || !selectedSlot) {
      setSubmitError("Escolha novamente o serviço e o horário.");
      return;
    }

    setSubmitting(true);
    try {
      const response = await fetch(`/api/agendar/${encodeURIComponent(slug)}/appointments`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
          serviceId: selectedService.id,
          slotId: selectedSlot.id,
          customerName: customerName.trim(),
          customerPhone: phoneDigits,
          notes: notes.trim(),
          idempotencyKey: idempotencyKey.current
        })
      });
      const payload = await response.json().catch(() => ({}));
      if (!response.ok) {
        if (response.status === 409) {
          idempotencyKey.current = newIdempotencyKey();
          await loadAvailability({ quiet: true });
          setSlotId("");
          setStage(2);
        }
        throw new Error(
          payload?.message ||
          payload?.error?.message ||
          (typeof payload?.error === "string" ? payload.error : "") ||
          "Não foi possível reservar este horário."
        );
      }
      setBooking(payload.booking || payload);
      setStage(4);
    } catch (requestError) {
      setSubmitError(requestError instanceof Error ? requestError.message : "Não foi possível concluir o agendamento.");
    } finally {
      setSubmitting(false);
    }
  };

  const restart = () => {
    setBooking(null);
    setServiceId("");
    setDate("");
    setSlotId("");
    setCustomerName("");
    setCustomerPhone("");
    setNotes("");
    setSubmitError("");
    idempotencyKey.current = newIdempotencyKey();
    setStage(0);
    loadAvailability({ quiet: true });
  };

  if (loading) {
    return (
      <main className={styles.page} style={pageStyle} aria-busy="true">
        <div className={styles.ambientTop} />
        <section className={styles.loadingShell}>
          <div className={styles.loadingBrand} />
          <div className={styles.loadingTitle} />
          <div className={styles.loadingLine} />
          <div className={styles.loadingGrid}><div /><div /><div /><div /></div>
        </section>
      </main>
    );
  }

  if (error || !availability) {
    return (
      <main className={styles.page} style={pageStyle}>
        <div className={styles.ambientTop} />
        <section className={styles.errorCard}>
          <span className={styles.errorIcon}><WifiOff size={27} /></span>
          <p className={styles.eyebrow}>Agenda temporariamente indisponível</p>
          <h1>Vamos tentar de novo?</h1>
          <p>{error || "Esta loja ainda não publicou os horários disponíveis."}</p>
          <button type="button" onClick={() => loadAvailability()}>
            <RefreshCw size={17} /> Atualizar agenda
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.page} style={pageStyle}>
      <div className={styles.ambientTop} aria-hidden="true" />
      <div className={styles.ambientBottom} aria-hidden="true" />

      <header className={styles.header}>
        <div className={styles.headerInner}>
          <div className={styles.brandBlock}>
            <StoreAvatar name={store.name} logoUrl={store.logoUrl} />
            <span>
              <strong>{store.name}</strong>
              <small>{store.segment}</small>
            </span>
          </div>
          <div className={styles.secureBadge}>
            <ShieldCheck size={16} />
            <span>Agendamento seguro</span>
          </div>
        </div>
      </header>

      <div className={styles.shell}>
        <section className={styles.hero}>
          <div className={styles.heroCopy}>
            <p className={styles.eyebrow}><Sparkles size={15} /> Agende online em poucos passos</p>
            <h1>Qual cuidado você quer reservar?</h1>
            <p>Veja os horários livres em tempo real e escolha o melhor momento para você.</p>
          </div>
          <div className={styles.heroTrust}>
            <span><CalendarDays size={20} /></span>
            <div><strong>Horários atualizados</strong><small>Direto da agenda da loja</small></div>
          </div>
        </section>

        {stage < 4 ? (
          <nav className={styles.steps} aria-label="Etapas do agendamento">
            <StepPill number="1" label="Serviço" active={stage === 0} completed={stage > 0} />
            <span className={styles.stepLine} />
            <StepPill number="2" label="Dia" active={stage === 1} completed={stage > 1} />
            <span className={styles.stepLine} />
            <StepPill number="3" label="Horário" active={stage === 2} completed={stage > 2} />
            <span className={styles.stepLine} />
            <StepPill number="4" label="Seus dados" active={stage === 3} completed={false} />
          </nav>
        ) : null}

        {stage > 0 && stage < 4 && selectedService ? (
          <div className={styles.selectionBar}>
            <div className={styles.selectionIcon}><Scissors size={18} /></div>
            <div>
              <small>Seu agendamento</small>
              <strong>{selectedService.name}</strong>
              <span>
                {selectedService.durationMinutes} min
                {selectedSlot ? ` • ${fullDate(date)} às ${selectedSlot.time}` : ""}
              </span>
            </div>
            <strong className={styles.selectionPrice}>{money.format(Number(selectedService.price || 0))}</strong>
          </div>
        ) : null}

        <section className={styles.contentCard}>
          {stage === 0 ? (
            <div className={styles.panel}>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>Serviços</p><h2>Escolha uma opção</h2></div>
                <span>{availability.services.length} disponíveis</span>
              </div>
              {availability.services.length ? (
                <div className={styles.serviceGrid}>
                  {availability.services.map((service, index) => (
                    <button
                      className={styles.serviceCard}
                      type="button"
                      key={service.id}
                      onClick={() => selectService(service)}
                    >
                      <span className={styles.serviceIcon}>{index % 3 === 0 ? <Sparkles size={21} /> : index % 3 === 1 ? <Scissors size={21} /> : <HeartMark />}</span>
                      <span className={styles.serviceInfo}>
                        <strong>{service.name}</strong>
                        <small><Clock3 size={14} /> {service.durationMinutes} minutos</small>
                      </span>
                      <span className={styles.servicePrice}>{money.format(Number(service.price || 0))}</span>
                      <ChevronMark />
                    </button>
                  ))}
                </div>
              ) : (
                <div className={styles.emptyState}>
                  <CalendarDays size={30} />
                  <h3>Nenhum serviço com horário livre</h3>
                  <p>Peça um novo link à loja ou tente novamente mais tarde.</p>
                </div>
              )}
            </div>
          ) : null}

          {stage === 1 && selectedService ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar serviço</button>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>Datas disponíveis</p><h2>Qual é o melhor dia?</h2></div>
                <button className={styles.refreshButton} type="button" disabled={refreshing} onClick={() => loadAvailability({ quiet: true })}>
                  <RefreshCw size={15} className={refreshing ? styles.spinning : ""} /> Atualizar
                </button>
              </div>
              <div className={styles.dateGrid}>
                {(selectedService.days || []).map((item) => {
                  const parts = shortDate(item.date);
                  return (
                    <button key={item.date} className={styles.dateCard} type="button" onClick={() => selectDate(item.date)}>
                      <small>{parts.weekday}</small><strong>{parts.day}</strong><span>{parts.month}</span>
                      <em>{item.availableSlots?.length || 0} horários</em>
                    </button>
                  );
                })}
              </div>
              {!selectedService.days?.length ? (
                <div className={styles.emptyState}><CalendarDays size={30} /><h3>Sem datas disponíveis</h3><p>Novos horários serão publicados pela loja em breve.</p></div>
              ) : null}
            </div>
          ) : null}

          {stage === 2 && selectedDay ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar dia</button>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>{fullDate(date)}</p><h2>Escolha o horário</h2></div>
                <span>{selectedDay.availableSlots?.length || 0} opções</span>
              </div>
              <div className={styles.slotGrid}>
                {(selectedDay.availableSlots || []).map((slot) => (
                  <button className={styles.slotButton} type="button" key={slot.id} onClick={() => selectSlot(slot)}>
                    <Clock3 size={17} /><strong>{slot.time}</strong>
                    <small>{slot.professionalName || "Profissional disponível"}</small>
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          {stage === 3 && selectedService && selectedSlot ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar horário</button>
              <div className={styles.formGrid}>
                <div className={styles.formIntro}>
                  <p className={styles.panelKicker}>Último passo</p>
                  <h2>Para quem é este horário?</h2>
                  <p>Usaremos seu WhatsApp somente para confirmar este agendamento e enviar o lembrete.</p>
                  <div className={styles.summaryCard}>
                    <div><span><Scissors size={17} /></span><p><small>Serviço</small><strong>{selectedService.name}</strong></p></div>
                    <div><span><CalendarDays size={17} /></span><p><small>Data e horário</small><strong>{fullDate(date)}, {selectedSlot.time}</strong></p></div>
                    <div><span><UserRound size={17} /></span><p><small>Profissional</small><strong>{selectedSlot.professionalName || "Profissional disponível"}</strong></p></div>
                  </div>
                </div>

                <form className={styles.bookingForm} onSubmit={submitBooking} noValidate>
                  <label>
                    <span>Seu nome</span>
                    <input
                      value={customerName}
                      onChange={(event) => setCustomerName(event.target.value.slice(0, 100))}
                      placeholder="Como podemos chamar você?"
                      autoComplete="name"
                      required
                    />
                  </label>
                  <label>
                    <span>WhatsApp com DDD</span>
                    <input
                      value={customerPhone}
                      onChange={(event) => setCustomerPhone(formatPhone(event.target.value))}
                      placeholder="(00) 00000-0000"
                      inputMode="tel"
                      autoComplete="tel"
                      required
                    />
                    <small><MessageCircle size={13} /> A confirmação e o lembrete chegarão neste número.</small>
                  </label>
                  <label>
                    <span>Observação <em>opcional</em></span>
                    <textarea
                      value={notes}
                      onChange={(event) => setNotes(event.target.value.slice(0, 300))}
                      placeholder="Alguma preferência ou informação importante?"
                      rows={3}
                    />
                  </label>
                  {submitError ? <p className={styles.formError} role="alert">{submitError}</p> : null}
                  <button className={styles.submitButton} type="submit" disabled={submitting}>
                    {submitting ? <><LoaderCircle className={styles.spinning} size={19} /> Reservando...</> : <>Confirmar agendamento <ArrowRight size={19} /></>}
                  </button>
                  <p className={styles.privacyNote}><ShieldCheck size={14} /> Seus dados são usados somente para este atendimento.</p>
                </form>
              </div>
            </div>
          ) : null}

          {stage === 4 && booking ? (
            <BookingResult
              booking={booking}
              store={store}
              service={selectedService}
              date={date}
              slot={selectedSlot}
              customerName={customerName}
              onRestart={restart}
            />
          ) : null}
        </section>

        <footer className={styles.footer}>
          <span>Agendamento protegido por</span>
          <a href="https://minhaagendalivre.com.br" target="_blank" rel="noreferrer">
            <Image src="/agenda-livre/agenda-livre-mark.png" alt="" width={62} height={32} unoptimized />
            <strong>Agenda Livre</strong>
          </a>
        </footer>
      </div>
    </main>
  );
}

function BookingResult({ booking, store, service, date, slot, customerName, onRestart }) {
  const status = String(booking.status || "pending").toLowerCase();
  const confirmed = status === "confirmed";
  const rejected = ["rejected", "slot_conflict", "cancelled"].includes(status);

  return (
    <div className={styles.resultPanel} aria-live="polite">
      <div className={`${styles.resultIcon} ${confirmed ? styles.resultConfirmed : ""} ${rejected ? styles.resultRejected : ""}`}>
        {rejected ? <CalendarDays size={34} /> : confirmed ? <CheckCircle2 size={38} /> : <LoaderCircle className={styles.spinning} size={34} />}
      </div>
      <p className={styles.panelKicker}>{confirmed ? "Tudo certo" : rejected ? "Precisamos escolher de novo" : "Pedido recebido"}</p>
      <h2>{confirmed ? "Agendamento confirmado!" : rejected ? "Este horário não está mais livre" : "Estamos confirmando com a agenda"}</h2>
      <p className={styles.resultLead}>
        {confirmed
          ? `${customerName.split(" ")[0]}, seu horário em ${store.name} está reservado.`
          : rejected
            ? booking.message || "Outro cliente acabou de reservar esse horário. Escolha uma nova opção."
            : "Isso costuma levar apenas alguns segundos. Você receberá a confirmação no WhatsApp informado."}
      </p>
      {!rejected ? (
        <div className={styles.resultSummary}>
          <div><small>Serviço</small><strong>{booking.serviceName || service?.name}</strong></div>
          <div><small>Quando</small><strong>{fullDate(date)}, às {slot?.time}</strong></div>
          <div><small>Local</small><strong>{store.name}</strong></div>
        </div>
      ) : null}
      {confirmed ? (
        <div className={styles.whatsappNotice}>
          <MessageCircle size={22} />
          <p><strong>Fique de olho no WhatsApp</strong><span>Enviaremos também um lembrete cerca de 4 horas antes.</span></p>
        </div>
      ) : null}
      {rejected ? <button className={styles.submitButton} type="button" onClick={onRestart}>Ver outros horários <ArrowRight size={18} /></button> : null}
      {!confirmed && !rejected ? <p className={styles.waitNote}><span /> Não feche esta página enquanto confirmamos.</p> : null}
      {confirmed ? <button className={styles.linkButton} type="button" onClick={onRestart}>Fazer outro agendamento</button> : null}
    </div>
  );
}

function ChevronMark() {
  return <ArrowRight className={styles.serviceArrow} size={18} aria-hidden="true" />;
}

function HeartMark() {
  return (
    <svg viewBox="0 0 24 24" width="21" height="21" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21l7.8-7.5 1.1-1.1a5.5 5.5 0 0 0-.1-7.8Z" />
    </svg>
  );
}
