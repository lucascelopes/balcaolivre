"""
Melhora automaticamente as paginas SEO da landing.

Fluxo:
1. Le as oportunidades do BigQuery.
2. Escolhe a URL/slug correspondente.
3. Gera overrides comerciais de titulo, descricao, H1, keywords e FAQ.
4. Salva em BalcaoLivreLadingPage/app/seoPageInsights.json.

As paginas do Next ja carregam esse JSON no build.

Uso:
  pip install google-cloud-bigquery
  set GOOGLE_CLOUD_PROJECT=seu-projeto
  set BQ_DATASET=balcao_livre_marketing
  python scripts/seo_auto_improve_pages.py

Para testar sem BigQuery:
  python scripts/seo_auto_improve_pages.py --sample
"""

from __future__ import annotations

import argparse
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
SEO_PAGES_JS = ROOT / "BalcaoLivreLadingPage" / "app" / "seoPages.js"
INSIGHTS_JSON = ROOT / "BalcaoLivreLadingPage" / "app" / "seoPageInsights.json"
PROJECT = os.getenv("GOOGLE_CLOUD_PROJECT")
DATASET = os.getenv("BQ_DATASET", "balcao_livre_marketing")


SAMPLE_ROWS = [
    {
        "suggested_url": "/pdv-delivery-gratuito",
        "example_queries": ["pdv delivery gratuito", "sistema delivery gratis", "pdv delivery gratis"],
        "impressions": 220,
        "missing_clicks_estimate": 12,
    },
    {
        "suggested_url": "/pdv-para-restaurante",
        "example_queries": ["pdv para restaurante", "sistema para restaurante", "software restaurante"],
        "impressions": 190,
        "missing_clicks_estimate": 8,
    },
    {
        "suggested_url": "/pdv-com-whatsapp",
        "example_queries": ["pdv com whatsapp", "whatsapp para restaurante", "pedido pelo whatsapp restaurante"],
        "impressions": 160,
        "missing_clicks_estimate": 7,
    },
    {
        "suggested_url": "/alternativa-anota-ai",
        "example_queries": ["alternativa anota ai", "sistema parecido com anota ai", "pdv com whatsapp"],
        "impressions": 120,
        "missing_clicks_estimate": 6,
    },
    {
        "suggested_url": "/sistema-para-lanchonete-pequena",
        "example_queries": ["sistema para lanchonete pequena", "pdv lanchonete pequena", "caixa para lanchonete"],
        "impressions": 96,
        "missing_clicks_estimate": 5,
    },
]


def slug_from_url(url: str) -> str | None:
    match = re.search(r"/([^/?#]+)/?$", url or "")
    return match.group(1) if match else None


def existing_slugs() -> set[str]:
    text = SEO_PAGES_JS.read_text(encoding="utf-8")
    return set(re.findall(r'slug:\s*"([^"]+)"', text))


def title_case_pt(text: str) -> str:
    small = {"de", "da", "do", "das", "dos", "para", "com", "e", "no", "na"}
    words = []
    for index, word in enumerate(text.strip().split()):
        lower = word.lower()
        if index > 0 and lower in small:
            words.append(lower)
        else:
            words.append(lower[:1].upper() + lower[1:])
    return " ".join(words)


def normalize_query(query: str) -> str:
    return re.sub(r"\s+", " ", (query or "").strip().lower())


