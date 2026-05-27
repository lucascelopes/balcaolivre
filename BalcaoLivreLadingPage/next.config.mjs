/** @type {import('next').NextConfig} */
const adminApiUrl = (
  process.env.BALCAO_ADMIN_API_URL || "https://balcaolivrepdv.onrender.com"
).replace(/\/$/, "");

const nextConfig = {
  poweredByHeader: false,
  async rewrites() {
    return [
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
    ];
  }
};

export default nextConfig;
