import { siteUrl } from "./seo";
import { seoPages } from "./seoPages";

const routes = [
  { path: "/", priority: 1, changeFrequency: "weekly" },
  { path: "/como-usar/", priority: 0.85, changeFrequency: "monthly" },
  { path: "/termos/", priority: 0.65, changeFrequency: "yearly" },
  ...seoPages.map((page) => ({
    path: `/${page.slug}/`,
    priority: page.plan === "online" ? 0.82 : 0.78,
    changeFrequency: "weekly"
  }))
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
