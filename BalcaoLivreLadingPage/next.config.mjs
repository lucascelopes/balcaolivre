/** @type {import('next').NextConfig} */
const adminApiUrl = (
  process.env.BALCAO_ADMIN_API_URL || "https://balcaolivrepdv.onrender.com"
).replace(/\/$/, "");

const agendaHostPattern =
  "(?<storeSlug>(?!(?:admin|api|app|cardapio|checkout|pdv|www)$)[a-z0-9][a-z0-9-]{1,61}[a-z0-9])\\.balcaolivrepdv\\.com\\.br";

const nextConfig = {
  poweredByHeader: false,
  async rewrites() {
    return {
      beforeFiles: [
        {
          source: "/",
          has: [
            {
              type: "host",
              value: agendaHostPattern
            }
          ],
          destination: "/agenda/:storeSlug"
        },
        {
          source: "/",
          missing: [
            {
              type: "query",
              key: "checkout",
              value: "sucesso"
            }
          ],
          destination: "/source-clone/page.html"
        }
      ],
      afterFiles: [
        {
          source: "/pdv",
          destination: "/pdv/index.html"
        },
        {
          source: "/pdv/",
          destination: "/pdv/index.html"
        },
        {
          source: "/admin-api/:path*",
          destination: `${adminApiUrl}/api/:path*`
        }
      ]
    };
  }
};

export default nextConfig;
