# Balcao Livre Android

Modulo inicial nativo Android/Kotlin para migrar o PDV Windows para mobile.

## Primeira entrega

- Login do restaurante por chave de licenca usando o admin existente em `/api/app/activate`.
- Envio do perfil da loja no mesmo formato usado pelo Windows.
- Identidade do aparelho Android com `machineHash` e `machineCode`.
- Sessao local persistida em `SharedPreferences`.
- Check-in no admin por `/api/app/checkin`.
- Tela inicial nativa com resumo do restaurante e pontos de entrada para mesas, produtos, pedidos e caixa.

## Como abrir

Abra a pasta `BalcaoLivre.Android` no Android Studio. Use o JDK embutido do Android Studio ou JDK 17+ para o Gradle/Android Gradle Plugin.

O build por terminal nesta maquina ainda precisa de `ANDROID_HOME`/Android SDK e Gradle configurados.
