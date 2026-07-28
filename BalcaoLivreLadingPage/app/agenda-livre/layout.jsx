const description =
  "Centralize agenda, clientes, equipe e finanças. Seus clientes também podem agendar online pelo endereço personalizado da sua loja. Teste grátis por 7 dias.";

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
        url: "/agenda-livre/og-v2.png",
        width: 1730,
        height: 909,
        alt: "Agenda Livre para Windows, Web e celular"
      }
    ]
  },
  twitter: {
    card: "summary_large_image",
    title: "Agenda Livre — Sua agenda. Seu tempo. Seu negócio.",
    description,
    images: ["/agenda-livre/og-v2.png"]
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
