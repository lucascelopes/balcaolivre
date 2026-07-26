import Link from "next/link";

export default function NotFound() {
  return (
    <main className="notFoundPage">
      <section>
        <span>Balcao Livre</span>
        <h1>Pagina nao encontrada</h1>
        <p>O endereco pode ter mudado ou nao existe mais.</p>
        <Link href="/">Voltar para o inicio</Link>
      </section>
    </main>
  );
}
