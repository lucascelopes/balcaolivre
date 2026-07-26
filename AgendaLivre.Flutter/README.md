# Agenda Livre Flutter

Versão responsiva do Agenda Livre para Web, Android e iOS. No desktop Web, o shell, as páginas e os fluxos principais reproduzem a identidade e a hierarquia visual do aplicativo Windows WPF.

## Funcionalidades

- Início com resumo do dia, indicadores e próximos atendimentos.
- Agenda em quadro, lista e semana, com conflitos, status e CRUD de atendimentos.
- Financeiro e relatórios com filtros, indicadores e gráficos.
- Cadastro de serviços, profissionais, clientes, recursos, horários e despesas.
- Marketing com campanhas e mensagens personalizadas.
- Configurações, temas, onboarding, busca de endereço por CEP e backup JSON.
- Dias de atendimento, expediente e intervalo aplicados à criação e à oferta de horários.
- Integração com WhatsApp por `wa.me` e suporte a proxy seguro.
- Apoio ao fluxo de conteúdo do Instagram sem expor credenciais no navegador.
- Layout adaptativo com navegação própria para desktop, tablet e celular.
- A mesma conta, o mesmo conteúdo e o mesmo contrato de dados do aplicativo Windows WPF.
- Sincronização automática pela nuvem entre Windows, Web e dispositivos móveis, com cópia de recuperação local.

## Executar

Requer Flutter 3.38 ou compatível.

```powershell
flutter pub get
flutter run -d chrome
```

Para Android, com Android Studio, SDK e licenças configurados:

```powershell
flutter run -d android
```

Para gerar a versão Web:

```powershell
flutter build web --release
```

O projeto iOS está incluído, mas a compilação e assinatura precisam ser feitas em um macOS com Xcode.

## Dados compartilhados com o Agenda Livre Windows

Ao entrar com a mesma conta, o Flutter baixa e mantém automaticamente o mesmo conteúdo do aplicativo Windows: configurações, clientes, profissionais, serviços, agenda, financeiro, WhatsApp e demais cadastros do contrato compartilhado. Enquanto estiverem ativos, Windows e Flutter consultam novas revisões da conta a cada 10 segundos, além da atualização imediata ao abrir ou retomar o aplicativo.

Em uma divergência de revisão, a nuvem é aplicada automaticamente, como no WPF, e a versão local anterior permanece guardada como cópia de recuperação. A opção **Configurações > Dados e backup > Importar backup JSON** continua disponível apenas para migração de instalações antigas ou recuperação manual.

## Integrações

- ViaCEP funciona diretamente para preenchimento de endereço.
- WhatsApp abre uma conversa via `wa.me` por padrão; envio automático exige um proxy seguro configurado.
- Instagram abre o canal oficial para concluir a publicação; OAuth e mensagens automáticas exigem o backend seguro da conta.
- Mercado Pago possui a camada de serviço preparada, mas credenciais e criação de cobranças devem permanecer no backend.

## Validar

```powershell
flutter analyze
flutter test
flutter build web --release
```
