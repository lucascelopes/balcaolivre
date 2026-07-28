import Link from "next/link";

export default function BookingNotFound() {
  return (
    <main
      style={{
        minHeight: "100vh",
        display: "grid",
        placeItems: "center",
        padding: 24,
        color: "#211b18",
        background: "#fffaf7",
        fontFamily: "Inter, ui-sans-serif, system-ui, sans-serif"
      }}
    >
      <section style={{ maxWidth: 520, textAlign: "center" }}>
        <p style={{ color: "#b95542", fontWeight: 800 }}>Agenda indisponível</p>
        <h1 style={{ margin: "8px 0 12px", fontSize: "clamp(2rem, 6vw, 3.5rem)" }}>
          Não encontramos esta loja
        </h1>
        <p style={{ color: "#6f625c", lineHeight: 1.7 }}>
          Confira o endereço recebido ou peça um novo link de agendamento ao estabelecimento.
        </p>
        <Link
          href="https://minhaagendalivre.com.br"
          style={{
            display: "inline-flex",
            marginTop: 24,
            padding: "13px 20px",
            borderRadius: 14,
            color: "#fff",
            background: "#bf5d4e",
            fontWeight: 800,
            textDecoration: "none"
          }}
        >
          Conhecer o Agenda Livre
        </Link>
      </section>
    </main>
  );
}
