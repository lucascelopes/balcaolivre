const description =
  "Centralize clientes, equipe, serviços e finanças no Agenda Livre. Teste grátis por 7 dias na Web ou conheça a versão para Windows.";

export const metadata = {
  title: {
    absolute: "Agenda Livre — Sua agenda. Seu tempo. Seu negócio."
  },
  description,
  keywords: [
    "sistema de agendamento",
    "agenda para salão",
    "agenda para barbearia",
    "agenda para clínica",
    "software de agenda",
    "Agenda Livre"
  ],
  alternates: {
    canonical: "/agenda-livre"
  },
  openGraph: {
    type: "website",
    locale: "pt_BR",
    url: "/agenda-livre",
    title: "Agenda Livre — Sua agenda. Seu tempo. Seu negócio.",
    description,
    images: [
      {
        url: "/agenda-livre/og.png",
        width: 1200,
        height: 630,
        alt: "Agenda Livre para Windows e Web responsiva"
      }
    ]
  },
  twitter: {
    card: "summary_large_image",
    title: "Agenda Livre — Sua agenda. Seu tempo. Seu negócio.",
    description,
    images: ["/agenda-livre/og.png"]
  },
  icons: {
    icon: "/agenda-livre/agenda-livre-mark.png",
    shortcut: "/agenda-livre/agenda-livre-mark.png",
    apple: "/agenda-livre/agenda-livre-mark.png"
  }
};

export default function AgendaLivreLayout({ children }) {
  return children;
}
