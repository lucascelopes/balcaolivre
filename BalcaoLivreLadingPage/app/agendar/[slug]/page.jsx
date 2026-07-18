import { notFound } from "next/navigation";
import BookingFlow from "./BookingFlow";

export const dynamic = "force-dynamic";

function readableStoreName(slug) {
  return String(slug || "")
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export async function generateMetadata({ params }) {
  const routeParams = await params;
  const slug = String(routeParams?.slug || "").trim().toLowerCase();
  const storeName = readableStoreName(slug) || "Agenda online";

  return {
    title: { absolute: `Agendar em ${storeName}` },
    description: `Escolha um serviço, veja os horários disponíveis e agende online em ${storeName}.`,
    robots: { index: false, follow: false },
    alternates: {
      canonical: `https://${slug}.minhaagendalivre.com.br`
    }
  };
}

export default async function PublicBookingPage({ params }) {
  const routeParams = await params;
  const slug = String(routeParams?.slug || "").trim().toLowerCase();
  if (!/^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(slug)) {
    notFound();
  }

  return <BookingFlow slug={slug} fallbackStoreName={readableStoreName(slug)} />;
}
