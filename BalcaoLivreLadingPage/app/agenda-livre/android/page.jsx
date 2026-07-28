import Image from "next/image";
import { ArrowLeft } from "lucide-react";
import AndroidPreRegistration from "../AndroidPreRegistration";
import styles from "./page.module.css";

export const metadata = {
  title: "Aplicativo Android personalizado — Agenda Livre",
  description:
    "Prepare um aplicativo Android do Agenda Livre com o nome, o ícone e a foto do seu negócio.",
  alternates: {
    canonical: "/agenda-livre/android",
  },
};

export default function AgendaLivreAndroidPage() {
  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <a className={styles.backLink} href="/agenda-livre">
          <ArrowLeft size={17} aria-hidden="true" />
          Voltar para a Agenda Livre
        </a>
        <a className={styles.brand} href="/agenda-livre" aria-label="Agenda Livre">
          <Image
            src="/agenda-livre/agenda-livre-mark.png"
            unoptimized
            width={900}
            height={480}
            alt=""
          />
          <span><strong>Agenda Livre</strong><small>Android personalizado</small></span>
        </a>
      </header>
      <AndroidPreRegistration />
    </main>
  );
}
