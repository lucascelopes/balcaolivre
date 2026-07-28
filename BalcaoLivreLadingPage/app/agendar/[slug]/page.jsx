import { notFound } from "next/navigation";
import { getCatalogMetadata } from "../../lib/agenda-booking-server";
import BookingFlow from "./BookingFlow";

export const dynamic = "force-dynamic";

function readableStoreName(slug) {
  return String(slug || "")
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

async function legacyGenerateMetadata({ params }) {
  const routeParams = await params;
  const slug = String(routeParams?.slug || "").trim().toLowerCase();
  const storeName = readableStoreName(slug) || "Agenda online";

  return {
    title: { absolute: `${storeName} | Catálogo e agendamento` },
    description: `Escolha um serviço, veja os horários disponíveis e agende online em ${storeName}.`,
    robots: { index: true, follow: true },
    alternates: {
      canonical: `https://${slug}.minhaagendalivre.com.br`
    }
  };
}

export async function generateMetadata({ params }) {
  const routeParams = await params;
  const slug = String(routeParams?.slug || "").trim().toLowerCase();
  const fallback = await legacyGenerateMetadata({ params: Promise.resolve(routeParams) });
  const metadata = await getCatalogMetadata(slug).catch(() => null);
  if (!metadata) return fallback;
  const canonical = `https://${slug}.minhaagendalivre.com.br`;
  const imageUrl = metadata.imageUrl ? `${canonical}${metadata.imageUrl}` : "";
  return {
    title: { absolute: metadata.title },
    description: metadata.description,
    robots: { index: true, follow: true },
    alternates: { canonical },
    openGraph: {
      title: metadata.title,
      description: metadata.description,
      url: canonical,
      type: "website",
      ...(imageUrl ? { images: [{ url: imageUrl, alt: metadata.name }] } : {})
    },
    twitter: {
      card: imageUrl ? "summary_large_image" : "summary",
      title: metadata.title,
      description: metadata.description,
      ...(imageUrl ? { images: [imageUrl] } : {})
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
