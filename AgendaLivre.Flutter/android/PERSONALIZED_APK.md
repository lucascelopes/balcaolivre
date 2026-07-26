# APK Android personalizado

O mesmo `applicationId` e o mesmo certificado de release devem ser usados em
todos os APKs. O nome, a marca e o token de provisionamento mudam por
estabelecimento.

Exemplo de build (os valores reais devem vir do job seguro de build):

```powershell
flutter build apk --release `
  --build-number 42 `
  --dart-define=AGENDA_ANDROID_BUILD_ID=build_opaque_id `
  --dart-define=AGENDA_ANDROID_PROVISIONING_TOKEN=one_time_short_lived_token `
  --dart-define=AGENDA_ANDROID_BUSINESS_NAME="Nome do estabelecimento" `
  --dart-define=AGENDA_LIVRE_API_BASE=https://app.minhaagendalivre.com.br
```

O nome do launcher usa `AGENDA_ANDROID_BUSINESS_NAME`. Os arquivos
`mipmap-*/ic_launcher.png` devem ser gerados a partir do ícone sanitizado antes
do build. A foto e o logo completos são retornados novamente pelo servidor no
provisionamento; assim, a marca pode ser atualizada sem guardar segredos no APK.

Assinatura pode ser configurada por `android/key.properties` (arquivo ignorado)
ou pelas variáveis `AGENDA_ANDROID_KEYSTORE_PATH`,
`AGENDA_ANDROID_STORE_PASSWORD`, `AGENDA_ANDROID_KEY_ALIAS` e
`AGENDA_ANDROID_KEY_PASSWORD`. Um build release falha quando elas não existem;
ele nunca usa a chave de debug.

Para desenvolvimento local sem provisionamento, use somente em debug:

```powershell
flutter run --dart-define=AGENDA_ANDROID_DEV_MODE=true
```
