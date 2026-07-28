# Agenda Livre Android — distribuição direta personalizada

Este fluxo gera um APK privado por estabelecimento, sem cobrança antes do download. O arquivo usa o nome, o ícone e a foto enviados no pré-cadastro e é vinculado à mesma conta usada na Web e no Windows.

O teste de 7 dias começa apenas quando o APK é aberto e ativado pela primeira vez. Ao terminar o teste, o servidor bloqueia a agenda até a confirmação do pagamento. O preço e a URL de pagamento são configurações do produto e não ficam fixos no aplicativo.

## Fluxo completo

1. A pessoa cria uma conta ou entra em uma conta existente em `/agenda-livre#android-download`.
2. Ela informa o estabelecimento, envia ícone e foto e confirma que entende a instalação direta.
3. `POST /api/agenda/android/pre-register` salva as imagens no R2, cria o build no D1 e tenta disparar o workflow com o `build_id` opaco.
4. Se o disparo não estiver configurado, o agendamento do workflow assume o próximo build da fila. O claim é atômico para impedir dois executores no mesmo pedido.
5. O runner recebe o manifesto privado, aplica a personalização, compila, assina e verifica o APK.
6. O runner envia o APK bruto e o SHA-256. O backend salva o arquivo no R2 e marca o build como `ready`.
7. A página consulta o status com a conta autenticada e libera o download privado. Um F5 retoma o pedido com a sessão temporária da aba.
8. Na primeira abertura, o Android resgata uma credencial de uso único. A senha da conta nunca entra no APK.
9. A ativação inicia os 7 dias. Depois do prazo, ficam disponíveis apenas pagamento, suporte e recuperação até o servidor liberar a conta.

## Contrato da página

Os endpoints públicos exigem `Authorization: Bearer <Supabase access token>`.

### Criar o build

`POST /api/agenda/android/pre-register`, como `multipart/form-data`:

- `businessName`: nome exibido no launcher e dentro do aplicativo;
- `icon`: PNG ou JPG, até 4 MB na interface;
- `cover`: PNG ou JPG, até 8 MB na interface (`photo` é apenas alias legado no backend);
- `sideloadConsent`: `true`.

Resposta resumida:

```json
{
  "ok": true,
  "registration": { "id": "registro_opaco", "businessName": "Studio Aurora" },
  "build": {
    "id": "build_opaco",
    "status": "queued",
    "versionCode": 1204,
    "versionName": "1.0.1204"
  }
}
```

### Status e download

`GET /api/agenda/android/builds/:id` retorna:

```json
{
  "ok": true,
  "build": {
    "id": "build_opaco",
    "status": "ready",
    "appName": "Studio Aurora",
    "versionCode": 1204,
    "versionName": "1.0.1204"
  },
  "download": {
    "url": "/api/agenda/android/builds/build_opaco/download?token=...",
    "fileName": "agenda-livre-Studio-Aurora-1.0.1204.apk",
    "size": 64806096,
    "sha256": "...",
    "expiresAt": "2026-07-18T18:00:00.000Z"
  }
}
```

A interface entende `queued`, `preparing`, `building`, `signing`, `ready` e `failed`; o backend atual usa `queued`, `building`, `ready` e `failed`. Antes de cada download, a página consulta o status novamente para obter um token novo.

`GET /api/agenda/android/builds/:id/download?token=...` valida o token curto e responde `application/vnd.android.package-archive`, `Content-Disposition` e `X-Agenda-Apk-Sha256`. O APK não é asset estático público.

## Contrato do runner

Os endpoints internos exigem `Authorization: Builder <AGENDA_ANDROID_BUILDER_SECRET>`. Sessões de usuário não substituem esse segredo.

### Claim e manifesto

- Build específico: `POST /api/agenda/android/internal/builds/:id`.
- Próximo da fila: `POST /api/agenda/android/internal/builds/next`.
- Corpo: `{"workerId":"github-<run>-<tentativa>"}`.

Somente o `POST` de claim entrega o token de provisionamento:

