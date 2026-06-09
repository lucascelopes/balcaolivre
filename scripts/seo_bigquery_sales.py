"""
BigQuery SEO + vendas para Balcao Livre PDV.

Objetivo:
- juntar Search Console, eventos da landing, downloads, WhatsApp e checkout;
- descobrir palavras com muita impressao e pouco clique;
- descobrir quais paginas geram teste, WhatsApp e pagamento;
- sugerir novas landings comerciais com base em oportunidade real.

Uso:
  pip install google-cloud-bigquery
  set GOOGLE_CLOUD_PROJECT=seu-projeto
  set BQ_DATASET=balcao_livre_marketing
  python scripts/seo_bigquery_sales.py

Antes de rodar, alimente as tabelas abaixo no dataset:
- search_console_daily
- landing_events
- checkout_events
- whatsapp_events
- installer_downloads
"""

from __future__ import annotations

import os
from textwrap import dedent


DATASET = os.getenv("BQ_DATASET", "balcao_livre_marketing")
PROJECT = os.getenv("GOOGLE_CLOUD_PROJECT")


TABLES_SQL = {
    "search_console_daily": """
        CREATE TABLE IF NOT EXISTS `{project}.{dataset}.search_console_daily` (
          event_date DATE,
          query STRING,
          page STRING,
          country STRING,
          device STRING,
          impressions INT64,
          clicks INT64,
          ctr FLOAT64,
          position FLOAT64
        )
        PARTITION BY event_date
        CLUSTER BY query, page
    """,
    "landing_events": """
        CREATE TABLE IF NOT EXISTS `{project}.{dataset}.landing_events` (
          event_ts TIMESTAMP,
          session_id STRING,
          user_id STRING,
          page STRING,
          event_name STRING,
          source STRING,
          medium STRING,
          campaign STRING,
          keyword STRING,
          value FLOAT64
        )
        PARTITION BY DATE(event_ts)
        CLUSTER BY page, event_name
    """,
    "checkout_events": """
        CREATE TABLE IF NOT EXISTS `{project}.{dataset}.checkout_events` (
          event_ts TIMESTAMP,
          session_id STRING,
          customer_email STRING,
          plan STRING,
          status STRING,
          amount FLOAT64,
          landing_page STRING,
          source STRING,
          keyword STRING
        )
        PARTITION BY DATE(event_ts)
        CLUSTER BY plan, status, landing_page
    """,
    "whatsapp_events": """
        CREATE TABLE IF NOT EXISTS `{project}.{dataset}.whatsapp_events` (
          event_ts TIMESTAMP,
          session_id STRING,
          seller STRING,
          phone STRING,
          page STRING,
          message STRING,
          source STRING,
          keyword STRING
        )
        PARTITION BY DATE(event_ts)
        CLUSTER BY page, seller
    """,
    "installer_downloads": """
        CREATE TABLE IF NOT EXISTS `{project}.{dataset}.installer_downloads` (
          event_ts TIMESTAMP,
          session_id STRING,
          plan STRING,
          version STRING,
          page STRING,
          source STRING,
          keyword STRING
        )
        PARTITION BY DATE(event_ts)
        CLUSTER BY plan, page
    """,
}


