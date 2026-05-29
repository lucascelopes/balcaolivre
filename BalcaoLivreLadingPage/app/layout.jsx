import "./globals.css";

export const metadata = {
  title: "Balcão Livre PDV | Sistema Windows para restaurantes",
  description:
    "Balcão Livre PDV para restaurantes, bares e lanchonetes. Mesas, balcão, delivery, Pix, estoque, relatórios e impressão pela impressora do Windows.",
  icons: {
    icon: "/brand/bl-modern-icon.png",
    shortcut: "/brand/bl-modern-icon.png",
    apple: "/brand/bl-modern-icon.png"
  }
};

export default function RootLayout({ children }) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