```json
{
  "ok": true,
  "build": {
    "id": "build_opaco",
    "applicationId": "br.com.balcaolivre.agenda_livre",
    "appName": "Studio Aurora",
    "versionCode": 1204,
    "versionName": "1.0.1204",
    "branding": {
      "icon": { "url": "https://.../assets/icon", "contentType": "image/png", "sha256": "..." },
      "cover": { "url": "https://.../assets/cover", "contentType": "image/jpeg", "sha256": "..." }
    },
    "provisioning": {
      "buildId": "build_opaco",
      "token": "token-de-uso-unico",
      "expiresAt": "2026-07-21T12:00:00.000Z"
    },
    "callbacks": {
      "artifact": "https://.../artifact",
      "failure": "https://.../failure"
    }
  }
}
```

Os assets privados ficam em `GET /api/agenda/android/internal/builds/:id/assets/icon` e `/assets/cover`. O runner confere o SHA-256 de ambos.

O `versionCode` é crescente e reservado no backend. Todos os APKs mantêm `br.com.balcaolivre.agenda_livre` e o mesmo certificado, permitindo atualizações compatíveis no mesmo aparelho.

### Finalização

`POST /api/agenda/android/internal/builds/:id/artifact` recebe o APK como corpo bruto com:

- `Content-Type: application/vnd.android.package-archive`;
- `Content-Length` calculado pelo cliente HTTP;
- `X-Agenda-Apk-Sha256` com o hash hexadecimal.

O backend calcula o nome personalizado com os dados registrados e salva os bytes no R2. Falhas são notificadas em `POST /api/agenda/android/internal/builds/:id/failure` com código e mensagem genéricos, sem tokens ou logs internos.

## Personalização no runner

`AgendaLivre.Flutter/tool/prepare_android_tenant.py`:

- valida o `applicationId` estável;
- confirma que o Manifest usa `@string/agenda_app_name`, preenchido por `AGENDA_ANDROID_BUSINESS_NAME`;
- gera `ic_launcher.png` em `mipmap-mdpi`, `hdpi`, `xhdpi`, `xxhdpi` e `xxxhdpi`;
- gera `assets/branding/android_tenant_icon.png` e `android_tenant_cover.png`;
- grava `assets/branding/android_tenant.json` para auditoria interna do pacote;
- gera um arquivo temporário para `--dart-define-from-file` com as chaves lidas por `AndroidBuildConfig`;
- define o nome local `Agenda-Livre-{estabelecimento}.apk`.

Os três arquivos `android_tenant*` estão ignorados pelo Git. O JSON contém a credencial de ativação e nunca pode aparecer em commit ou log. Senha, refresh token da conta, chave privilegiada do Supabase, segredo do checkout e chave de assinatura nunca entram no APK.

## Secrets e variáveis

Secrets do workflow:

- `AGENDA_ANDROID_API_BASE_URL`;
- `AGENDA_ANDROID_BUILDER_SECRET`;
- `AGENDA_ANDROID_KEYSTORE_BASE64`;
- `AGENDA_ANDROID_STORE_PASSWORD`;
- `AGENDA_ANDROID_KEY_ALIAS`;
- `AGENDA_ANDROID_KEY_PASSWORD`;
- `AGENDA_ANDROID_CERT_SHA256`.

Variáveis do backend para disparo imediato:

- `AGENDA_ANDROID_GITHUB_TOKEN`;
- `AGENDA_ANDROID_GITHUB_REPOSITORY` no formato `dono/repositorio`;
- `AGENDA_ANDROID_GITHUB_WORKFLOW`, normalmente `agenda-android-build.yml`;
- `AGENDA_ANDROID_GITHUB_WORKFLOW_REF`, com a branch ou tag publicada.

Sem essas quatro variáveis, o agendamento do workflow continua processando a fila. A chave de assinatura deve ter backup seguro e nunca ser adicionada ao Git.

## Produto configurável

O backend controla duração do teste, URL de pagamento, suporte, licença offline, validade do provisionamento, retenção dos builds e reativação após pagamento. O download depende apenas de o build estar pronto; pagamento nunca é condição para gerar ou baixar o APK durante o pré-cadastro.
