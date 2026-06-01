import { siteUrl } from "./seo";

const routes = [
  { path: "/", priority: 1, changeFrequency: "weekly" },
  { path: "/como-usar/", priority: 0.85, changeFrequency: "monthly" },
  { path: "/termos/", priority: 0.65, changeFrequency: "yearly" }
];

export default function sitemap() {
  const lastModified = new Date();

  return routes.map((route) => ({
    url: `${siteUrl}${route.path}`,
    lastModified,
    changeFrequency: route.changeFrequency,
    priority: route.priority
  }));
}