def build_override(slug: str, queries: list[str], row: dict[str, Any]) -> dict[str, Any]:
    main_query = normalize_query(queries[0] if queries else slug.replace("-", " "))
    pretty_query = title_case_pt(main_query)
    is_free = any(term in main_query for term in ["gratis", "gratuito", "grátis"])
    is_whatsapp = "whatsapp" in main_query or "zap" in main_query
    is_ifood = "ifood" in main_query or "i food" in main_query
    is_fiscal = "nfce" in main_query or "nfc-e" in main_query or "nota fiscal" in main_query

    benefit = "com caixa Windows, estoque, comprovante e suporte na implantação"
    if is_whatsapp:
        benefit = "com atendimento no WhatsApp, cardápio, pedidos e caixa Windows"
    elif is_ifood:
        benefit = "com delivery, iFood, status de pedido, caixa Windows e relatórios"
    elif is_fiscal:
        benefit = "com NFC-e configurável, caixa Windows, estoque e fechamento"

    price_line = "teste grátis por 7 dias" if is_free else "teste grátis por 7 dias e planos a partir de R$17/mês"

    return {
        "metaTitle": f"{pretty_query} | Balcão Livre PDV"[:68],
        "description": (
            f"{pretty_query} para restaurante: {benefit}. "
            f"Comece com {price_line}."
        )[:158],
        "h1": f"{pretty_query} para vender mais sem complicar o caixa",
        "lead": (
            f"Para quem procura {main_query}, o Balcão Livre PDV organiza a rotina "
            f"da loja em um fluxo direto: vender no Windows, acompanhar pedidos, "
            f"controlar estoque e fechar o caixa com menos retrabalho."
        ),
        "keywords": [normalize_query(query) for query in queries if query],
        "outcomes": [
            "Mais cliques vindos do Google",
            "Mensagem mais direta para quem quer comprar",
            "CTA claro para teste, WhatsApp e plano mensal",
        ],
        "faq": [
            [
                f"O {pretty_query} tem teste grátis?",
                "Sim. O cliente pode testar por 7 dias antes de contratar.",
            ],
            [
                "Qual plano escolher?",
                "Para caixa local, comece pelo plano de entrada. Para cardápio online, WhatsApp, garçom e integrações, use o Restaurante Profissional.",
            ],
            [
                "Funciona no Windows?",
                "Sim. O Balcão Livre PDV tem instalador Windows para operação de caixa.",
            ],
        ],
        "updatedAt": datetime.now(timezone.utc).isoformat(),
        "autoReason": {
            "impressions": int(row.get("impressions") or 0),
            "missingClicksEstimate": float(row.get("missing_clicks_estimate") or 0),
            "mainQuery": main_query,
        },
    }


def load_bigquery_rows(limit: int) -> list[dict[str, Any]]:
    if not PROJECT:
        raise SystemExit("Defina GOOGLE_CLOUD_PROJECT ou rode com --sample.")

    try:
        from google.cloud import bigquery
    except ImportError as exc:
        raise SystemExit("Instale: pip install google-cloud-bigquery") from exc

    sql = f"""
      SELECT suggested_url, example_queries, impressions, missing_clicks_estimate
      FROM `{PROJECT}.{DATASET}.vw_next_20_pages`
      ORDER BY missing_clicks_estimate DESC, impressions DESC
      LIMIT @limit
    """
    client = bigquery.Client(project=PROJECT)
    job = client.query(
        sql,
        job_config=bigquery.QueryJobConfig(
            query_parameters=[bigquery.ScalarQueryParameter("limit", "INT64", limit)]
        ),
    )
    return [dict(row.items()) for row in job.result()]


def write_insights(rows: list[dict[str, Any]], dry_run: bool) -> dict[str, Any]:
    slugs = existing_slugs()
    pages: dict[str, Any] = {}

    for row in rows:
        slug = slug_from_url(row.get("suggested_url", ""))
        if not slug or slug not in slugs:
            continue
        queries = list(row.get("example_queries") or [])
        pages[slug] = build_override(slug, queries, row)

    payload = {
        "updatedAt": datetime.now(timezone.utc).isoformat(),
        "source": "bigquery_auto" if not dry_run else "dry_run",
        "pages": pages,
    }

    if not dry_run:
        INSIGHTS_JSON.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    return payload


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sample", action="store_true", help="gera melhorias de exemplo sem BigQuery")
    parser.add_argument("--dry-run", action="store_true", help="mostra sem gravar o JSON")
    parser.add_argument("--limit", type=int, default=20)
    args = parser.parse_args()

    rows = SAMPLE_ROWS if args.sample else load_bigquery_rows(args.limit)
    payload = write_insights(rows, args.dry_run)

    print(f"Paginas melhoradas: {len(payload['pages'])}")
    for slug, page in payload["pages"].items():
        print(f"- /{slug}/ -> {page['autoReason']['mainQuery']}")


if __name__ == "__main__":
    main()
