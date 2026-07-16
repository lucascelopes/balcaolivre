# Agenda Livre para Windows

Aplicativo WPF em .NET 8 com integrações de WhatsApp e Instagram através das Edge Functions do Supabase.

## Instalar em outro computador

Instale o Git e o SDK do .NET 8. Depois, no PowerShell:

```powershell
git clone --branch codex/agenda-livre-whatsapp-instagram --single-branch https://github.com/lucascelopes/balcaolivre.git
cd balcaolivre
dotnet restore .\AgendaLivre.Windows\AgendaLivre.Windows.csproj
dotnet run --project .\AgendaLivre.Windows\AgendaLivre.Windows.csproj -c Release
```

Para gerar uma pasta executável independente do .NET instalado:

```powershell
dotnet publish .\AgendaLivre.Windows\AgendaLivre.Windows.csproj -c Release -r win-x64 --self-contained true -o .\publish\AgendaLivre.Windows
.\publish\AgendaLivre.Windows\AgendaLivreWindows.exe
```

## Ativar os canais

- WhatsApp: abra `Configurações > Integrações`, informe o número da loja, clique em conectar e escaneie o QR Code pelo celular.
- Instagram: use uma conta profissional (Business ou Creator), clique em conectar e conclua a autorização da Meta no navegador.

Em um computador novo, a licença e o código da máquina precisam ser ativados pelo administrador antes da primeira conexão do Instagram. Essa validação impede que uma licença copiada de outro computador seja usada indevidamente.

Os dados do estabelecimento ficam somente em `%LOCALAPPDATA%\AgendaLivre.Windows`. Essa pasta não deve ser enviada ao GitHub nem copiada entre estabelecimentos.
