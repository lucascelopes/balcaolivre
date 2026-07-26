const BRL = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL"
});

const defaultServices = [
  {
    id: "corte-masculino",
    name: "Corte masculino",
    category: "Barbearia",
    description: "Corte completo com acabamento na navalha.",
    durationMinutes: 35,
    priceCents: 4500,
    professionalIds: ["leo", "rafa"]
  },
  {
    id: "barba",
    name: "Barba",
    category: "Barbearia",
    description: "Modelagem, toalha quente e finalizacao.",
    durationMinutes: 25,
    priceCents: 3500,
    professionalIds: ["leo"]
  },
  {
    id: "sobrancelha",
    name: "Sobrancelha",
    category: "Beleza",
    description: "Design rapido para manter o visual alinhado.",
    durationMinutes: 30,
    priceCents: 4500,
    professionalIds: ["naya"]
  },
  {
    id: "manicure",
    name: "Manicure",
    category: "Beleza",
    description: "Cutilagem, esmalte e acabamento.",
    durationMinutes: 45,
    priceCents: 5500,
    professionalIds: ["naya"]
  }
];

const defaultProfessionals = [
  {
    id: "leo",
    name: "Leo Barber",
    role: "Barbeiro"
  },
  {
    id: "rafa",
    name: "Rafa Cortes",
    role: "Barbeiro"
  },
  {
    id: "naya",
    name: "Naya Beauty",
    role: "Designer"
  }
];

const stores = {
  nomedaloja: {
    slug: "nomedaloja",
    name: "Nome da Loja",
    segment: "Salao e barbearia",
    headline: "Agende seu horario sem espera",
    description:
      "Escolha o servico, profissional e horario. A loja recebe seu pedido e confirma pelo WhatsApp.",
    address: "Rua Principal, 120 - Centro",
    city: "Governador Valadares - MG",
    phone: "(33) 99960-9457",
    whatsapp: "5533999609457",
    imageUrl:
      "https://images.unsplash.com/photo-1512690459411-b9245aed614b?auto=format&fit=crop&w=1200&q=82",
    theme: {
      accent: "#0f766e",
      accentStrong: "#0b5f59",
      soft: "#e7f7f4"
    },
    workdayStartHour: 9,
    workdayEndHour: 19,
    slotMinutes: 30,
    availableWeekdays: [1, 2, 3, 4, 5, 6],
    services: defaultServices,
    professionals: defaultProfessionals,
    bookedSlots: [
      {
        dateOffset: 1,
        time: "10:00",
        professionalId: "leo"
      },
      {
        dateOffset: 2,
        time: "14:30",
        professionalId: "naya"
      }
    ]
  },
  "lucas-barbearia": {
    slug: "lucas-barbearia",
    name: "Lucas Barbearia",
    segment: "Barbearia",
    headline: "Agende seu corte ou barba sem fila",
    description:
      "Escolha o servico, profissional e horario. A Lucas Barbearia recebe seu pedido e confirma pelo WhatsApp.",
    address: "Rua Principal, 120 - Centro",
    city: "Governador Valadares - MG",
    phone: "(33) 99960-9457",
    whatsapp: "5533999609457",
    imageUrl:
      "https://images.unsplash.com/photo-1512690459411-b9245aed614b?auto=format&fit=crop&w=1200&q=82",
    theme: {
      accent: "#0f766e",
      accentStrong: "#0b5f59",
      soft: "#e7f7f4"
    },
    workdayStartHour: 9,
    workdayEndHour: 19,
    slotMinutes: 30,
    availableWeekdays: [1, 2, 3, 4, 5, 6],
    services: defaultServices,
    professionals: defaultProfessionals,
    bookedSlots: [
      {
        dateOffset: 1,
        time: "10:00",
        professionalId: "leo"
      },
      {
        dateOffset: 2,
        time: "14:30",
        professionalId: "naya"
      }
    ]
  }
};

export const agendaStoreSlugs = Object.keys(stores);

function titleFromSlug(slug) {
  return String(slug || "loja")
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function formatMoneyFromCents(value) {
  return BRL.format((Number(value) || 0) / 100);
}

export function getAgendaStore(slug) {
  const cleanSlug = String(slug || "nomedaloja")
    .toLowerCase()
    .replace(/[^a-z0-9-]/g, "")
    .replace(/^-+|-+$/g, "");
  const configuredStore = stores[cleanSlug];

  if (configuredStore) {
    return configuredStore;
  }

  const name = titleFromSlug(cleanSlug);

  return {
    ...stores.nomedaloja,
    slug: cleanSlug || "nomedaloja",
    name: name || "Nome da Loja",
    headline: `Agende seu horario na ${name || "loja"}`,
    description:
      "Pagina publica de agendamento pronta para conectar esta loja ao Agenda Livre.",
    bookedSlots: []
  };
}

export function findService(store, serviceId) {
  return store.services.find((service) => service.id === serviceId) || null;
}

export function findProfessional(store, professionalId) {
  return (
    store.professionals.find((professional) => professional.id === professionalId) ||
    null
  );
}
