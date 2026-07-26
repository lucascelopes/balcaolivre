import { notFound } from "next/navigation";
import BookingForm from "./BookingForm";
import { formatMoneyFromCents, getAgendaStore } from "../stores";

export async function generateMetadata({ params }) {
  const { storeSlug } = await params;
  const store = getAgendaStore(storeSlug);

  return {
    title: `${store.name} | Agendamento online`,
    description: store.description,
    alternates: {
      canonical: `https://${store.slug}.balcaolivrepdv.com.br/`
    },
    openGraph: {
      title: `${store.name} | Agendamento online`,
      description: store.description,
      url: `https://${store.slug}.balcaolivrepdv.com.br/`,
      type: "website",
      locale: "pt_BR",
      images: [
        {
          url: store.imageUrl,
          width: 1200,
          height: 630,
          alt: store.name
        }
      ]
    }
  };
}

export default async function AgendaStorePage({ params }) {
  const { storeSlug } = await params;
  const store = getAgendaStore(storeSlug);

  if (!store?.slug) {
    notFound();
  }

  const topServices = store.services.slice(0, 4);

  return (
    <main
      className="agendaPublicPage"
      style={{
        "--agenda-accent": store.theme.accent,
        "--agenda-accent-strong": store.theme.accentStrong,
        "--agenda-soft": store.theme.soft
      }}
    >
      <section className="agendaShell">
        <header className="agendaTopbar">
          <a className="agendaBrandMark" href="https://balcaolivrepdv.com.br">
            <span>AL</span>
            <strong>Agenda Livre</strong>
          </a>
          <a className="agendaTopAction" href={`https://wa.me/${store.whatsapp}`}>
            WhatsApp
          </a>
        </header>

        <section className="agendaBookingLayout">
          <aside className="agendaStorePanel" aria-label={`Informacoes de ${store.name}`}>
            <div className="agendaStoreMedia">
              <img src={store.imageUrl} alt={`${store.name} pronto para atendimento`} />
              <div className="agendaStoreBadge">
                <span>Agenda aberta</span>
                <strong>{store.workdayStartHour}h as {store.workdayEndHour}h</strong>
              </div>
            </div>

            <div className="agendaStoreBody">
              <span className="agendaSegment">{store.segment}</span>
              <h1>{store.name}</h1>
              <p>{store.headline}</p>

              <div className="agendaInfoList">
                <div>
                  <span>Endereco</span>
                  <strong>{store.address}</strong>
                  <small>{store.city}</small>
                </div>
                <a href={`https://wa.me/${store.whatsapp}`}>
                  <span>Contato</span>
                  <strong>{store.phone}</strong>
                  <small>Confirmacao pelo WhatsApp</small>
                </a>
              </div>

              <div className="agendaServicePreview">
                <div className="agendaSectionTitle">
                  <span>Atendimentos</span>
                  <h2>Servicos da loja</h2>
                </div>
                <div className="agendaServiceRows">
                  {topServices.map((service) => (
                    <article key={service.id}>
                      <div>
                        <span>{service.category}</span>
                        <strong>{service.name}</strong>
                      </div>
                      <small>
                        {service.durationMinutes} min | {formatMoneyFromCents(service.priceCents)}
                      </small>
                    </article>
                  ))}
                </div>
              </div>

              <div className="agendaTeamStrip">
                {store.professionals.map((professional) => (
                  <article key={professional.id}>
                    <b>{professional.name.slice(0, 1)}</b>
                    <div>
                      <strong>{professional.name}</strong>
                      <span>{professional.role}</span>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          </aside>

          <section className="agendaBookingPanel" id="agendar" aria-labelledby="agenda-booking-title">
            <div className="agendaBookingHeader">
              <span>Agendamento online</span>
              <h2 id="agenda-booking-title">Agende na {store.name}</h2>
              <p>{store.description}</p>
            </div>
            <BookingForm store={store} />
          </section>
        </section>
      </section>
    </main>
  );
}