VIEWS_SQL = {
    "vw_seo_query_opportunities": """
        CREATE OR REPLACE VIEW `{project}.{dataset}.vw_seo_query_opportunities` AS
        WITH base AS (
          SELECT
            LOWER(TRIM(query)) AS query,
            ANY_VALUE(page) AS best_page,
            SUM(impressions) AS impressions,
            SUM(clicks) AS clicks,
            SAFE_DIVIDE(SUM(clicks), SUM(impressions)) AS ctr,
            AVG(position) AS avg_position
          FROM `{project}.{dataset}.search_console_daily`
          WHERE event_date >= DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY)
            AND query IS NOT NULL
            AND query != ''
          GROUP BY query
        )
        SELECT
          query,
          best_page,
          impressions,
          clicks,
          ROUND(ctr * 100, 2) AS ctr_percent,
          ROUND(avg_position, 1) AS avg_position,
          ROUND(impressions * GREATEST(0.03 - ctr, 0), 0) AS missing_clicks_estimate,
          CASE
            WHEN REGEXP_CONTAINS(query, r'(anota ai|anotaai)') THEN 'pagina alternativa anota ai'
            WHEN REGEXP_CONTAINS(query, r'(consumer|consumer pdv)') THEN 'pagina alternativa consumer'
            WHEN REGEXP_CONTAINS(query, r'(gratis|gratuito|teste)') THEN 'pagina de teste gratis'
            WHEN REGEXP_CONTAINS(query, r'(pizzaria|pizza).*(delivery|entrega)|(delivery|entrega).*(pizzaria|pizza)') THEN 'pagina pizzaria delivery'
            WHEN REGEXP_CONTAINS(query, r'(delivery|entrega)') THEN 'pagina de delivery'
            WHEN REGEXP_CONTAINS(query, r'(whatsapp|zap)') THEN 'pagina de WhatsApp'
            WHEN REGEXP_CONTAINS(query, r'(ifood|i food)') THEN 'pagina de iFood'
            WHEN REGEXP_CONTAINS(query, r'(nfce|nfc-e|nota fiscal)') THEN 'pagina de NFC-e'
            WHEN REGEXP_CONTAINS(query, r'(pizzaria|pizza)') THEN 'pagina para pizzaria'
            WHEN REGEXP_CONTAINS(query, r'(lanchonete).*(pequena|simples|barata)|(pequena|simples|barata).*(lanchonete)') THEN 'pagina lanchonete pequena'
            WHEN REGEXP_CONTAINS(query, r'(lanchonete|hamburgueria|hamburguer)') THEN 'pagina para lanchonete/hamburgueria'
            WHEN REGEXP_CONTAINS(query, r'(comanda|comandas)') THEN 'pagina de comandas'
            WHEN REGEXP_CONTAINS(query, r'(estoque|controlar estoque)') THEN 'pagina de estoque'
            WHEN REGEXP_CONTAINS(query, r'(bar|espetinho|acai|acaiteria)') THEN 'pagina por segmento'
            ELSE 'melhorar pagina atual'
          END AS recommended_action
        FROM base
        WHERE impressions >= 20
        ORDER BY missing_clicks_estimate DESC, impressions DESC
    """,
    "vw_landing_sales_funnel": """
        CREATE OR REPLACE VIEW `{project}.{dataset}.vw_landing_sales_funnel` AS
        WITH pages AS (
          SELECT
            page,
            COUNTIF(event_name = 'page_view') AS views,
            COUNTIF(event_name = 'plan_click') AS plan_clicks,
            COUNTIF(event_name = 'trial_download') AS trial_clicks,
            COUNTIF(event_name = 'whatsapp_click') AS whatsapp_clicks
          FROM `{project}.{dataset}.landing_events`
          WHERE event_ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 90 DAY)
          GROUP BY page
        ),
        downloads AS (
          SELECT page, COUNT(*) AS downloads
          FROM `{project}.{dataset}.installer_downloads`
          WHERE event_ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 90 DAY)
          GROUP BY page
        ),
        sales AS (
          SELECT
            landing_page AS page,
            COUNTIF(status IN ('paid', 'approved', 'active')) AS paid_orders,
            SUM(IF(status IN ('paid', 'approved', 'active'), amount, 0)) AS revenue
          FROM `{project}.{dataset}.checkout_events`
          WHERE event_ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 90 DAY)
          GROUP BY landing_page
        )
        SELECT
          p.page,
          p.views,
          p.plan_clicks,
          p.trial_clicks,
          p.whatsapp_clicks,
          COALESCE(d.downloads, 0) AS downloads,
          COALESCE(s.paid_orders, 0) AS paid_orders,
          COALESCE(s.revenue, 0) AS revenue,
          ROUND(SAFE_DIVIDE(p.whatsapp_clicks + p.trial_clicks, p.views) * 100, 2) AS lead_rate_percent,
          ROUND(SAFE_DIVIDE(COALESCE(s.paid_orders, 0), p.views) * 100, 2) AS sale_rate_percent
        FROM pages p
        LEFT JOIN downloads d USING (page)
        LEFT JOIN sales s USING (page)
        ORDER BY revenue DESC, lead_rate_percent DESC
    """,
    "vw_keyword_to_money": """
        CREATE OR REPLACE VIEW `{project}.{dataset}.vw_keyword_to_money` AS
        WITH traffic AS (
          SELECT
            LOWER(TRIM(query)) AS keyword,
            ANY_VALUE(page) AS page,
            SUM(impressions) AS impressions,
            SUM(clicks) AS clicks,
            AVG(position) AS avg_position
          FROM `{project}.{dataset}.search_console_daily`
          WHERE event_date >= DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY)
          GROUP BY keyword
        ),
        leads AS (
          SELECT
            LOWER(TRIM(COALESCE(keyword, ''))) AS keyword,
            COUNTIF(event_name IN ('whatsapp_click', 'trial_download', 'plan_click')) AS leads
          FROM `{project}.{dataset}.landing_events`
          WHERE event_ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 90 DAY)
          GROUP BY keyword
        ),
        sales AS (
          SELECT
            LOWER(TRIM(COALESCE(keyword, ''))) AS keyword,
            COUNTIF(status IN ('paid', 'approved', 'active')) AS paid_orders,
            SUM(IF(status IN ('paid', 'approved', 'active'), amount, 0)) AS revenue
          FROM `{project}.{dataset}.checkout_events`
          WHERE event_ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 90 DAY)
          GROUP BY keyword
        )
        SELECT
          t.keyword,
          t.page,
          t.impressions,
          t.clicks,
          ROUND(t.avg_position, 1) AS avg_position,
          COALESCE(l.leads, 0) AS leads,
          COALESCE(s.paid_orders, 0) AS paid_orders,
          COALESCE(s.revenue, 0) AS revenue,
          ROUND(SAFE_DIVIDE(COALESCE(s.revenue, 0), NULLIF(t.clicks, 0)), 2) AS revenue_per_click
        FROM traffic t
        LEFT JOIN leads l USING (keyword)
        LEFT JOIN sales s USING (keyword)
        ORDER BY revenue DESC, impressions DESC
    """,
    "vw_next_20_pages": """
        CREATE OR REPLACE VIEW `{project}.{dataset}.vw_next_20_pages` AS
        WITH opportunity AS (
          SELECT
            recommended_action,
            ARRAY_AGG(query ORDER BY missing_clicks_estimate DESC LIMIT 8) AS example_queries,
            SUM(impressions) AS impressions,
            SUM(missing_clicks_estimate) AS missing_clicks_estimate,
            AVG(avg_position) AS avg_position
          FROM `{project}.{dataset}.vw_seo_query_opportunities`
          GROUP BY recommended_action
        )
        SELECT
          recommended_action,
          example_queries,
          impressions,
          missing_clicks_estimate,
          ROUND(avg_position, 1) AS avg_position,
          CASE
            WHEN recommended_action = 'pagina alternativa anota ai' THEN '/alternativa-anota-ai'
            WHEN recommended_action = 'pagina alternativa consumer' THEN '/alternativa-consumer'
            WHEN recommended_action = 'pagina de teste gratis' THEN '/pdv-gratis-para-restaurante'
            WHEN recommended_action = 'pagina pizzaria delivery' THEN '/pdv-para-pizzaria-delivery'
            WHEN recommended_action = 'pagina de delivery' THEN '/pdv-para-delivery'
            WHEN recommended_action = 'pagina de WhatsApp' THEN '/pdv-com-whatsapp'
            WHEN recommended_action = 'pagina de iFood' THEN '/pdv-com-ifood'
            WHEN recommended_action = 'pagina de NFC-e' THEN '/pdv-com-nfce'
            WHEN recommended_action = 'pagina para pizzaria' THEN '/pdv-para-pizzaria'
            WHEN recommended_action = 'pagina lanchonete pequena' THEN '/sistema-para-lanchonete-pequena'
            WHEN recommended_action = 'pagina para lanchonete/hamburgueria' THEN '/pdv-para-lanchonete'
            WHEN recommended_action = 'pagina de comandas' THEN '/programa-para-controlar-comandas'
            WHEN recommended_action = 'pagina de estoque' THEN '/como-controlar-estoque-de-lanchonete'
            WHEN recommended_action = 'pagina por segmento' THEN '/pdv-para-bar'
            ELSE '/sistema-de-caixa-para-restaurante'
          END AS suggested_url
        FROM opportunity
        ORDER BY missing_clicks_estimate DESC
        LIMIT 20
    """,
}


def format_sql(sql: str) -> str:
    if not PROJECT:
        raise SystemExit("Defina GOOGLE_CLOUD_PROJECT antes de rodar.")
    return dedent(sql).strip().format(project=PROJECT, dataset=DATASET)


def main() -> None:
    try:
        from google.cloud import bigquery
    except ImportError as exc:
        raise SystemExit(
            "Instale a dependencia: pip install google-cloud-bigquery"
        ) from exc

    client = bigquery.Client(project=PROJECT)
    dataset_ref = bigquery.Dataset(f"{PROJECT}.{DATASET}")
    dataset_ref.location = os.getenv("BQ_LOCATION", "US")
    client.create_dataset(dataset_ref, exists_ok=True)

    for name, sql in TABLES_SQL.items():
        print(f"Criando tabela {name}...")
        client.query(format_sql(sql)).result()

    for name, sql in VIEWS_SQL.items():
        print(f"Criando view {name}...")
        client.query(format_sql(sql)).result()

    print("\nPronto. Abra estas views no BigQuery:")
    for name in VIEWS_SQL:
        print(f"- {PROJECT}.{DATASET}.{name}")


if __name__ == "__main__":
    main()
