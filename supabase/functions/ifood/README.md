# iFood Supabase backend

Endpoint publico esperado:

```text
https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood
https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood/webhook
```

Variaveis obrigatorias no Supabase:

```text
IFOOD_CLIENT_ID
IFOOD_CLIENT_SECRET
IFOOD_PUBLIC_FUNCTION_URL=https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood
```

Deploy:

```powershell
$env:SUPABASE_ACCESS_TOKEN="seu_token_de_deploy"
npx supabase db push --project-ref hzvplpotsdzxygkxrgyi
npx supabase secrets set IFOOD_CLIENT_ID="..." IFOOD_CLIENT_SECRET="..." IFOOD_PUBLIC_FUNCTION_URL="https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood" --project-ref hzvplpotsdzxygkxrgyi
npx supabase functions deploy ifood --project-ref hzvplpotsdzxygkxrgyi
```

O app Windows nao mostra nem salva Client ID/Secret. O cliente final usa somente o botao **Conectar iFood**.
