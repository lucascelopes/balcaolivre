using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const int MarketingSiteImageMaximumDimension = 2200;
    private const long MarketingSiteImageMaximumSourceBytes = 30L * 1024 * 1024;
    private const int MarketingSiteMaximumSectionImages = 6;
    private const int MarketingSiteMaximumSiteImages = 24;
    private const int MarketingSiteMaximumItemsPerSection = 8;
    private const int MarketingSiteBuilderVersion = 2;
    private static readonly HashSet<string> MarketingSiteImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp"
    };

    private DispatcherTimer? _marketingSiteSaveTimer;
    private bool _loadingMarketingSiteEditor;
    private string _marketingSiteAccentColor = "#FF6B4A";
    private string _onlineBookingLastSyncedCatalogHeroFingerprint = "";
    private string _onlineBookingCachedCatalogHeroFingerprint = "";
    private string _onlineBookingCachedCatalogHeroDataUrl = "";
    private string _marketingSiteSelectedPart = "hero";
    private string _marketingSiteSelectedSectionId = "";
    private string _marketingSiteSelectedItemId = "";
    private bool _updatingMarketingSiteBuilderControls;
    private bool _marketingSiteHasUnpublishedChanges;
    private string _marketingSiteSectionDrawerCategory = "essential";
    private string _marketingSiteSectionDrawerSelectedType = "team";

    private void EnsureMarketingCatalogAddressState()
    {
        var settings = _data.Settings;
        var changed = false;
        var fallbackSlug = string.IsNullOrWhiteSpace(settings.PublicBookingSlug)
            ? SlugifyPublicBookingStore(BusinessDisplayName())
            : SlugifyPublicBookingStore(settings.PublicBookingSlug);

        if (settings.PublishedMarketingCatalog is null &&
            settings.MarketingSitePublishedAt is { } legacyPublishedAt)
        {
            settings.PublishedMarketingCatalog = new MarketingCatalogPublication
            {
                AddressSnapshotVersion = 1,
                Slug = fallbackSlug,
                CustomDomain = NormalizeMarketingSiteCustomDomain(settings.PublicBookingCustomDomain),
                Title = settings.MarketingSiteTitle,
                SupportText = settings.MarketingSiteSupportText,
                ButtonText = settings.MarketingSiteButtonText,
                HeroImagePath = settings.MarketingSiteHeroImagePath,
                AccentColor = settings.MarketingSiteAccentColor,
                Alignment = settings.MarketingSiteAlignment,
                Spacing = settings.MarketingSiteSpacing,
                TitleFont = settings.MarketingSiteTitleFont,
                ImageContrast = settings.MarketingSiteImageContrast,
                ShowButton = settings.MarketingSiteShowButton,
                Header = CloneMarketingCatalogHeader(settings.MarketingSiteHeader),
                Footer = CloneMarketingCatalogFooter(settings.MarketingSiteFooter),
                Design = CloneMarketingCatalogDesign(settings.MarketingSiteDesign),
                Sections = CloneMarketingCatalogSections(settings.MarketingSiteSections),
                SeoTitle = settings.MarketingSiteSeoTitle,
                SeoDescription = settings.MarketingSiteSeoDescription,
                Promotion = settings.MarketingSitePromotion.IsPublished
                    ? CloneMarketingSitePromotion(settings.MarketingSitePromotion)
                    : null,
                PublishedAt = legacyPublishedAt
            };
            changed = true;
        }

        if (settings.PublishedMarketingCatalog is { AddressSnapshotVersion: < 1 } publication)
        {
            publication.AddressSnapshotVersion = 1;
            publication.Slug = fallbackSlug;
            publication.CustomDomain = NormalizeMarketingSiteCustomDomain(settings.PublicBookingCustomDomain);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.MarketingSiteDraftSlug))
        {
            settings.MarketingSiteDraftSlug = settings.PublishedMarketingCatalog is { } published
                && !string.IsNullOrWhiteSpace(published.Slug)
                    ? SlugifyPublicBookingStore(published.Slug)
                    : fallbackSlug;
            settings.MarketingSiteDraftCustomDomain = settings.PublishedMarketingCatalog is { } publishedAddress
                ? NormalizeMarketingSiteCustomDomain(publishedAddress.CustomDomain)
                : NormalizeMarketingSiteCustomDomain(settings.PublicBookingCustomDomain);
            changed = true;
        }

        changed |= EnsureMarketingSiteBuilderState();

        if (changed)
        {
            _store.Save(_data);
        }
    }

    private bool EnsureMarketingSiteBuilderState()
    {
        var settings = _data.Settings;
        var changed = false;
        settings.MarketingSiteHeader ??= new MarketingCatalogHeader();
        settings.MarketingSiteFooter ??= new MarketingCatalogFooter();
        settings.MarketingSiteDesign ??= new MarketingCatalogDesign();
        settings.MarketingSiteSections ??= [];

        if (settings.MarketingSiteBuilderVersion < MarketingSiteBuilderVersion)
        {
            if (IsLegacyGeneratedMarketingSitePresetSet(settings.MarketingSiteSections, settings.BusinessSegment))
            {
                settings.MarketingSiteSections = settings.MarketingSiteSections
                    .Where(section => string.Equals(section.Type, "services", StringComparison.OrdinalIgnoreCase))
                    .Take(1)
                    .ToList();
            }
            settings.MarketingSiteBuilderVersion = MarketingSiteBuilderVersion;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.MarketingSiteHeader.BusinessName))
        {
            settings.MarketingSiteHeader.BusinessName = BusinessDisplayName();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteHeader.Subtitle))
        {
            settings.MarketingSiteHeader.Subtitle = FirstFilled(
                settings.BusinessSegment,
                "Atendimento com hora marcada");
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteHeader.ButtonText))
        {
            settings.MarketingSiteHeader.ButtonText = FirstFilled(
                settings.MarketingSiteButtonText,
                "Agendar agora");
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.BusinessName))
        {
            settings.MarketingSiteFooter.BusinessName = BusinessDisplayName();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.Description))
        {
            settings.MarketingSiteFooter.Description =
                $"Atendimento de {FirstFilled(settings.BusinessSegment, "serviços")} com agendamento online.";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.Address) &&
            !string.IsNullOrWhiteSpace(settings.BusinessAddress))
        {
            settings.MarketingSiteFooter.Address = settings.BusinessAddress;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.Phone) &&
            !string.IsNullOrWhiteSpace(settings.BusinessPhone))
        {
            settings.MarketingSiteFooter.Phone = settings.BusinessPhone;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.WhatsApp) &&
            !string.IsNullOrWhiteSpace(settings.WhatsAppStorePhone))
        {
            settings.MarketingSiteFooter.WhatsApp = settings.WhatsAppStorePhone;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.Instagram) &&
            !string.IsNullOrWhiteSpace(settings.InstagramUsername))
        {
            settings.MarketingSiteFooter.Instagram = settings.InstagramUsername.TrimStart('@');
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteFooter.Hours))
        {
            settings.MarketingSiteFooter.Hours = MarketingSiteHoursSummary();
            changed = true;
        }

        if (settings.MarketingSiteSections.Count == 0)
        {
            settings.MarketingSiteSections = DefaultMarketingSiteSections(settings.BusinessSegment);
            changed = true;
        }

        foreach (var section in settings.MarketingSiteSections)
        {
            if (string.IsNullOrWhiteSpace(section.Id))
            {
                section.Id = Guid.NewGuid().ToString("N");
                changed = true;
            }
            section.Items ??= [];
            foreach (var item in section.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    item.Id = Guid.NewGuid().ToString("N");
                    changed = true;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(settings.MarketingSiteSeoTitle))
        {
            settings.MarketingSiteSeoTitle =
                $"{BusinessDisplayName()} | {FirstFilled(settings.BusinessSegment, "Agendamento online")}";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.MarketingSiteSeoDescription))
        {
            settings.MarketingSiteSeoDescription =
                $"Conheça os serviços de {BusinessDisplayName()}, consulte horários e agende online.";
            changed = true;
        }
        return changed;
    }

    private string MarketingSiteHoursSummary()
    {
        var settings = _data.Settings;
        var workdays = settings.Workdays.OrderBy(day => day).ToList();
        var daySummary = workdays.Count switch
        {
            0 => "Consulte os horários",
            1 => MarketingSiteWeekdayLabel(workdays[0]),
            _ => $"{MarketingSiteWeekdayLabel(workdays[0])} a {MarketingSiteWeekdayLabel(workdays[^1])}"
        };
        return $"{daySummary} • {settings.WorkdayStartHour:00}h às {settings.WorkdayEndHour:00}h";
    }

    private static string MarketingSiteWeekdayLabel(int day) => day switch
    {
        0 => "Dom",
        1 => "Seg",
        2 => "Ter",
        3 => "Qua",
        4 => "Qui",
        5 => "Sex",
        6 => "Sáb",
        _ => "Dia"
    };

    private List<MarketingCatalogSection> DefaultMarketingSiteSections(string? segment)
    {
        return [CreateMarketingSiteSection("services", segment)];
    }

    private bool IsLegacyGeneratedMarketingSitePresetSet(
        IReadOnlyList<MarketingCatalogSection> sections,
        string? segment)
    {
        if (sections.Count <= 1)
        {
            return false;
        }

        var legacyPresets = LegacyDefaultMarketingSiteSections(segment);
        if (sections.Count != legacyPresets.Count)
        {
            return false;
        }

        return sections.Zip(legacyPresets).All(pair =>
            string.Equals(pair.First.Type, pair.Second.Type, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pair.First.Title, pair.Second.Title, StringComparison.Ordinal) &&
            string.Equals(pair.First.Subtitle, pair.Second.Subtitle, StringComparison.Ordinal) &&
            string.Equals(pair.First.Body, pair.Second.Body, StringComparison.Ordinal) &&
            string.Equals(pair.First.ButtonText, pair.Second.ButtonText, StringComparison.Ordinal) &&
            pair.First.AutomaticContent == pair.Second.AutomaticContent &&
            (pair.First.AutomaticContent ||
             (pair.First.Items.Count == pair.Second.Items.Count &&
              pair.First.Items.Zip(pair.Second.Items).All(itemPair =>
                  string.Equals(itemPair.First.Title, itemPair.Second.Title, StringComparison.Ordinal) &&
                  string.Equals(itemPair.First.Text, itemPair.Second.Text, StringComparison.Ordinal) &&
                  string.Equals(itemPair.First.Detail, itemPair.Second.Detail, StringComparison.Ordinal) &&
                  string.IsNullOrWhiteSpace(itemPair.First.ImagePath)))));
    }

    private List<MarketingCatalogSection> LegacyDefaultMarketingSiteSections(string? segment)
    {
        var preferred = MarketingSiteRecommendedSectionTypes(segment)
            .Where(type => type is not ("gallery" or "testimonials" or "before-after" or "brands"))
            .Take(6)
            .ToList();
        if (!preferred.Contains("services"))
        {
            preferred.Insert(0, "services");
        }
        if (!preferred.Contains("callout"))
        {
            preferred.Add("callout");
        }
        return preferred
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(type => CreateMarketingSiteSection(type, segment))
            .ToList();
    }

    private static IReadOnlyList<string> MarketingSiteRecommendedSectionTypes(string? segment)
    {
        var normalized = (segment ?? "").Trim().ToLowerInvariant();
        if (normalized.Contains("barbear"))
        {
            return ["services", "benefits", "gallery", "team", "testimonials", "faq", "location", "callout"];
        }
        if (normalized.Contains("salão") || normalized.Contains("cabelo") || normalized.Contains("unha"))
        {
            return ["services", "benefits", "gallery", "team", "before-after", "testimonials", "location", "callout"];
        }
        if (normalized.Contains("estética") || normalized.Contains("spa") || normalized.Contains("podolog"))
        {
            return ["services", "benefits", "process", "team", "before-after", "faq", "testimonials", "location", "callout"];
        }
        if (normalized.Contains("clínica") || normalized.Contains("medic"))
        {
            return ["services", "process", "team", "faq", "location", "callout"];
        }
        if (normalized.Contains("pet"))
        {
            return ["services", "benefits", "gallery", "team", "process", "faq", "testimonials", "location", "callout"];
        }
        if (normalized.Contains("oficina") || normalized.Contains("mecân"))
        {
            return ["services", "benefits", "process", "brands", "testimonials", "faq", "location", "callout"];
        }
        return ["services", "benefits", "team", "process", "faq", "location", "callout"];
    }

    private static IReadOnlyList<string> MarketingSiteAllSectionTypes() =>
    [
        "services",
        "benefits",
        "team",
        "gallery",
        "before-after",
        "process",
        "testimonials",
        "faq",
        "brands",
        "location",
        "callout"
    ];

    private static string MarketingSiteSectionTypeLabel(string type) => type switch
    {
        "services" => "Serviços e preços",
        "benefits" => "Diferenciais",
        "team" => "Equipe e profissionais",
        "gallery" => "Galeria de fotos",
        "before-after" => "Antes e depois",
        "process" => "Como funciona",
        "testimonials" => "Depoimentos",
        "faq" => "Perguntas frequentes",
        "brands" => "Marcas e especialidades",
        "location" => "Localização e contato",
        "callout" => "Chamada para agendamento",
        _ => "Seção personalizada"
    };

    private MarketingCatalogSection CreateMarketingSiteSection(string type, string? segment)
    {
        var segmentLabel = FirstFilled(segment ?? "", "seu atendimento");
        var section = new MarketingCatalogSection
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Enabled = true,
            Alignment = type is "callout" ? "center" : "left",
            Layout = type is "gallery" or "before-after" ? "gallery" :
                type is "process" ? "steps" :
                type is "location" ? "split" :
                "cards",
            Background = type is "benefits" or "faq" ? "soft" :
                type is "callout" ? "accent" :
                "light"
        };

        switch (type)
        {
            case "services":
                section.Title = "Escolha seu atendimento";
                section.Subtitle = "Serviços";
                section.Body = "Confira os serviços disponíveis, valores e duração antes de escolher o melhor horário.";
                section.AutomaticContent = true;
                break;
            case "benefits":
                section.Title = MarketingSiteBenefitsTitle(segment);
                section.Subtitle = "Por que escolher";
                section.Body = $"Detalhes que tornam a experiência em {segmentLabel} mais clara, segura e confortável.";
                section.Items = MarketingSiteBenefitItems(segment);
                break;
            case "team":
                section.Title = "Conheça nossa equipe";
                section.Subtitle = "Profissionais";
                section.Body = "Escolha quem combina com o atendimento que você procura.";
                section.AutomaticContent = true;
                break;
            case "gallery":
                section.Title = MarketingSiteGalleryTitle(segment);
                section.Subtitle = "Galeria";
                section.Body = "Adicione fotos reais do espaço, dos serviços ou dos resultados.";
                break;
            case "before-after":
                section.Title = "Resultados que contam histórias";
                section.Subtitle = "Antes e depois";
                section.Body = "Mostre resultados reais somente com autorização do cliente.";
                section.Layout = "comparison";
                break;
            case "process":
                section.Title = MarketingSiteProcessTitle(segment);
                section.Subtitle = "Como funciona";
                section.Body = "Um caminho simples desde a escolha do serviço até o atendimento.";
                section.Items = MarketingSiteProcessItems(segment);
                break;
            case "testimonials":
                section.Title = "Quem conhece, recomenda";
                section.Subtitle = "Depoimentos";
                section.Body = "Adicione avaliações verdadeiras de clientes.";
                break;
            case "faq":
                section.Title = "Dúvidas frequentes";
                section.Subtitle = "Perguntas e respostas";
                section.Body = "As informações mais importantes antes de agendar.";
                section.Items = MarketingSiteFaqItems(segment);
                break;
            case "brands":
                section.Title = "Marcas e especialidades";
                section.Subtitle = "Experiência";
                section.Body = "Informe apenas marcas, modelos ou especialidades realmente atendidos.";
                break;
            case "location":
                section.Title = "Venha nos visitar";
                section.Subtitle = "Localização e contato";
                section.Body = "Veja como chegar e fale com nossa equipe.";
                section.AutomaticContent = true;
                section.Items =
                [
                    new MarketingCatalogSectionItem
                    {
                        Title = "Endereço",
                        Text = FirstFilled(_data.Settings.BusinessAddress, "Adicione o endereço do estabelecimento")
                    },
                    new MarketingCatalogSectionItem
                    {
                        Title = "Horários",
                        Text = MarketingSiteHoursSummary()
                    },
                    new MarketingCatalogSectionItem
                    {
                        Title = "Contato",
                        Text = FirstFilled(
                            _data.Settings.BusinessPhone,
                            _data.Settings.WhatsAppStorePhone,
                            "Adicione um telefone")
                    }
                ];
                break;
            case "callout":
                section.Title = "Pronto para reservar seu horário?";
                section.Subtitle = "Agendamento online";
                section.Body = "Escolha o serviço e encontre o melhor horário em poucos passos.";
                section.ButtonText = "Agendar agora";
                section.ButtonTarget = "booking";
                break;
        }
        return section;
    }

    private static string MarketingSiteBenefitsTitle(string? segment)
    {
        var normalized = (segment ?? "").ToLowerInvariant();
        if (normalized.Contains("pet")) return "Cuidado em cada detalhe";
        if (normalized.Contains("oficina") || normalized.Contains("mecân")) return "Serviço claro, do diagnóstico à entrega";
        if (normalized.Contains("clínica") || normalized.Contains("medic")) return "Atendimento organizado e acolhedor";
        if (normalized.Contains("spa") || normalized.Contains("estética") || normalized.Contains("podolog")) return "Bem-estar com atenção de verdade";
        return "Uma experiência feita para você";
    }

    private static string MarketingSiteGalleryTitle(string? segment)
    {
        var normalized = (segment ?? "").ToLowerInvariant();
        if (normalized.Contains("pet")) return "Momentos de cuidado";
        if (normalized.Contains("oficina") || normalized.Contains("mecân")) return "Nossa estrutura e serviços";
        if (normalized.Contains("clínica") || normalized.Contains("medic")) return "Conheça nosso espaço";
        return "Conheça nosso trabalho";
    }

    private static string MarketingSiteProcessTitle(string? segment)
    {
        var normalized = (segment ?? "").ToLowerInvariant();
        if (normalized.Contains("oficina") || normalized.Contains("mecân")) return "Do diagnóstico à entrega";
        if (normalized.Contains("clínica") || normalized.Contains("medic")) return "Sua jornada de atendimento";
        if (normalized.Contains("pet")) return "Do agendamento ao cuidado";
        return "Agendar é simples";
    }

    private static List<MarketingCatalogSectionItem> MarketingSiteBenefitItems(string? segment)
    {
        var normalized = (segment ?? "").ToLowerInvariant();
        if (normalized.Contains("pet"))
        {
            return
            [
                MarketingSiteItem("Cuidado individual", "Atenção ao porte, à rotina e às necessidades de cada pet."),
                MarketingSiteItem("Horário reservado", "Menos espera e uma experiência mais tranquila."),
                MarketingSiteItem("Contato fácil", "Confirmações e orientações em um só lugar.")
            ];
        }
        if (normalized.Contains("oficina") || normalized.Contains("mecân"))
        {
            return
            [
                MarketingSiteItem("Orçamento claro", "Entenda o serviço antes de autorizar."),
                MarketingSiteItem("Horário combinado", "Organização para receber e entregar seu veículo."),
                MarketingSiteItem("Acompanhamento", "Contato simples durante todo o atendimento.")
            ];
        }
        if (normalized.Contains("clínica") || normalized.Contains("medic"))
        {
            return
            [
                MarketingSiteItem("Atendimento com hora marcada", "Mais previsibilidade para sua rotina."),
                MarketingSiteItem("Informações organizadas", "Orientações importantes reunidas antes da visita."),
                MarketingSiteItem("Contato facilitado", "Confirmações e dúvidas pelo canal informado.")
            ];
        }
        return
        [
            MarketingSiteItem("Atendimento personalizado", "Cada serviço é escolhido de acordo com o que você procura."),
            MarketingSiteItem("Horário reservado", "Organização para você aproveitar melhor seu tempo."),
            MarketingSiteItem("Agendamento simples", "Escolha o serviço e confirme em poucos passos.")
        ];
    }

    private static List<MarketingCatalogSectionItem> MarketingSiteProcessItems(string? segment)
    {
        var normalized = (segment ?? "").ToLowerInvariant();
        if (normalized.Contains("oficina") || normalized.Contains("mecân"))
        {
            return
            [
                MarketingSiteItem("1. Escolha o serviço", "Selecione o atendimento inicial que seu veículo precisa."),
                MarketingSiteItem("2. Reserve um horário", "Defina o melhor momento para levar o veículo."),
                MarketingSiteItem("3. Confirme o diagnóstico", "O serviço final só deve seguir após sua autorização.")
            ];
        }
        return
        [
            MarketingSiteItem("1. Escolha", "Veja os serviços e selecione o atendimento desejado."),
            MarketingSiteItem("2. Encontre um horário", "Consulte a agenda atualizada e escolha a melhor opção."),
            MarketingSiteItem("3. Confirme", "Preencha seus dados para enviar a solicitação.")
        ];
    }

    private static List<MarketingCatalogSectionItem> MarketingSiteFaqItems(string? segment)
    {
        var items = new List<MarketingCatalogSectionItem>
        {
            MarketingSiteItem(
                "Como faço para agendar?",
                "Escolha um serviço, selecione um dia e horário disponíveis e confirme seus dados."),
            MarketingSiteItem(
                "Posso remarcar ou cancelar?",
                "Entre em contato com o estabelecimento usando o telefone ou WhatsApp informado no site."),
            MarketingSiteItem(
                "O horário fica confirmado na hora?",
                "A confirmação aparece no acompanhamento do pedido conforme a política do estabelecimento.")
        };
        if ((segment ?? "").Contains("Clínica", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(MarketingSiteItem(
                "Este site substitui orientação profissional?",
                "Não. As informações do site são gerais; dúvidas sobre o atendimento devem ser confirmadas com a equipe."));
        }
        return items;
    }

    private static MarketingCatalogSectionItem MarketingSiteItem(string title, string text) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Text = text
        };

    private static MarketingCatalogHeader CloneMarketingCatalogHeader(MarketingCatalogHeader source) =>
        new()
        {
            BusinessName = source.BusinessName,
            Subtitle = source.Subtitle,
            ButtonText = source.ButtonText,
            ShowLogo = source.ShowLogo,
            ShowNavigation = source.ShowNavigation,
            ShowButton = source.ShowButton,
            Sticky = source.Sticky,
            Background = source.Background
        };

    private static MarketingCatalogFooter CloneMarketingCatalogFooter(MarketingCatalogFooter source) =>
        new()
        {
            BusinessName = source.BusinessName,
            Description = source.Description,
            Address = source.Address,
            Phone = source.Phone,
            Hours = source.Hours,
            Instagram = source.Instagram,
            WhatsApp = source.WhatsApp,
            ShowContact = source.ShowContact,
            ShowHours = source.ShowHours,
            ShowSocial = source.ShowSocial
        };

    private static MarketingCatalogDesign CloneMarketingCatalogDesign(MarketingCatalogDesign source) =>
        new()
        {
            ColorScheme = source.ColorScheme,
            ButtonStyle = source.ButtonStyle,
            CornerStyle = source.CornerStyle,
            ContentWidth = source.ContentWidth
        };

    private static List<MarketingCatalogSection> CloneMarketingCatalogSections(
        IEnumerable<MarketingCatalogSection> sections) =>
        sections.Select(CloneMarketingCatalogSection).ToList();

    private static MarketingSitePromotion CloneMarketingSitePromotion(MarketingSitePromotion source) =>
        new()
        {
            Name = source.Name,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            LimitPerCustomer = source.LimitPerCustomer,
            HighlightInCatalog = source.HighlightInCatalog,
            IsPublished = source.IsPublished,
            PublishedAt = source.PublishedAt,
            Items = source.Items.Select(item => new MarketingSitePromotionItem
            {
                ServiceId = item.ServiceId,
                ServiceName = item.ServiceName,
                OriginalPrice = item.OriginalPrice,
                PromotionalPrice = item.PromotionalPrice
            }).ToList()
        };

    private static MarketingCatalogSection CloneMarketingCatalogSection(MarketingCatalogSection source) =>
        new()
        {
            Id = source.Id,
            Type = source.Type,
            Title = source.Title,
            Subtitle = source.Subtitle,
            Body = source.Body,
            ButtonText = source.ButtonText,
            ButtonTarget = source.ButtonTarget,
            Layout = source.Layout,
            Background = source.Background,
            Alignment = source.Alignment,
            Enabled = source.Enabled,
            AutomaticContent = source.AutomaticContent,
            Items = source.Items.Select(item => new MarketingCatalogSectionItem
            {
                Id = item.Id,
                Title = item.Title,
                Text = item.Text,
                Detail = item.Detail,
                ImagePath = item.ImagePath
            }).ToList()
        };

    private void LoadMarketingSiteEditorSettings()
    {
        if (MarketingSiteTitleTextBox is null)
        {
            return;
        }

        _loadingMarketingSiteEditor = true;
        try
        {
            EnsureMarketingCatalogAddressState();
            var settings = _data.Settings;
            MarketingSiteTitleTextBox.Text = string.IsNullOrWhiteSpace(settings.MarketingSiteTitle)
                ? "Sua beleza, do seu jeito"
                : settings.MarketingSiteTitle;
            MarketingSiteSupportTextBox.Text = string.IsNullOrWhiteSpace(settings.MarketingSiteSupportText)
                ? "Realce sua essência com cuidados personalizados para você se sentir incrível todos os dias."
                : settings.MarketingSiteSupportText;
            MarketingSiteButtonTextBox.Text = string.IsNullOrWhiteSpace(settings.MarketingSiteButtonText)
                ? "Agendar agora"
                : settings.MarketingSiteButtonText;
            MarketingSiteStyleButtonTextBox.Text = MarketingSiteButtonTextBox.Text;
            MarketingSiteStyleShowButtonToggle.IsChecked = settings.MarketingSiteShowButton;
            MarketingSiteCustomAccentTextBox.Text = string.IsNullOrWhiteSpace(settings.MarketingSiteAccentColor)
                ? "#FF6B4A"
                : settings.MarketingSiteAccentColor.ToUpperInvariant();
            MarketingSiteSlugTextBox.Text = string.IsNullOrWhiteSpace(settings.MarketingSiteDraftSlug)
                ? SlugifyPublicBookingStore(BusinessDisplayName())
                : settings.MarketingSiteDraftSlug;
            MarketingSiteCustomDomainTextBox.Text = settings.MarketingSiteDraftCustomDomain;
            MarketingSiteShowButtonToggle.IsChecked = settings.MarketingSiteShowButton;
            MarketingSiteContrastSlider.Value = Math.Clamp(settings.MarketingSiteImageContrast, 0, 100);
            MarketingSiteSeoTitleTextBox.Text = settings.MarketingSiteSeoTitle;
            MarketingSiteSeoDescriptionTextBox.Text = settings.MarketingSiteSeoDescription;

            SelectComboItemByTag(MarketingSiteSpacingCombo, settings.MarketingSiteSpacing, "compact");
            SelectComboItemByTag(MarketingSiteFontCombo, settings.MarketingSiteTitleFont, "Georgia");
            SelectComboItemByTag(MarketingSiteColorSchemeCombo, settings.MarketingSiteDesign.ColorScheme, "warm");
            SelectComboItemByTag(MarketingSiteButtonStyleCombo, settings.MarketingSiteDesign.ButtonStyle, "rounded");
            SelectComboItemByTag(MarketingSiteCornerStyleCombo, settings.MarketingSiteDesign.CornerStyle, "rounded");
            SelectComboItemByTag(MarketingSiteContentWidthCombo, settings.MarketingSiteDesign.ContentWidth, "standard");
            SelectMarketingSiteAlignment(settings.MarketingSiteAlignment);
            ApplyMarketingSiteAccentColor(settings.MarketingSiteAccentColor);
            ApplyMarketingSiteHeroImage(settings.MarketingSiteHeroImagePath);
            ApplyMarketingSiteSpacing();
            ApplyMarketingSiteFont();
            ApplyMarketingSiteContrast();
            UpdateMarketingSiteAddressPreview();
            UpdateMarketingSiteCustomDomainStatus();
            MarketingSiteRecommendedSegmentText.Text =
                $"Recomendadas para {FirstFilled(settings.BusinessSegment, "seu segmento")}";
            _marketingSiteSelectedPart = "hero";
            _marketingSiteSelectedSectionId = "";
            _marketingSiteSelectedItemId = "";
            LoadMarketingSiteBuilderControls();
            RebuildMarketingSitePreviewSections();
            RebuildMarketingSiteStructureList();
            AnimateMarketingSiteElementIn(MarketingSiteBrowserFrame);
            _marketingSiteHasUnpublishedChanges = MarketingSiteDraftDiffersFromPublished();
            MarketingSiteSavedStatusText.Text = _marketingSiteHasUnpublishedChanges
                ? "Rascunho salvo • alterações não publicadas"
                : settings.MarketingSitePublishedAt is { } publishedAt
                    ? $"Publicado em {publishedAt:dd/MM, HH:mm}"
                : "Salvo automaticamente";
        }
        finally
        {
            _loadingMarketingSiteEditor = false;
        }
    }

    private static void SelectComboItemByTag(ComboBox combo, string? tag, string fallbackTag)
    {
        var desiredTag = string.IsNullOrWhiteSpace(tag) ? fallbackTag : tag;
        combo.SelectedItem = combo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, desiredTag, StringComparison.OrdinalIgnoreCase))
            ?? combo.Items.OfType<ComboBoxItem>().FirstOrDefault();
    }

    private void SelectMarketingSiteAlignment(string? alignment)
    {
        var desired = string.IsNullOrWhiteSpace(alignment) ? "left" : alignment;
        var button = FindVisualChildren<RadioButton>(MarketingSiteContentInspector)
            .FirstOrDefault(item => string.Equals(item.Tag as string, desired, StringComparison.OrdinalIgnoreCase));
        if (button is not null)
        {
            button.IsChecked = true;
        }
    }

    private void ScheduleMarketingSiteSave()
    {
        if (_loadingMarketingSiteEditor || MarketingSiteSavedStatusText is null)
        {
            return;
        }

        _marketingSiteHasUnpublishedChanges = true;
        MarketingSiteSavedStatusText.Text = "Salvando alterações...";
        _marketingSiteSaveTimer ??= CreateMarketingSiteSaveTimer();
        _marketingSiteSaveTimer.Stop();
        _marketingSiteSaveTimer.Start();
    }

    private DispatcherTimer CreateMarketingSiteSaveTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(550) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            SaveMarketingSiteSettings(markAsPublished: false);
        };
        return timer;
    }

    private void SaveMarketingSiteSettings(bool markAsPublished)
    {
        if (MarketingSiteTitleTextBox is null)
        {
            return;
        }

        PersistSelectedMarketingSitePartFromControls();
        var settings = _data.Settings;
        settings.MarketingSiteTitle = MarketingSiteTitleTextBox.Text.Trim();
        settings.MarketingSiteSupportText = MarketingSiteSupportTextBox.Text.Trim();
        settings.MarketingSiteButtonText = MarketingSiteButtonTextBox.Text.Trim();
        settings.MarketingSiteAccentColor = _marketingSiteAccentColor;
        settings.MarketingSiteAlignment = SelectedMarketingSiteAlignment();
        settings.MarketingSiteSpacing = SelectedTag(MarketingSiteSpacingCombo, "compact");
        settings.MarketingSiteTitleFont = SelectedTag(MarketingSiteFontCombo, "Georgia");
        settings.MarketingSiteImageContrast = MarketingSiteContrastSlider.Value;
        settings.MarketingSiteShowButton = MarketingSiteShowButtonToggle.IsChecked == true;
        settings.MarketingSiteDraftSlug = NormalizedMarketingSiteSlug();
        settings.MarketingSiteDraftCustomDomain = NormalizeMarketingSiteCustomDomain(
            MarketingSiteCustomDomainTextBox.Text);
        settings.MarketingSiteSeoTitle = MarketingSiteSeoTitleTextBox.Text.Trim();
        settings.MarketingSiteSeoDescription = MarketingSiteSeoDescriptionTextBox.Text.Trim();
        settings.MarketingSiteDesign.ColorScheme = SelectedTag(MarketingSiteColorSchemeCombo, "warm");
        settings.MarketingSiteDesign.ButtonStyle = SelectedTag(MarketingSiteButtonStyleCombo, "rounded");
        settings.MarketingSiteDesign.CornerStyle = SelectedTag(MarketingSiteCornerStyleCombo, "rounded");
        settings.MarketingSiteDesign.ContentWidth = SelectedTag(MarketingSiteContentWidthCombo, "standard");
        if (markAsPublished)
        {
            RefreshAutomaticMarketingSiteSectionContent();
            var publishedAt = DateTime.Now;
            settings.MarketingSitePublishedAt = publishedAt;
            settings.PublishedMarketingCatalog = new MarketingCatalogPublication
            {
                AddressSnapshotVersion = 1,
                Slug = settings.MarketingSiteDraftSlug,
                CustomDomain = settings.MarketingSiteDraftCustomDomain,
                Title = settings.MarketingSiteTitle,
                SupportText = settings.MarketingSiteSupportText,
                ButtonText = settings.MarketingSiteButtonText,
                HeroImagePath = settings.MarketingSiteHeroImagePath,
                AccentColor = settings.MarketingSiteAccentColor,
                Alignment = settings.MarketingSiteAlignment,
                Spacing = settings.MarketingSiteSpacing,
                TitleFont = settings.MarketingSiteTitleFont,
                ImageContrast = settings.MarketingSiteImageContrast,
                ShowButton = settings.MarketingSiteShowButton,
                Header = CloneMarketingCatalogHeader(settings.MarketingSiteHeader),
                Footer = CloneMarketingCatalogFooter(settings.MarketingSiteFooter),
                Design = CloneMarketingCatalogDesign(settings.MarketingSiteDesign),
                Sections = CloneMarketingCatalogSections(settings.MarketingSiteSections),
                SeoTitle = settings.MarketingSiteSeoTitle,
                SeoDescription = settings.MarketingSiteSeoDescription,
                Promotion = settings.MarketingSitePromotion.IsPublished
                    ? CloneMarketingSitePromotion(settings.MarketingSitePromotion)
                    : null,
                PublishedAt = publishedAt
            };
            _marketingSiteHasUnpublishedChanges = false;
        }

        _store.Save(_data);
        MarketingSiteSavedStatusText.Text = markAsPublished
            ? "Enviando publicação..."
            : _marketingSiteHasUnpublishedChanges
                ? "Rascunho salvo • alterações não publicadas"
                : "Salvo automaticamente";
    }

    private bool MarketingSiteDraftDiffersFromPublished()
    {
        var settings = _data.Settings;
        var publication = settings.PublishedMarketingCatalog;
        if (publication is null)
        {
            return false;
        }

        var draft = new object?[]
        {
            settings.MarketingSiteDraftSlug,
            settings.MarketingSiteDraftCustomDomain,
            settings.MarketingSiteTitle,
            settings.MarketingSiteSupportText,
            settings.MarketingSiteButtonText,
            settings.MarketingSiteHeroImagePath,
            settings.MarketingSiteAccentColor,
            settings.MarketingSiteAlignment,
            settings.MarketingSiteSpacing,
            settings.MarketingSiteTitleFont,
            settings.MarketingSiteImageContrast,
            settings.MarketingSiteShowButton,
            settings.MarketingSiteHeader,
            settings.MarketingSiteFooter,
            settings.MarketingSiteDesign,
            settings.MarketingSiteSections,
            settings.MarketingSiteSeoTitle,
            settings.MarketingSiteSeoDescription
        };
        var published = new object?[]
        {
            publication.Slug,
            publication.CustomDomain,
            publication.Title,
            publication.SupportText,
            publication.ButtonText,
            publication.HeroImagePath,
            publication.AccentColor,
            publication.Alignment,
            publication.Spacing,
            publication.TitleFont,
            publication.ImageContrast,
            publication.ShowButton,
            publication.Header,
            publication.Footer,
            publication.Design,
            publication.Sections,
            publication.SeoTitle,
            publication.SeoDescription
        };
        return !string.Equals(
            System.Text.Json.JsonSerializer.Serialize(draft),
            System.Text.Json.JsonSerializer.Serialize(published),
            StringComparison.Ordinal);
    }

    private MarketingCatalogPublication? MarketingCatalogPublicationForSync()
    {
        EnsureMarketingCatalogAddressState();
        if (_data.Settings.PublishedMarketingCatalog is { PublishedAt: not null } publication)
        {
            return publication;
        }
        return null;
    }

    private string NormalizedMarketingSiteSlug()
    {
        var source = string.IsNullOrWhiteSpace(MarketingSiteSlugTextBox?.Text)
            ? BusinessDisplayName()
            : MarketingSiteSlugTextBox.Text;
        return SlugifyPublicBookingStore(source);
    }

    private static string NormalizeMarketingSiteCustomDomain(string? value)
    {
        var domain = (value ?? "").Trim().ToLowerInvariant();
        if (domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            domain = domain[8..];
        }
        else if (domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            domain = domain[7..];
        }

        var separatorIndex = domain.IndexOfAny(['/', ':']);
        if (separatorIndex >= 0)
        {
            domain = domain[..separatorIndex];
        }
        return domain.TrimEnd('.');
    }

    private static bool IsValidMarketingSiteCustomDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        if (value.Length > 253 || !value.Contains('.') ||
            value.Equals("minhaagendalivre.com.br", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".minhaagendalivre.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return value.Split('.').All(label =>
            label.Length is >= 1 and <= 63 &&
            char.IsLetterOrDigit(label[0]) &&
            char.IsLetterOrDigit(label[^1]) &&
            label.All(character => char.IsLetterOrDigit(character) || character == '-'));
    }

    private bool TryValidateMarketingSiteAddressForPublish()
    {
        var customDomain = NormalizeMarketingSiteCustomDomain(MarketingSiteCustomDomainTextBox?.Text);
        if (!string.IsNullOrWhiteSpace(customDomain) && !IsValidMarketingSiteCustomDomain(customDomain))
        {
            MessageBox.Show(
                this,
                "Informe um domínio válido, como www.seusalao.com.br, ou deixe o campo vazio.",
                "Publicar catálogo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            MarketingSiteCustomDomainTextBox?.Focus();
            MarketingSiteCustomDomainTextBox?.SelectAll();
            return false;
        }

        if (MarketingSiteCustomDomainTextBox is not null)
        {
            MarketingSiteCustomDomainTextBox.Text = customDomain;
        }
        if (MarketingSiteSlugTextBox is not null)
        {
            MarketingSiteSlugTextBox.Text = NormalizedMarketingSiteSlug();
        }
        return true;
    }

    private void MarketingSiteAddressField_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMarketingSiteAddressPreview();
        UpdateMarketingSiteCustomDomainStatus();
        ScheduleMarketingSiteSave();
    }

    private void UpdateMarketingSiteAddressPreview()
    {
        if (MarketingSiteCatalogAddressText is null || MarketingSitePreviewUrlText is null)
        {
            return;
        }
        var platformHost = $"{NormalizedMarketingSiteSlug()}.minhaagendalivre.com.br";
        MarketingSiteCatalogAddressText.Text = platformHost;
        var customDomain = NormalizeMarketingSiteCustomDomain(MarketingSiteCustomDomainTextBox?.Text);
        MarketingSitePreviewUrlText.Text = IsValidMarketingSiteCustomDomain(customDomain) && !string.IsNullOrWhiteSpace(customDomain)
            ? customDomain
            : platformHost;
    }

    private void UpdateMarketingSiteCustomDomainStatus()
    {
        if (MarketingSiteCustomDomainStatusText is null)
        {
            return;
        }
        var settings = _data.Settings;
        var draftDomain = NormalizeMarketingSiteCustomDomain(
            MarketingSiteCustomDomainTextBox?.Text ?? settings.MarketingSiteDraftCustomDomain);
        var publishedDomain = NormalizeMarketingSiteCustomDomain(
            settings.PublishedMarketingCatalog?.CustomDomain);
        if (!string.Equals(draftDomain, publishedDomain, StringComparison.OrdinalIgnoreCase))
        {
            MarketingSiteCustomDomainStatusText.Text = "Domínio salvo no rascunho. Clique em Publicar para aplicar no Cloudflare.";
            MarketingSiteCustomDomainStatusText.Foreground = MutedBrush;
            return;
        }
        if (string.IsNullOrWhiteSpace(publishedDomain))
        {
            MarketingSiteCustomDomainStatusText.Text = "Opcional. Publique para receber os registros de DNS.";
            MarketingSiteCustomDomainStatusText.Foreground = MutedBrush;
            return;
        }
        if (!string.Equals(
                NormalizeMarketingSiteCustomDomain(settings.PublicBookingCustomDomain),
                publishedDomain,
                StringComparison.OrdinalIgnoreCase))
        {
            MarketingSiteCustomDomainStatusText.Text = "Publicado. Aguardando a sincronização segura com o Cloudflare.";
            MarketingSiteCustomDomainStatusText.Foreground = MutedBrush;
            return;
        }
        var domain = publishedDomain;
        if (string.Equals(settings.PublicBookingCustomDomainStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            MarketingSiteCustomDomainStatusText.Text = $"Ativo em https://{domain}";
            MarketingSiteCustomDomainStatusText.Foreground = Solid("#15803D");
            return;
        }
        if (!string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainLastError))
        {
            MarketingSiteCustomDomainStatusText.Text = settings.PublicBookingCustomDomainLastError;
            MarketingSiteCustomDomainStatusText.Foreground = Solid("#B91C1C");
            return;
        }
        if (string.Equals(settings.PublicBookingCustomDomainStatus, "failed", StringComparison.OrdinalIgnoreCase))
        {
            MarketingSiteCustomDomainStatusText.Text = "Não foi possível conectar este domínio.";
            MarketingSiteCustomDomainStatusText.Foreground = Solid("#B91C1C");
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainCnameTarget))
        {
            MarketingSiteCustomDomainStatusText.Text = "Domínio salvo no rascunho. Clique em Publicar para iniciar a conexão no Cloudflare.";
            MarketingSiteCustomDomainStatusText.Foreground = MutedBrush;
            return;
        }
        var target = settings.PublicBookingCustomDomainCnameTarget;
        var validationRecordType = string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainValidationRecordType)
            ? "TXT"
            : settings.PublicBookingCustomDomainValidationRecordType;
        var validationRecord = string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainValidationRecordName) ||
            string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainValidationRecordValue)
            ? ""
            : $" Depois, crie {validationRecordType} " +
              $"{settings.PublicBookingCustomDomainValidationRecordName} com o valor {settings.PublicBookingCustomDomainValidationRecordValue}.";
        MarketingSiteCustomDomainStatusText.Text = $"Aguardando DNS: crie um CNAME para {target}.{validationRecord}";
        MarketingSiteCustomDomainStatusText.Foreground = MutedBrush;
    }

    private void MarketingSiteConnectDomainButton_Click(object sender, RoutedEventArgs e)
    {
        var domain = NormalizeMarketingSiteCustomDomain(MarketingSiteCustomDomainTextBox.Text);
        if (!IsValidMarketingSiteCustomDomain(domain))
        {
            MessageBox.Show(
                this,
                "Informe um domínio válido, como www.seusalao.com.br.",
                "Domínio personalizado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        MarketingSiteCustomDomainTextBox.Text = domain;
        SaveMarketingSiteSettings(markAsPublished: false);
        UpdateMarketingSiteCustomDomainStatus();
        ShowStatus(string.IsNullOrWhiteSpace(domain)
            ? "Domínio personalizado removido."
            : "Domínio salvo no rascunho. Clique em Publicar para iniciar a conexão.");
    }

    private string SelectedMarketingSiteAlignment() =>
        FindVisualChildren<RadioButton>(MarketingSiteContentInspector)
            .FirstOrDefault(item => item.IsChecked == true && item.Tag is string)?.Tag as string ?? "left";

    private static string SelectedTag(ComboBox combo, string fallback) =>
        (combo.SelectedItem as ComboBoxItem)?.Tag as string ?? fallback;

    private MarketingCatalogSection? SelectedMarketingSiteSection() =>
        _data.Settings.MarketingSiteSections.FirstOrDefault(section =>
            string.Equals(section.Id, _marketingSiteSelectedSectionId, StringComparison.Ordinal));

    private MarketingCatalogSectionItem? SelectedMarketingSiteSectionItem()
    {
        var section = SelectedMarketingSiteSection();
        return section?.Items.FirstOrDefault(item =>
            string.Equals(item.Id, _marketingSiteSelectedItemId, StringComparison.Ordinal));
    }

    private void LoadMarketingSiteBuilderControls()
    {
        if (MarketingSiteHeaderInspectorPanel is null)
        {
            return;
        }

        _updatingMarketingSiteBuilderControls = true;
        try
        {
            var settings = _data.Settings;
            var isHeader = _marketingSiteSelectedPart == "header";
            var isHero = _marketingSiteSelectedPart == "hero";
            var isSection = _marketingSiteSelectedPart == "section";
            var isFooter = _marketingSiteSelectedPart == "footer";
            var selectedSection = isSection ? SelectedMarketingSiteSection() : null;
            MarketingSiteInspectorPartTitleText.Text = isHeader
                ? "Cabeçalho"
                : isHero
                    ? "Capa principal"
                    : isFooter
                        ? "Rodapé"
                        : FirstFilled(selectedSection?.Title ?? "", MarketingSiteSectionTypeLabel(selectedSection?.Type ?? ""));
            MarketingSiteHeaderInspectorPanel.Visibility = isHeader ? Visibility.Visible : Visibility.Collapsed;
            MarketingSiteHeroInspectorPanel.Visibility = isHero ? Visibility.Visible : Visibility.Collapsed;
            MarketingSiteSectionInspectorPanel.Visibility = isSection ? Visibility.Visible : Visibility.Collapsed;
            MarketingSiteFooterInspectorPanel.Visibility = isFooter ? Visibility.Visible : Visibility.Collapsed;
            MarketingSiteSelectedSectionStylePanel.Visibility = isSection
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (isHeader)
            {
                var header = settings.MarketingSiteHeader;
                MarketingSiteHeaderBusinessNameTextBox.Text = header.BusinessName;
                MarketingSiteHeaderSubtitleTextBox.Text = header.Subtitle;
                MarketingSiteHeaderButtonTextBox.Text = header.ButtonText;
                MarketingSiteHeaderShowLogoToggle.IsChecked = header.ShowLogo;
                MarketingSiteHeaderShowNavigationToggle.IsChecked = header.ShowNavigation;
                MarketingSiteHeaderShowButtonToggle.IsChecked = header.ShowButton;
                MarketingSiteHeaderStickyToggle.IsChecked = header.Sticky;
                SelectComboItemByTag(MarketingSiteHeaderBackgroundCombo, header.Background, "solid");
            }
            else if (isSection)
            {
                var section = SelectedMarketingSiteSection();
                if (section is null)
                {
                    _marketingSiteSelectedPart = "hero";
                    _marketingSiteSelectedSectionId = "";
                    LoadMarketingSiteBuilderControls();
                    return;
                }

                MarketingSiteSectionTypeText.Text = MarketingSiteSectionTypeLabel(section.Type);
                MarketingSiteSectionEnabledToggle.IsChecked = section.Enabled;
                MarketingSiteSectionTitleTextBox.Text = section.Title;
                MarketingSiteSectionSubtitleTextBox.Text = section.Subtitle;
                MarketingSiteSectionBodyTextBox.Text = section.Body;
                MarketingSiteSectionButtonTextBox.Text = section.ButtonText;
                MarketingSiteSectionAutomaticNotice.Visibility = section.AutomaticContent
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                MarketingSiteSectionItemsEditor.Visibility = section.AutomaticContent
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                SelectComboItemByTag(MarketingSiteSectionLayoutCombo, section.Layout, "cards");
                SelectComboItemByTag(MarketingSiteSectionBackgroundCombo, section.Background, "light");
                SelectComboItemByTag(MarketingSiteSectionAlignmentCombo, section.Alignment, "left");
                RebuildMarketingSiteSectionItemCombo(section);
            }
            else if (isFooter)
            {
                var footer = settings.MarketingSiteFooter;
                MarketingSiteFooterBusinessNameTextBox.Text = footer.BusinessName;
                MarketingSiteFooterDescriptionTextBox.Text = footer.Description;
                MarketingSiteFooterAddressTextBox.Text = footer.Address;
                MarketingSiteFooterPhoneTextBox.Text = footer.Phone;
                MarketingSiteFooterHoursTextBox.Text = footer.Hours;
                MarketingSiteFooterInstagramTextBox.Text = footer.Instagram;
                MarketingSiteFooterWhatsAppTextBox.Text = footer.WhatsApp;
                MarketingSiteFooterShowContactToggle.IsChecked = footer.ShowContact;
                MarketingSiteFooterShowHoursToggle.IsChecked = footer.ShowHours;
                MarketingSiteFooterShowSocialToggle.IsChecked = footer.ShowSocial;
            }

            UpdateMarketingSiteSelectionBorders();
        }
        finally
        {
            _updatingMarketingSiteBuilderControls = false;
        }
    }

    private void RebuildMarketingSiteSectionItemCombo(MarketingCatalogSection section)
    {
        MarketingSiteSectionItemCombo.Items.Clear();
        foreach (var item in section.Items)
        {
            MarketingSiteSectionItemCombo.Items.Add(new ComboBoxItem
            {
                Tag = item.Id,
                Content = string.IsNullOrWhiteSpace(item.Title) ? "Item sem título" : item.Title
            });
        }

        if (section.Items.Count == 0)
        {
            _marketingSiteSelectedItemId = "";
            MarketingSiteSectionItemEditorPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (!section.Items.Any(item => item.Id == _marketingSiteSelectedItemId))
        {
            _marketingSiteSelectedItemId = section.Items[0].Id;
        }
        MarketingSiteSectionItemCombo.SelectedItem = MarketingSiteSectionItemCombo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(
                item.Tag as string,
                _marketingSiteSelectedItemId,
                StringComparison.Ordinal));
        LoadMarketingSiteSectionItemControls();
    }

    private void LoadMarketingSiteSectionItemControls()
    {
        var item = SelectedMarketingSiteSectionItem();
        MarketingSiteSectionItemEditorPanel.Visibility = item is null
            ? Visibility.Collapsed
            : Visibility.Visible;
        if (item is null)
        {
            return;
        }

        _updatingMarketingSiteBuilderControls = true;
        try
        {
            MarketingSiteSectionItemTitleTextBox.Text = item.Title;
            MarketingSiteSectionItemTextTextBox.Text = item.Text;
            MarketingSiteSectionItemDetailTextBox.Text = item.Detail;
            MarketingSiteSectionItemThumbnail.Source = string.IsNullOrWhiteSpace(item.ImagePath)
                ? null
                : LoadMarketingSiteBitmap(item.ImagePath);
        }
        finally
        {
            _updatingMarketingSiteBuilderControls = false;
        }
    }

    private void PersistSelectedMarketingSitePartFromControls()
    {
        if (_updatingMarketingSiteBuilderControls || MarketingSiteHeaderInspectorPanel is null)
        {
            return;
        }

        var settings = _data.Settings;
        if (_marketingSiteSelectedPart == "header")
        {
            var header = settings.MarketingSiteHeader;
            header.BusinessName = MarketingSiteHeaderBusinessNameTextBox.Text.Trim();
            header.Subtitle = MarketingSiteHeaderSubtitleTextBox.Text.Trim();
            header.ButtonText = MarketingSiteHeaderButtonTextBox.Text.Trim();
            header.ShowLogo = MarketingSiteHeaderShowLogoToggle.IsChecked == true;
            header.ShowNavigation = MarketingSiteHeaderShowNavigationToggle.IsChecked == true;
            header.ShowButton = MarketingSiteHeaderShowButtonToggle.IsChecked == true;
            header.Sticky = MarketingSiteHeaderStickyToggle.IsChecked == true;
            header.Background = SelectedTag(MarketingSiteHeaderBackgroundCombo, "solid");
        }
        else if (_marketingSiteSelectedPart == "section" && SelectedMarketingSiteSection() is { } section)
        {
            section.Title = MarketingSiteSectionTitleTextBox.Text.Trim();
            section.Subtitle = MarketingSiteSectionSubtitleTextBox.Text.Trim();
            section.Body = MarketingSiteSectionBodyTextBox.Text.Trim();
            section.ButtonText = MarketingSiteSectionButtonTextBox.Text.Trim();
            section.Enabled = MarketingSiteSectionEnabledToggle.IsChecked == true;
            section.Layout = SelectedTag(MarketingSiteSectionLayoutCombo, "cards");
            section.Background = SelectedTag(MarketingSiteSectionBackgroundCombo, "light");
            section.Alignment = SelectedTag(MarketingSiteSectionAlignmentCombo, "left");
            if (SelectedMarketingSiteSectionItem() is { } item)
            {
                item.Title = MarketingSiteSectionItemTitleTextBox.Text.Trim();
                item.Text = MarketingSiteSectionItemTextTextBox.Text.Trim();
                item.Detail = MarketingSiteSectionItemDetailTextBox.Text.Trim();
            }
        }
        else if (_marketingSiteSelectedPart == "footer")
        {
            var footer = settings.MarketingSiteFooter;
            footer.BusinessName = MarketingSiteFooterBusinessNameTextBox.Text.Trim();
            footer.Description = MarketingSiteFooterDescriptionTextBox.Text.Trim();
            footer.Address = MarketingSiteFooterAddressTextBox.Text.Trim();
            footer.Phone = MarketingSiteFooterPhoneTextBox.Text.Trim();
            footer.Hours = MarketingSiteFooterHoursTextBox.Text.Trim();
            footer.Instagram = MarketingSiteFooterInstagramTextBox.Text.Trim().TrimStart('@');
            footer.WhatsApp = MarketingSiteFooterWhatsAppTextBox.Text.Trim();
            footer.ShowContact = MarketingSiteFooterShowContactToggle.IsChecked == true;
            footer.ShowHours = MarketingSiteFooterShowHoursToggle.IsChecked == true;
            footer.ShowSocial = MarketingSiteFooterShowSocialToggle.IsChecked == true;
        }

        settings.MarketingSiteSeoTitle = MarketingSiteSeoTitleTextBox.Text.Trim();
        settings.MarketingSiteSeoDescription = MarketingSiteSeoDescriptionTextBox.Text.Trim();
        settings.MarketingSiteDesign.ColorScheme = SelectedTag(MarketingSiteColorSchemeCombo, "warm");
        settings.MarketingSiteDesign.ButtonStyle = SelectedTag(MarketingSiteButtonStyleCombo, "rounded");
        settings.MarketingSiteDesign.CornerStyle = SelectedTag(MarketingSiteCornerStyleCombo, "rounded");
        settings.MarketingSiteDesign.ContentWidth = SelectedTag(MarketingSiteContentWidthCombo, "standard");
    }

    private void MarketingSiteBuilderField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingMarketingSiteEditor || _updatingMarketingSiteBuilderControls)
        {
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        UpdateMarketingSiteBuilderPreview();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteBuilderToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingMarketingSiteEditor || _updatingMarketingSiteBuilderControls)
        {
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        UpdateMarketingSiteBuilderPreview();
        RebuildMarketingSiteStructureList();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteBuilderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingMarketingSiteEditor || _updatingMarketingSiteBuilderControls)
        {
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        UpdateMarketingSiteBuilderPreview();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSitePreviewPart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string part })
        {
            SelectMarketingSitePart(part);
            e.Handled = true;
        }
    }

    private void MarketingSiteSelectPartButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string part })
        {
            SelectMarketingSitePart(part);
        }
    }

    private void SelectMarketingSitePart(string part, string sectionId = "")
    {
        PersistSelectedMarketingSitePartFromControls();
        _marketingSiteSelectedPart = part;
        _marketingSiteSelectedSectionId = part == "section" ? sectionId : "";
        _marketingSiteSelectedItemId = "";
        MarketingSiteContentTab.IsChecked = true;
        LoadMarketingSiteBuilderControls();
        SetMarketingSiteInspectorVisible(true);
    }

    private void UpdateMarketingSiteSelectionBorders()
    {
        if (MarketingSitePreviewHeaderSelectionBorder is null)
        {
            return;
        }
        var selectedBrush = AccentBrush;
        MarketingSitePreviewHeaderSelectionBorder.BorderBrush =
            _marketingSiteSelectedPart == "header" ? selectedBrush : LineBrush;
        MarketingSitePreviewHeaderSelectionBorder.BorderThickness =
            _marketingSiteSelectedPart == "header" ? new Thickness(1.5) : new Thickness(0, 0, 0, 1);
        MarketingSiteHeroSelectionBorder.BorderBrush =
            _marketingSiteSelectedPart == "hero" ? selectedBrush : LineBrush;
        MarketingSiteHeroSelectionBorder.BorderThickness =
            _marketingSiteSelectedPart == "hero" ? new Thickness(1.5) : new Thickness(1);
        MarketingSitePreviewFooterSelectionBorder.BorderBrush =
            _marketingSiteSelectedPart == "footer" ? selectedBrush : LineBrush;
        MarketingSitePreviewFooterSelectionBorder.BorderThickness =
            _marketingSiteSelectedPart == "footer" ? new Thickness(1.5) : new Thickness(1);
    }

    private void UpdateMarketingSiteBuilderPreview()
    {
        if (MarketingSitePreviewBusinessNameText is null)
        {
            return;
        }

        var settings = _data.Settings;
        var header = settings.MarketingSiteHeader;
        var globalDark = string.Equals(
            settings.MarketingSiteDesign.ColorScheme,
            "dark",
            StringComparison.OrdinalIgnoreCase);
        var globalPaper = globalDark ? Solid("#2B2522") : Solid("#FFFDFC");
        var globalInk = globalDark ? Solid("#FFF9F5") : InkBrush;
        var globalMuted = globalDark ? Solid("#D7CAC2") : MutedBrush;
        MarketingSitePreviewBusinessNameText.Text = FirstFilled(header.BusinessName, BusinessDisplayName());
        MarketingSiteHeaderButtonText.Text = FirstFilled(header.ButtonText, "Agendar agora");
        MarketingSiteHeaderButtonBorder.Visibility = header.ShowButton
            ? Visibility.Visible
            : Visibility.Collapsed;
        MarketingSitePreviewNavigation.Visibility = header.ShowNavigation
            ? Visibility.Visible
            : Visibility.Collapsed;
        MarketingSitePreviewHeaderSelectionBorder.Background = globalDark
            ? Solid("#241F1C")
            : header.Background switch
            {
                "transparent" => Solid("#F8F4F0"),
                "soft" => Solid("#FFF5F0"),
                _ => Solid("#FFFDFB")
            };
        MarketingSitePreviewBusinessNameText.Foreground = globalInk;
        foreach (var navigationText in FindVisualChildren<TextBlock>(MarketingSitePreviewNavigation))
        {
            navigationText.Foreground = globalInk;
        }
        MarketingSitePreviewSectionsScroll.Background = globalPaper;
        var buttonCorner = settings.MarketingSiteDesign.ButtonStyle switch
        {
            "pill" => new CornerRadius(18),
            "square" => new CornerRadius(2),
            _ => new CornerRadius(8)
        };
        MarketingSiteHeaderButtonBorder.CornerRadius = buttonCorner;
        MarketingSitePreviewButtonBorder.CornerRadius = buttonCorner;

        var footer = settings.MarketingSiteFooter;
        MarketingSitePreviewFooterBusinessNameText.Text =
            FirstFilled(footer.BusinessName, BusinessDisplayName());
        MarketingSitePreviewFooterDescriptionText.Text = footer.Description;
        var footerParts = new List<string>();
        if (footer.ShowContact)
        {
            footerParts.Add(FirstFilled(footer.Phone, footer.WhatsApp));
        }
        if (footer.ShowHours)
        {
            footerParts.Add(footer.Hours);
        }
        MarketingSitePreviewFooterContactText.Text = string.Join(
            " • ",
            footerParts.Where(part => !string.IsNullOrWhiteSpace(part)));
        MarketingSitePreviewFooterSelectionBorder.Background =
            globalDark ? Solid("#241F1C") : Solid("#F7F3F0");
        MarketingSitePreviewFooterSelectionBorder.CornerRadius = MarketingSiteGlobalCornerRadius();
        MarketingSitePreviewFooterBusinessNameText.Foreground = globalInk;
        MarketingSitePreviewFooterDescriptionText.Foreground = globalMuted;
        MarketingSitePreviewFooterContactText.Foreground = globalMuted;

        UpdateMarketingSitePreview();
        RebuildMarketingSitePreviewSections();
        UpdateMarketingSiteSelectionBorders();
    }

    private void RebuildMarketingSitePreviewSections()
    {
        if (MarketingSiteDynamicSectionsPanel is null)
        {
            return;
        }

        MarketingSiteDynamicSectionsPanel.Children.Clear();
        foreach (var section in _data.Settings.MarketingSiteSections.Where(section => section.Enabled))
        {
            MarketingSiteDynamicSectionsPanel.Children.Add(BuildMarketingSitePreviewSection(section));
        }
    }

    private Border BuildMarketingSitePreviewSection(MarketingCatalogSection section)
    {
        if (section.Type == "services")
        {
            return BuildMarketingSiteServicesPreviewSection(section);
        }

        var globalDark = string.Equals(
            _data.Settings.MarketingSiteDesign.ColorScheme,
            "dark",
            StringComparison.OrdinalIgnoreCase);
        var background = section.Background switch
        {
            "soft" => globalDark ? Solid("#352E2A") : Solid("#F7F2EE"),
            "accent" => globalDark ? Solid("#4B2E25") : Solid("#FFF0E8"),
            "dark" => Solid("#241F1C"),
            _ => globalDark ? Solid("#2B2522") : Solid("#FFFDFC")
        };
        var darkSurface = globalDark || section.Background == "dark";
        var foreground = darkSurface ? Solid("#FFF9F5") : InkBrush;
        var muted = darkSurface ? Solid("#D7CAC2") : MutedBrush;
        var cornerRadius = MarketingSiteGlobalCornerRadius();
        var sectionPadding = MarketingSiteGlobalSectionPadding();
        var border = new Border
        {
            Tag = section.Id,
            Margin = new Thickness(0, 0, 0, MarketingSiteGlobalSectionGap()),
            Padding = sectionPadding,
            CornerRadius = cornerRadius,
            BorderBrush = _marketingSiteSelectedPart == "section" &&
                          _marketingSiteSelectedSectionId == section.Id
                ? AccentBrush
                : LineBrush,
            BorderThickness = _marketingSiteSelectedPart == "section" &&
                              _marketingSiteSelectedSectionId == section.Id
                ? new Thickness(1)
                : new Thickness(1),
            Background = background,
            Cursor = Cursors.Hand
        };
        border.MouseLeftButtonDown += MarketingSitePreviewSection_MouseLeftButtonDown;

        var root = new StackPanel();
        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headingCopy = new StackPanel();
        headingCopy.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(section.Subtitle)
                ? MarketingSiteSectionTypeLabel(section.Type)
                : section.Subtitle,
            Foreground = AccentBrush,
            FontSize = 7.5,
            FontWeight = FontWeights.SemiBold
        });
        headingCopy.Children.Add(new TextBlock
        {
            Text = FirstFilled(section.Title, MarketingSiteSectionTypeLabel(section.Type)),
            Foreground = foreground,
            FontFamily = new FontFamily(SelectedTag(MarketingSiteFontCombo, "Georgia")),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        heading.Children.Add(headingCopy);
        var typeText = new TextBlock
        {
            Text = MarketingSiteSectionTypeLabel(section.Type),
            Foreground = muted,
            FontSize = 7.5,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 2, 0, 0)
        };
        Grid.SetColumn(typeText, 1);
        heading.Children.Add(typeText);
        root.Children.Add(heading);
        if (!string.IsNullOrWhiteSpace(section.Body))
        {
            root.Children.Add(new TextBlock
            {
                Text = section.Body,
                Foreground = muted,
                FontSize = 8.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        var previewItems = MarketingSitePreviewItems(section)
            .Take(section.Type == "services" ? 12 : 6)
            .ToList();
        if (previewItems.Count > 0)
        {
            var itemsPanel = new UniformGrid
            {
                Columns = Math.Min(3, previewItems.Count),
                Margin = new Thickness(0, 8, 0, 0)
            };
            foreach (var item in previewItems)
            {
                var itemBorder = new Border
                {
                    Background = darkSurface ? Solid("#3A322E") : Solid("#FFFCFA"),
                    BorderBrush = darkSurface ? Solid("#554A44") : LineBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = cornerRadius,
                    Padding = new Thickness(9),
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var itemGrid = new Grid();
                if (!string.IsNullOrWhiteSpace(item.ImagePath) &&
                    LoadMarketingSiteBitmap(item.ImagePath) is { } image)
                {
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.Children.Add(new Border
                    {
                        Width = 38,
                        Height = 38,
                        CornerRadius = new CornerRadius(5),
                        ClipToBounds = true,
                        Child = new Image { Source = image, Stretch = Stretch.UniformToFill }
                    });
                }
                else
                {
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                var copy = new StackPanel
                {
                    Margin = itemGrid.ColumnDefinitions.Count > 1
                        ? new Thickness(6, 0, 0, 0)
                        : new Thickness(0)
                };
                copy.Children.Add(new TextBlock
                {
                    Text = FirstFilled(item.Title, "Item"),
                    Foreground = foreground,
                    FontSize = 9,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                if (!string.IsNullOrWhiteSpace(item.Text))
                {
                    copy.Children.Add(new TextBlock
                    {
                        Text = item.Text,
                        Foreground = muted,
                        FontSize = 7.5,
                        TextWrapping = TextWrapping.Wrap,
                        MaxHeight = 28,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
                if (!string.IsNullOrWhiteSpace(item.Detail))
                {
                    copy.Children.Add(new TextBlock
                    {
                        Text = item.Detail,
                        Foreground = AccentTextBrush,
                        FontSize = 8,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }
                if (itemGrid.ColumnDefinitions.Count > 1)
                {
                    Grid.SetColumn(copy, 1);
                }
                itemGrid.Children.Add(copy);
                itemBorder.Child = itemGrid;
                itemsPanel.Children.Add(itemBorder);
            }
            root.Children.Add(itemsPanel);
        }
        else if (section.Type is "gallery" or "before-after" or "testimonials" or "brands")
        {
            root.Children.Add(new TextBlock
            {
                Text = "Adicione itens para esta seção aparecer completa no site.",
                Foreground = muted,
                FontSize = 8,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 7, 0, 0)
            });
        }

        if (!string.IsNullOrWhiteSpace(section.ButtonText))
        {
            root.Children.Add(new Border
            {
                Background = AccentBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock
                {
                    Text = section.ButtonText,
                    Foreground = OnAccentBrush,
                    FontSize = 8.5,
                    FontWeight = FontWeights.SemiBold
                }
            });
        }

        border.Child = root;
        return border;
    }

    private Border BuildMarketingSiteServicesPreviewSection(MarketingCatalogSection section)
    {
        var globalDark = string.Equals(
            _data.Settings.MarketingSiteDesign.ColorScheme,
            "dark",
            StringComparison.OrdinalIgnoreCase);
        var foreground = globalDark ? Solid("#FFF9F5") : InkBrush;
        var muted = globalDark ? Solid("#D7CAC2") : MutedBrush;
        var line = globalDark ? Solid("#4F4540") : LineBrush;
        var surface = globalDark ? Solid("#2B2522") : Solid("#FFFDFC");
        var soft = globalDark ? Solid("#3A322E") : Solid("#F8F2EE");
        var selected = _marketingSiteSelectedPart == "section" &&
                       _marketingSiteSelectedSectionId == section.Id;
        var border = new Border
        {
            Tag = section.Id,
            Margin = new Thickness(0, 0, 0, MarketingSiteGlobalSectionGap()),
            Padding = MarketingSiteGlobalSectionPadding(),
            CornerRadius = MarketingSiteGlobalCornerRadius(),
            BorderBrush = selected ? AccentBrush : line,
            BorderThickness = new Thickness(1),
            Background = surface,
            Cursor = Cursors.Hand
        };
        border.MouseLeftButtonDown += MarketingSitePreviewSection_MouseLeftButtonDown;

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = FirstFilled(section.Subtitle, "Serviços"),
            Foreground = AccentTextBrush,
            FontSize = 7.5,
            FontWeight = FontWeights.SemiBold
        });

        var heading = new Grid { Margin = new Thickness(0, 3, 0, 1) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.Children.Add(new TextBlock
        {
            Text = FirstFilled(section.Title, "Escolha seu atendimento"),
            Foreground = foreground,
            FontFamily = new FontFamily(SelectedTag(MarketingSiteFontCombo, "Georgia")),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });
        var type = new TextBlock
        {
            Text = "Serviços e preços",
            Foreground = muted,
            FontSize = 7.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(type, 1);
        heading.Children.Add(type);
        root.Children.Add(heading);

        if (!string.IsNullOrWhiteSpace(section.Body))
        {
            root.Children.Add(new TextBlock
            {
                Text = section.Body,
                Foreground = muted,
                FontSize = 8.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 7)
            });
        }

        var services = MarketingSitePreviewItems(section).Take(12).ToList();
        for (var index = 0; index < services.Count; index++)
        {
            var service = services[index];
            var row = new Grid
            {
                MinHeight = 42,
                Background = index % 2 == 0 ? Brushes.Transparent : soft
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });

            row.Children.Add(new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = soft,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new PackIcon
                {
                    Kind = MarketingSiteServicePreviewIcon(index),
                    Width = 14,
                    Height = 14,
                    Foreground = AccentTextBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            var name = new TextBlock
            {
                Text = FirstFilled(service.Title, "Serviço"),
                Foreground = foreground,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(name, 1);
            row.Children.Add(name);

            var duration = new TextBlock
            {
                Text = FirstFilled(service.Text, "45 min"),
                Foreground = muted,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(duration, 2);
            row.Children.Add(duration);

            var price = new TextBlock
            {
                Text = FirstFilled(service.Detail, "Sob consulta"),
                Foreground = foreground,
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(price, 3);
            row.Children.Add(price);

            var action = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Agendar",
                        Foreground = AccentTextBrush,
                        FontSize = 8.5,
                        FontWeight = FontWeights.SemiBold
                    },
                    new PackIcon
                    {
                        Kind = PackIconKind.ChevronRight,
                        Width = 13,
                        Height = 13,
                        Foreground = AccentTextBrush,
                        Margin = new Thickness(3, 0, 0, 0)
                    }
                }
            };
            Grid.SetColumn(action, 4);
            row.Children.Add(action);

            var rowShell = new Border
            {
                BorderBrush = line,
                BorderThickness = new Thickness(0, index == 0 ? 1 : 0, 0, 1),
                Padding = new Thickness(2, 0, 2, 0),
                Child = row
            };
            root.Children.Add(rowShell);
        }

        border.Child = root;
        return border;
    }

    private static PackIconKind MarketingSiteServicePreviewIcon(int index) => (index % 4) switch
    {
        1 => PackIconKind.StarOutline,
        2 => PackIconKind.AccountOutline,
        3 => PackIconKind.ImageMultipleOutline,
        _ => PackIconKind.CalendarCheckOutline
    };

    private CornerRadius MarketingSiteGlobalCornerRadius() =>
        SelectedTag(MarketingSiteCornerStyleCombo, "rounded") switch
        {
            "sharp" => new CornerRadius(2),
            "soft" => new CornerRadius(7),
            _ => new CornerRadius(12)
        };

    private Thickness MarketingSiteGlobalSectionPadding() =>
        SelectedTag(MarketingSiteSpacingCombo, "compact") switch
        {
            "wide" => new Thickness(20, 18, 20, 18),
            "comfortable" => new Thickness(17, 15, 17, 15),
            _ => new Thickness(14, 12, 14, 12)
        };

    private double MarketingSiteGlobalSectionGap() =>
        SelectedTag(MarketingSiteSpacingCombo, "compact") switch
        {
            "wide" => 16,
            "comfortable" => 12,
            _ => 8
        };

    private IEnumerable<MarketingCatalogSectionItem> MarketingSitePreviewItems(
        MarketingCatalogSection section)
    {
        if (section.Type == "services")
        {
            return _data.Services
                .Where(service => !string.IsNullOrWhiteSpace(service.Name))
                .Take(12)
                .Select(service => new MarketingCatalogSectionItem
                {
                    Id = service.Id,
                    Title = service.Name,
                    Text = $"{service.DurationMinutes} min",
                    Detail = service.Price > 0 ? service.Price.ToString("C") : ""
                });
        }
        if (section.Type == "team")
        {
            return _data.Professionals
                .Where(professional => !string.IsNullOrWhiteSpace(professional.Name))
                .Take(6)
                .Select(professional => new MarketingCatalogSectionItem
                {
                    Id = professional.Id,
                    Title = professional.Name,
                    Text = string.Join(", ", professional.Segments.Take(2))
                });
        }
        return section.Items;
    }

    private void MarketingSitePreviewSection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string sectionId })
        {
            SelectMarketingSitePart("section", sectionId);
            RebuildMarketingSitePreviewSections();
            e.Handled = true;
        }
    }

    private void RebuildMarketingSiteStructureList()
    {
        if (MarketingSiteSectionsListPanel is null)
        {
            return;
        }

        MarketingSiteSectionsListPanel.Children.Clear();
        var sections = _data.Settings.MarketingSiteSections;
        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var row = new Border
            {
                BorderBrush = _marketingSiteSelectedSectionId == section.Id
                    ? AccentBrush
                    : LineBrush,
                BorderThickness = new Thickness(
                    _marketingSiteSelectedSectionId == section.Id ? 1.5 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 6),
                Background = section.Enabled ? Brushes.White : Solid("#F4F1EF")
            };
            var container = new StackPanel();
            var selectButton = new Button
            {
                Tag = section.Id,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = FirstFilled(section.Title, MarketingSiteSectionTypeLabel(section.Type)),
                            Foreground = InkBrush,
                            FontSize = 9.5,
                            FontWeight = FontWeights.SemiBold,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = $"{MarketingSiteSectionTypeLabel(section.Type)} • {(section.Enabled ? "visível" : "oculta")}",
                            Foreground = MutedBrush,
                            FontSize = 7.5,
                            Margin = new Thickness(0, 2, 0, 0)
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = Cursors.Hand
            };
            selectButton.Click += MarketingSiteStructureSectionButton_Click;
            container.Children.Add(selectButton);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 7, 0, 0)
            };
            actions.Children.Add(MarketingSiteStructureActionButton("↑", "Mover para cima", section.Id, MarketingSiteMoveSectionUpButton_Click, index == 0));
            actions.Children.Add(MarketingSiteStructureActionButton("↓", "Mover para baixo", section.Id, MarketingSiteMoveSectionDownButton_Click, index == sections.Count - 1));
            actions.Children.Add(MarketingSiteStructureActionButton(
                section.Enabled ? "Ocultar" : "Exibir",
                section.Enabled ? "Ocultar seção" : "Exibir seção",
                section.Id,
                MarketingSiteToggleSectionButton_Click,
                false,
                47));
            actions.Children.Add(MarketingSiteStructureActionButton(
                "Copiar",
                "Duplicar seção",
                section.Id,
                MarketingSiteDuplicateSectionButton_Click,
                false,
                42));
            actions.Children.Add(MarketingSiteStructureActionButton(
                "Excluir",
                "Excluir seção",
                section.Id,
                MarketingSiteDeleteSectionButton_Click,
                false,
                46));
            container.Children.Add(actions);
            row.Child = container;
            MarketingSiteSectionsListPanel.Children.Add(row);
        }
    }

    private static Button MarketingSiteStructureActionButton(
        string content,
        string name,
        string sectionId,
        RoutedEventHandler handler,
        bool disabled,
        double width = 27)
    {
        var button = new Button
        {
            Content = content,
            Tag = sectionId,
            Width = width,
            MinWidth = width,
            Height = 26,
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(3, 0, 0, 0),
            FontSize = content.Length > 2 ? 7.5 : 11,
            IsEnabled = !disabled,
            Background = Brushes.Transparent,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        AutomationProperties.SetName(button, name);
        button.Click += handler;
        return button;
    }

    private void MarketingSiteStructureSectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string sectionId })
        {
            SelectMarketingSitePart("section", sectionId);
            RebuildMarketingSitePreviewSections();
            RebuildMarketingSiteStructureList();
        }
    }

    private void MarketingSiteMoveSectionUpButton_Click(object sender, RoutedEventArgs e) =>
        MoveMarketingSiteSection(sender, -1);

    private void MarketingSiteMoveSectionDownButton_Click(object sender, RoutedEventArgs e) =>
        MoveMarketingSiteSection(sender, 1);

    private void MoveMarketingSiteSection(object sender, int direction)
    {
        if (sender is not FrameworkElement { Tag: string sectionId })
        {
            return;
        }
        var sections = _data.Settings.MarketingSiteSections;
        var index = sections.FindIndex(section => section.Id == sectionId);
        var destination = index + direction;
        if (index < 0 || destination < 0 || destination >= sections.Count)
        {
            return;
        }
        (sections[index], sections[destination]) = (sections[destination], sections[index]);
        RebuildMarketingSitePreviewSections();
        RebuildMarketingSiteStructureList();
        if (MarketingSiteDynamicSectionsPanel.Children.OfType<UIElement>().LastOrDefault() is { } addedPreview)
        {
            AnimateMarketingSiteElementIn(addedPreview);
        }
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteToggleSectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string sectionId } &&
            _data.Settings.MarketingSiteSections.FirstOrDefault(section => section.Id == sectionId) is { } section)
        {
            section.Enabled = !section.Enabled;
            if (_marketingSiteSelectedSectionId == section.Id)
            {
                LoadMarketingSiteBuilderControls();
            }
            RebuildMarketingSitePreviewSections();
            RebuildMarketingSiteStructureList();
            ScheduleMarketingSiteSave();
        }
    }

    private void MarketingSiteDeleteSectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string sectionId })
        {
            return;
        }
        var section = _data.Settings.MarketingSiteSections.FirstOrDefault(item => item.Id == sectionId);
        if (section is null)
        {
            return;
        }
        if (MessageBox.Show(
                this,
                $"Excluir a seção “{FirstFilled(section.Title, MarketingSiteSectionTypeLabel(section.Type))}”?",
                "Editar site",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }
        var imagePaths = section.Items.Select(item => item.ImagePath).ToList();
        _data.Settings.MarketingSiteSections.Remove(section);
        if (_marketingSiteSelectedSectionId == section.Id)
        {
            SelectMarketingSitePart("hero");
        }
        foreach (var path in imagePaths)
        {
            DeleteManagedMarketingSiteImageIfUnused(path);
        }
        RebuildMarketingSitePreviewSections();
        RebuildMarketingSiteStructureList();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteDuplicateSectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string sectionId })
        {
            return;
        }
        var sections = _data.Settings.MarketingSiteSections;
        var sourceIndex = sections.FindIndex(section => section.Id == sectionId);
        if (sourceIndex < 0)
        {
            return;
        }

        var duplicate = CloneMarketingCatalogSection(sections[sourceIndex]);
        duplicate.Id = Guid.NewGuid().ToString("N");
        duplicate.Title = $"{FirstFilled(duplicate.Title, MarketingSiteSectionTypeLabel(duplicate.Type))} — cópia";
        foreach (var item in duplicate.Items)
        {
            item.Id = Guid.NewGuid().ToString("N");
        }
        sections.Insert(sourceIndex + 1, duplicate);
        SelectMarketingSitePart("section", duplicate.Id);
        RebuildMarketingSitePreviewSections();
        RebuildMarketingSiteStructureList();
        ScheduleMarketingSiteSave();
        ShowStatus("Seção duplicada. Você já pode editar a cópia.");
    }

    private void MarketingSiteAddSectionButton_Click(object sender, RoutedEventArgs e)
    {
        OpenMarketingSiteSectionDrawer();
    }

    private void MarketingSiteCloseInspectorButton_Click(object sender, RoutedEventArgs e) =>
        SetMarketingSiteInspectorVisible(false);

    private void MarketingSiteOpenInspectorButton_Click(object sender, RoutedEventArgs e) =>
        SetMarketingSiteInspectorVisible(true);

    private void SetMarketingSiteInspectorVisible(bool visible)
    {
        if (MarketingSiteInspectorColumn is null ||
            MarketingSiteInspectorGapColumn is null ||
            MarketingSiteEditorInspectorCard is null ||
            MarketingSiteSectionDrawer is null ||
            MarketingSiteOpenInspectorButton is null)
        {
            return;
        }

        MarketingSiteInspectorGapColumn.Width = new GridLength(visible ? 12 : 0);
        MarketingSiteInspectorColumn.Width = new GridLength(visible ? 430 : 0);
        MarketingSiteEditorInspectorCard.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        MarketingSiteSectionDrawer.Visibility = Visibility.Collapsed;
        MarketingSiteOpenInspectorButton.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenMarketingSiteSectionDrawer()
    {
        if (MarketingSiteSectionDrawer is null || MarketingSiteEditorInspectorCard is null)
        {
            return;
        }

        _marketingSiteSectionDrawerCategory = "essential";
        _marketingSiteSectionDrawerSelectedType = new[] { "team", "gallery", "services" }
            .FirstOrDefault(type => !_data.Settings.MarketingSiteSections
                .Any(section => string.Equals(section.Type, type, StringComparison.OrdinalIgnoreCase)))
            ?? "services";
        MarketingSiteInspectorGapColumn.Width = new GridLength(12);
        MarketingSiteInspectorColumn.Width = new GridLength(430);
        MarketingSiteOpenInspectorButton.Visibility = Visibility.Collapsed;
        MarketingSiteSectionDrawerSearchBox.Text = "";
        MarketingSiteEditorInspectorCard.Visibility = Visibility.Collapsed;
        MarketingSiteSectionDrawer.Visibility = Visibility.Visible;
        RebuildMarketingSiteSectionDrawer();
        AnimateMarketingSiteDrawerIn(MarketingSiteSectionDrawer);
        MarketingSiteSectionDrawerSearchBox.Focus();
    }

    private void MarketingSiteCloseSectionDrawerButton_Click(object sender, RoutedEventArgs e) =>
        CloseMarketingSiteSectionDrawer();

    private void CloseMarketingSiteSectionDrawer()
    {
        if (MarketingSiteSectionDrawer is null || MarketingSiteEditorInspectorCard is null)
        {
            return;
        }

        MarketingSiteSectionDrawer.Visibility = Visibility.Collapsed;
        MarketingSiteEditorInspectorCard.Visibility = Visibility.Visible;
        MarketingSiteInspectorGapColumn.Width = new GridLength(12);
        MarketingSiteInspectorColumn.Width = new GridLength(430);
        MarketingSiteOpenInspectorButton.Visibility = Visibility.Collapsed;
    }

    private void MarketingSiteSectionDrawerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MarketingSiteSectionDrawer?.Visibility == Visibility.Visible)
        {
            RebuildMarketingSiteSectionDrawer();
        }
    }

    private void MarketingSiteSectionDrawerCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string category })
        {
            _marketingSiteSectionDrawerCategory = category;
            RebuildMarketingSiteSectionDrawer();
        }
    }

    private void MarketingSiteSectionDrawerCard_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string type })
        {
            _marketingSiteSectionDrawerSelectedType = type;
            RebuildMarketingSiteSectionDrawer();
            e.Handled = true;
        }
    }

    private void MarketingSiteSectionDrawerAddButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string selectedType } ||
            _data.Settings.MarketingSiteSections.Any(section =>
                string.Equals(section.Type, selectedType, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var section = CreateMarketingSiteSection(selectedType, _data.Settings.BusinessSegment);
        _data.Settings.MarketingSiteSections.Add(section);
        SelectMarketingSitePart("section", section.Id);
        RebuildMarketingSitePreviewSections();
        RebuildMarketingSiteStructureList();
        RebuildMarketingSiteSectionDrawer();
        ScheduleMarketingSiteSave();
        ShowStatus($"{MarketingSiteSectionTypeLabel(section.Type)} adicionada ao site.");
    }

    private void RebuildMarketingSiteSectionDrawer()
    {
        if (MarketingSiteSectionDrawerItemsPanel is null)
        {
            return;
        }

        var selectedBackground = Solid("#FFF1E9");
        foreach (var (button, category) in new[]
                 {
                     (MarketingSiteSectionDrawerEssentialButton, "essential"),
                     (MarketingSiteSectionDrawerContentButton, "content"),
                     (MarketingSiteSectionDrawerTrustButton, "trust")
                 })
        {
            var selected = string.Equals(
                _marketingSiteSectionDrawerCategory,
                category,
                StringComparison.OrdinalIgnoreCase);
            button.Background = selected ? selectedBackground : Brushes.Transparent;
            button.Foreground = selected ? AccentTextBrush : InkBrush;
            button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }

        var query = MarketingSiteSectionDrawerSearchBox?.Text?.Trim() ?? "";
        var ordered = MarketingSiteAllSectionTypes()
            .Where(type => MarketingSiteSectionDrawerCategory(type) ==
                           _marketingSiteSectionDrawerCategory)
            .Where(type => string.IsNullOrWhiteSpace(query) ||
                           MarketingSiteSectionTypeLabel(type)
                               .Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           MarketingSiteSectionPickerDescription(type)
                               .Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!ordered.Contains(_marketingSiteSectionDrawerSelectedType) && ordered.Count > 0)
        {
            _marketingSiteSectionDrawerSelectedType = ordered[0];
        }

        var recommendedTypes = MarketingSiteRecommendedSectionTypes(_data.Settings.BusinessSegment);
        var recommended = recommendedTypes.Contains(_marketingSiteSectionDrawerSelectedType) &&
                          !_data.Settings.MarketingSiteSections.Any(section =>
                              string.Equals(
                                  section.Type,
                                  _marketingSiteSectionDrawerSelectedType,
                                  StringComparison.OrdinalIgnoreCase))
            ? _marketingSiteSectionDrawerSelectedType
            : ordered.FirstOrDefault(type =>
                recommendedTypes.Contains(type) &&
                !_data.Settings.MarketingSiteSections.Any(section =>
                    string.Equals(section.Type, type, StringComparison.OrdinalIgnoreCase)));
        MarketingSiteSectionDrawerItemsPanel.Children.Clear();

        if (ordered.Count == 0)
        {
            MarketingSiteSectionDrawerItemsPanel.Children.Add(new TextBlock
            {
                Text = "Nenhuma seção encontrada.",
                Foreground = MutedBrush,
                FontSize = 11,
                Margin = new Thickness(8, 18, 8, 0)
            });
            return;
        }

        foreach (var type in ordered)
        {
            var alreadyAdded = _data.Settings.MarketingSiteSections.Any(section =>
                string.Equals(section.Type, type, StringComparison.OrdinalIgnoreCase));
            MarketingSiteSectionDrawerItemsPanel.Children.Add(
                BuildMarketingSiteSectionDrawerCard(
                    type,
                    alreadyAdded,
                    string.Equals(type, recommended, StringComparison.OrdinalIgnoreCase),
                    string.Equals(
                        type,
                        _marketingSiteSectionDrawerSelectedType,
                        StringComparison.OrdinalIgnoreCase)));
        }
    }

    private Border BuildMarketingSiteSectionDrawerCard(
        string type,
        bool alreadyAdded,
        bool recommended,
        bool selected)
    {
        var card = new Border
        {
            Tag = type,
            Background = selected ? Solid("#FFF9F5") : Brushes.White,
            BorderBrush = selected ? AccentBrush : LineBrush,
            BorderThickness = new Thickness(selected ? 1.5 : 1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(11, 10, 11, 10),
            Margin = new Thickness(0, 0, 0, 9),
            Cursor = alreadyAdded ? Cursors.Arrow : Cursors.Hand
        };
        card.MouseLeftButtonDown += MarketingSiteSectionDrawerCard_MouseLeftButtonDown;

        var root = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(9),
            Background = selected ? Solid("#FFF0E8") : Solid("#F3F0ED"),
            Child = new PackIcon
            {
                Kind = MarketingSiteSectionPickerIcon(type),
                Width = 17,
                Height = 17,
                Foreground = selected ? AccentTextBrush : InkBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = MarketingSiteSectionTypeLabel(type),
            Foreground = InkBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        });
        copy.Children.Add(new TextBlock
        {
            Text = MarketingSiteSectionPickerDescription(type),
            Foreground = MutedBrush,
            FontSize = 8.5,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 225,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(copy, 1);
        header.Children.Add(copy);

        FrameworkElement status;
        if (alreadyAdded)
        {
            status = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.CheckCircleOutline,
                        Width = 14,
                        Height = 14,
                        Foreground = Solid("#20A464"),
                        Margin = new Thickness(0, 1, 4, 0)
                    },
                    new TextBlock
                    {
                        Text = "Já adicionado",
                        Foreground = MutedBrush,
                        FontSize = 8.5
                    }
                }
            };
        }
        else if (recommended)
        {
            status = new TextBlock
            {
                Text = "Recomendado",
                Foreground = AccentTextBrush,
                FontSize = 8.5,
                VerticalAlignment = VerticalAlignment.Top
            };
        }
        else
        {
            status = new Border();
        }
        Grid.SetColumn(status, 2);
        header.Children.Add(status);
        root.Children.Add(header);

        if (selected)
        {
            var preview = BuildMarketingSiteSectionDrawerPreview(type);
            preview.Margin = new Thickness(38, 8, 0, 0);
            root.Children.Add(preview);

            if (!alreadyAdded)
            {
                var addButton = new Button
                {
                    Tag = type,
                    Content = "Adicionar",
                    Height = 30,
                    MinWidth = 78,
                    Padding = new Thickness(12, 0, 12, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 8, 0, 0),
                    Cursor = Cursors.Hand
                };
                addButton.SetResourceReference(StyleProperty, "CommandButton");
                addButton.Click += MarketingSiteSectionDrawerAddButton_Click;
                root.Children.Add(addButton);
            }
        }

        card.Child = root;
        return card;
    }

    private FrameworkElement BuildMarketingSiteSectionDrawerPreview(string type)
    {
        var shell = new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Solid("#FFFCFA"),
            Padding = new Thickness(7),
            Height = 54
        };

        if (type == "services")
        {
            var service = _data.Services.FirstOrDefault();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.Children.Add(new TextBlock
            {
                Text = service?.Name ?? "Manicure",
                Foreground = InkBrush,
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            foreach (var (text, column, brush) in new[]
                     {
                         ($"{service?.DurationMinutes ?? 45} min", 1, MutedBrush),
                         (service?.Price.ToString("C") ?? "R$ 55,00", 2, InkBrush),
                         ("Agendar →", 3, AccentTextBrush)
                     })
            {
                var item = new TextBlock
                {
                    Text = text,
                    Foreground = brush,
                    FontSize = 7.5,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(item, column);
                grid.Children.Add(item);
            }
            shell.Child = grid;
            return shell;
        }

        if (type is "team" or "gallery")
        {
            var images = new[]
            {
                "Assets/marketing-site-hero-hair.png",
                "Assets/marketing-campaign-hair.png",
                "Assets/marketing-campaign-nails.png"
            };
            var grid = new UniformGrid { Columns = 3 };
            foreach (var imagePath in images)
            {
                grid.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(type == "team" ? 18 : 4),
                    ClipToBounds = true,
                    Margin = new Thickness(0, 0, 5, 0),
                    Child = new Image
                    {
                        Source = LoadMarketingSiteBitmap(imagePath),
                        Stretch = Stretch.UniformToFill
                    }
                });
            }
            shell.Child = grid;
            return shell;
        }

        if (type == "testimonials")
        {
            shell.Child = new TextBlock
            {
                Text = "“Atendimento impecável e resultado incrível.”",
                Foreground = InkBrush,
                FontFamily = new FontFamily("Georgia"),
                FontStyle = FontStyles.Italic,
                FontSize = 8.5,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            return shell;
        }

        if (type == "process")
        {
            var steps = new UniformGrid { Columns = 3 };
            foreach (var label in new[] { "1  Agendar", "2  Atender", "3  Finalizar" })
            {
                steps.Children.Add(new TextBlock
                {
                    Text = label,
                    Foreground = InkBrush,
                    FontSize = 7.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            shell.Child = steps;
            return shell;
        }

        shell.Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PackIcon
                {
                    Kind = MarketingSiteSectionPickerIcon(type),
                    Width = 18,
                    Height = 18,
                    Foreground = AccentTextBrush,
                    Margin = new Thickness(2, 0, 9, 0)
                },
                new TextBlock
                {
                    Text = MarketingSiteSectionTypeLabel(type),
                    Foreground = InkBrush,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
        return shell;
    }

    private static string MarketingSiteSectionDrawerCategory(string type) => type switch
    {
        "services" or "team" or "gallery" => "essential",
        "testimonials" or "location" or "brands" => "trust",
        _ => "content"
    };

    private static void AnimateMarketingSiteDrawerIn(UIElement drawer)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var translate = new TranslateTransform(24, 0);
        drawer.RenderTransform = translate;
        drawer.Opacity = 0;
        drawer.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        translate.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(210))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private string ShowMarketingSiteSectionPicker()
    {
        var recommended = MarketingSiteRecommendedSectionTypes(_data.Settings.BusinessSegment);
        var selected = "";
        var window = new Window
        {
            Owner = this,
            Title = "Adicionar seção",
            Width = Math.Min(780, SystemParameters.WorkArea.Width * 0.86),
            Height = Math.Min(660, SystemParameters.WorkArea.Height * 0.86),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Solid("#F7F4F1")
        };
        var root = new Grid { Margin = new Thickness(28, 24, 22, 22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        heading.Children.Add(new TextBlock
        {
            Text = "Adicionar uma seção pronta",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = InkBrush
        });
        heading.Children.Add(new TextBlock
        {
            Text = $"Primeiro aparecem as melhores opções para {FirstFilled(_data.Settings.BusinessSegment, "seu segmento")}. Todo o conteúdo poderá ser editado.",
            FontSize = 11,
            Foreground = MutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        root.Children.Add(heading);

        var stack = new WrapPanel { Width = 720 };
        var ordered = recommended
            .Concat(MarketingSiteAllSectionTypes())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var type in ordered)
        {
            var isRecommended = recommended.Contains(type);
            var button = new Button
            {
                Tag = type,
                Width = 348,
                Height = 84,
                Margin = new Thickness(0, 0, 12, 12),
                Padding = new Thickness(14, 12, 14, 12),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(10),
                Background = isRecommended ? Solid("#FFF0E8") : Solid("#F2EFEC"),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new PackIcon
                {
                    Kind = MarketingSiteSectionPickerIcon(type),
                    Width = 17,
                    Height = 17,
                    Foreground = isRecommended ? AccentTextBrush : MutedBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            copy.Children.Add(new TextBlock
            {
                Text = MarketingSiteSectionTypeLabel(type),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = InkBrush
            });
            copy.Children.Add(new TextBlock
            {
                Text = MarketingSiteSectionPickerDescription(type),
                FontSize = 9.5,
                Foreground = MutedBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 225,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(copy, 1);
            content.Children.Add(copy);
            if (isRecommended)
            {
                var badge = new Border
                {
                    Background = Solid("#FFF0E8"),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(7, 4, 7, 4),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = "INDICADA",
                        FontSize = 7,
                        FontWeight = FontWeights.Bold,
                        Foreground = AccentTextBrush
                    }
                };
                Grid.SetColumn(badge, 2);
                content.Children.Add(badge);
            }
            button.Content = content;
            button.Click += (_, _) =>
            {
                selected = type;
                window.DialogResult = true;
            };
            stack.Children.Add(button);
        }
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        window.Content = root;
        AnimateMarketingSiteWindowIn(window);
        window.ShowDialog();
        return selected;
    }

    private static PackIconKind MarketingSiteSectionPickerIcon(string type) => type switch
    {
        "services" => PackIconKind.CalendarCheckOutline,
        "benefits" => PackIconKind.StarOutline,
        "team" => PackIconKind.AccountGroupOutline,
        "gallery" => PackIconKind.ImageMultipleOutline,
        "before-after" => PackIconKind.Compare,
        "process" => PackIconKind.FormatListNumbered,
        "testimonials" => PackIconKind.MessageTextOutline,
        "faq" => PackIconKind.HelpCircleOutline,
        "brands" => PackIconKind.TagOutline,
        "location" => PackIconKind.MapMarkerOutline,
        "callout" => PackIconKind.BullhornOutline,
        _ => PackIconKind.ViewGridPlusOutline
    };

    private static void AnimateMarketingSiteWindowIn(Window window)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }
        window.Opacity = 0;
        window.Loaded += (_, _) =>
        {
            window.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
        };
    }

    private static void AnimateMarketingSiteElementIn(UIElement element)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }
        var translate = new TranslateTransform(0, 8);
        element.RenderTransform = translate;
        element.Opacity = 0;
        element.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        translate.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(210))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private static string MarketingSiteSectionPickerDescription(string type) => type switch
    {
        "services" => "Usa automaticamente os serviços, valores e duração cadastrados.",
        "team" => "Usa os profissionais cadastrados no estabelecimento.",
        "gallery" => "Fotos do espaço, do trabalho ou dos resultados.",
        "before-after" => "Comparações com fotos reais e autorização do cliente.",
        "testimonials" => "Avaliações verdadeiras adicionadas pelo estabelecimento.",
        "faq" => "Perguntas e respostas importantes antes do atendimento.",
        "location" => "Endereço, horários, telefone e WhatsApp.",
        "callout" => "Uma chamada forte levando diretamente ao agendamento.",
        "process" => "Etapas claras explicando como funciona o atendimento.",
        "brands" => "Marcas, modelos ou especialidades realmente atendidos.",
        _ => "Cartões editáveis para destacar os principais diferenciais."
    };

    private void MarketingSiteSectionItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingMarketingSiteEditor || _updatingMarketingSiteBuilderControls)
        {
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        _marketingSiteSelectedItemId =
            (MarketingSiteSectionItemCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        LoadMarketingSiteSectionItemControls();
    }

    private void MarketingSiteAddSectionItemButton_Click(object sender, RoutedEventArgs e)
    {
        var section = SelectedMarketingSiteSection();
        if (section is null || section.AutomaticContent)
        {
            return;
        }
        if (section.Items.Count >= MarketingSiteMaximumItemsPerSection)
        {
            MessageBox.Show(
                this,
                $"Cada seção pode ter até {MarketingSiteMaximumItemsPerSection} itens.",
                "Editar site",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        var item = new MarketingCatalogSectionItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = section.Type switch
            {
                "faq" => "Nova pergunta",
                "testimonials" => "Nome do cliente",
                "gallery" or "before-after" => "Nova foto",
                _ => "Novo item"
            }
        };
        section.Items.Add(item);
        _marketingSiteSelectedItemId = item.Id;
        _updatingMarketingSiteBuilderControls = true;
        try
        {
            RebuildMarketingSiteSectionItemCombo(section);
        }
        finally
        {
            _updatingMarketingSiteBuilderControls = false;
        }
        LoadMarketingSiteSectionItemControls();
        RebuildMarketingSitePreviewSections();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteRemoveSectionItemButton_Click(object sender, RoutedEventArgs e)
    {
        var section = SelectedMarketingSiteSection();
        var item = SelectedMarketingSiteSectionItem();
        if (section is null || item is null)
        {
            return;
        }
        PersistSelectedMarketingSitePartFromControls();
        var imagePath = item.ImagePath;
        section.Items.Remove(item);
        _marketingSiteSelectedItemId = section.Items.FirstOrDefault()?.Id ?? "";
        _updatingMarketingSiteBuilderControls = true;
        try
        {
            RebuildMarketingSiteSectionItemCombo(section);
        }
        finally
        {
            _updatingMarketingSiteBuilderControls = false;
        }
        LoadMarketingSiteSectionItemControls();
        DeleteManagedMarketingSiteImageIfUnused(imagePath);
        RebuildMarketingSitePreviewSections();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteUploadSectionItemImageButton_Click(object sender, RoutedEventArgs e)
    {
        var item = SelectedMarketingSiteSectionItem();
        if (item is null)
        {
            return;
        }
        var currentImageCount = SelectedMarketingSiteSection()?.Items
            .Count(candidate => !string.IsNullOrWhiteSpace(candidate.ImagePath)) ?? 0;
        var currentSiteImageCount = _data.Settings.MarketingSiteSections
            .SelectMany(section => section.Items)
            .Count(candidate => !string.IsNullOrWhiteSpace(candidate.ImagePath));
        if (string.IsNullOrWhiteSpace(item.ImagePath) &&
            currentImageCount >= MarketingSiteMaximumSectionImages)
        {
            MessageBox.Show(
                this,
                $"Cada seção pode publicar até {MarketingSiteMaximumSectionImages} fotos. A capa não entra nesse limite.",
                "Editar site",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(item.ImagePath) &&
            currentSiteImageCount >= MarketingSiteMaximumSiteImages)
        {
            MessageBox.Show(
                this,
                $"O site pode publicar até {MarketingSiteMaximumSiteImages} fotos nas seções. Remova uma foto para adicionar outra.",
                "Editar site",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Escolha uma foto para esta seção",
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var previousPath = item.ImagePath;
            item.ImagePath = PersistMarketingSiteSectionImage(dialog.FileName);
            MarketingSiteSectionItemThumbnail.Source = LoadMarketingSiteBitmap(item.ImagePath);
            DeleteManagedMarketingSiteImageIfUnused(previousPath);
            RebuildMarketingSitePreviewSections();
            ScheduleMarketingSiteSave();
            ShowStatus("Foto adicionada à seção.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           InvalidDataException or FileFormatException or ArgumentException or
                                           NotSupportedException)
        {
            MessageBox.Show(
                this,
                $"Não foi possível usar essa imagem.\n\n{exception.Message}",
                "Editar site",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void MarketingSiteRemoveSectionItemImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMarketingSiteSectionItem() is not { } item ||
            string.IsNullOrWhiteSpace(item.ImagePath))
        {
            return;
        }
        var previousPath = item.ImagePath;
        item.ImagePath = "";
        MarketingSiteSectionItemThumbnail.Source = null;
        DeleteManagedMarketingSiteImageIfUnused(previousPath);
        RebuildMarketingSitePreviewSections();
        ScheduleMarketingSiteSave();
    }

    private string PersistMarketingSiteSectionImage(string sourcePath)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("A imagem escolhida não foi encontrada.", fullSourcePath);
        }
        if (!MarketingSiteImageExtensions.Contains(Path.GetExtension(fullSourcePath)))
        {
            throw new InvalidDataException("Escolha uma imagem PNG, JPG, JPEG ou BMP.");
        }
        var sourceInfo = new FileInfo(fullSourcePath);
        if (sourceInfo.Length <= 0 || sourceInfo.Length > MarketingSiteImageMaximumSourceBytes)
        {
            throw new InvalidDataException("A imagem deve ter no máximo 30 MB.");
        }
        var source = LoadMarketingSiteBitmap(fullSourcePath)
            ?? throw new InvalidDataException("Não foi possível ler a imagem escolhida.");
        if (source.PixelWidth < 240 || source.PixelHeight < 160)
        {
            throw new InvalidDataException("Escolha uma imagem com pelo menos 240 × 160 pixels.");
        }

        byte[]? encoded = null;
        foreach (var maximumDimension in new[] { 1200, 1000, 800, 640 })
        {
            var longestSide = Math.Max(source.PixelWidth, source.PixelHeight);
            BitmapSource scaled = source;
            if (longestSide > maximumDimension)
            {
                var scale = (double)maximumDimension / longestSide;
                scaled = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                scaled.Freeze();
            }
            foreach (var quality in new[] { 80, 70, 60, 50, 42 })
            {
                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(scaled));
                using var output = new MemoryStream();
                encoder.Save(output);
                encoded = output.ToArray();
                if (encoded.Length <= 120_000)
                {
                    break;
                }
            }
            if (encoded is { Length: <= 120_000 })
            {
                break;
            }
        }
        if (encoded is null)
        {
            throw new InvalidDataException("Não foi possível preparar a imagem.");
        }

        var directory = Path.Combine(_store.DataRoot, "marketing-site");
        Directory.CreateDirectory(directory);
        var destinationPath = Path.Combine(
            directory,
            $"site-section-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.jpg");
        var temporaryPath = $"{destinationPath}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, encoded);
            File.Move(temporaryPath, destinationPath);
            return destinationPath;
        }
        catch
        {
            TryDeleteFile(temporaryPath);
            TryDeleteFile(destinationPath);
            throw;
        }
    }

    private void RefreshAutomaticMarketingSiteSectionContent()
    {
        foreach (var section in _data.Settings.MarketingSiteSections.Where(section => section.AutomaticContent))
        {
            if (section.Type == "team")
            {
                section.Items = _data.Professionals
                    .Where(professional => professional.IsActive &&
                                           !string.IsNullOrWhiteSpace(professional.Name))
                    .Take(12)
                    .Select(professional => new MarketingCatalogSectionItem
                    {
                        Id = professional.Id,
                        Title = professional.Name,
                        Text = string.Join(", ", professional.Segments.Take(3)),
                        Detail = "Profissional"
                    })
                    .ToList();
            }
            else if (section.Type == "location")
            {
                section.Items =
                [
                    new MarketingCatalogSectionItem
                    {
                        Id = "address",
                        Title = "Endereço",
                        Text = FirstFilled(
                            _data.Settings.MarketingSiteFooter.Address,
                            _data.Settings.BusinessAddress)
                    },
                    new MarketingCatalogSectionItem
                    {
                        Id = "hours",
                        Title = "Horários",
                        Text = FirstFilled(
                            _data.Settings.MarketingSiteFooter.Hours,
                            MarketingSiteHoursSummary())
                    },
                    new MarketingCatalogSectionItem
                    {
                        Id = "contact",
                        Title = "Contato",
                        Text = FirstFilled(
                            _data.Settings.MarketingSiteFooter.Phone,
                            _data.Settings.MarketingSiteFooter.WhatsApp,
                            _data.Settings.BusinessPhone,
                            _data.Settings.WhatsAppStorePhone)
                    }
                ];
            }
        }
    }

    private static List<Dictionary<string, object?>> BuildMarketingCatalogSectionsPayload(
        IEnumerable<MarketingCatalogSection> sections) =>
        sections.Take(20).Select(section => new Dictionary<string, object?>
        {
            ["id"] = section.Id,
            ["type"] = section.Type,
            ["title"] = section.Title,
            ["subtitle"] = section.Subtitle,
            ["body"] = section.Body,
            ["buttonText"] = section.ButtonText,
            ["buttonTarget"] = section.ButtonTarget,
            ["layout"] = section.Layout,
            ["background"] = section.Background,
            ["alignment"] = section.Alignment,
            ["enabled"] = section.Enabled,
            ["automaticContent"] = section.AutomaticContent,
            ["items"] = section.Items.Take(12).Select(item => new Dictionary<string, object?>
            {
                ["id"] = item.Id,
                ["title"] = item.Title,
                ["text"] = item.Text,
                ["detail"] = item.Detail,
                ["mediaId"] = string.IsNullOrWhiteSpace(item.ImagePath) ||
                              !File.Exists(item.ImagePath)
                    ? ""
                    : MarketingCatalogMediaId(section.Id, item.Id)
            }).ToList()
        }).ToList();

    private static List<Dictionary<string, object?>> BuildMarketingCatalogMediaUploads(
        IEnumerable<MarketingCatalogSection> sections)
    {
        var uploads = new List<Dictionary<string, object?>>();
        foreach (var section in sections)
        {
            foreach (var item in section.Items.Where(item => !string.IsNullOrWhiteSpace(item.ImagePath)))
            {
                if (uploads.Count >= MarketingSiteMaximumSiteImages)
                {
                    return uploads;
                }
                var dataUrl = BuildMarketingCatalogMediaDataUrl(item.ImagePath);
                if (string.IsNullOrWhiteSpace(dataUrl))
                {
                    continue;
                }
                uploads.Add(new Dictionary<string, object?>
                {
                    ["id"] = MarketingCatalogMediaId(section.Id, item.Id),
                    ["dataUrl"] = dataUrl
                });
            }
        }
        return uploads;
    }

    private static string MarketingCatalogMediaId(string sectionId, string itemId)
    {
        var source = $"{sectionId}-{itemId}".ToLowerInvariant();
        var normalized = new string(source
            .Select(character => char.IsLetterOrDigit(character) || character == '-'
                ? character
                : '-')
            .ToArray());
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }
        return normalized.Trim('-')[..Math.Min(120, normalized.Trim('-').Length)];
    }

    private static string BuildMarketingCatalogMediaDataUrl(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return "";
            }
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length is < 128 or > 160_000)
            {
                return "";
            }
            var extension = Path.GetExtension(path);
            var contentType = extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           ArgumentException or NotSupportedException)
        {
            return "";
        }
    }

    private void MarketingSiteUploadImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Escolha uma foto do computador ou celular conectado",
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var previousPath = _data.Settings.MarketingSiteHeroImagePath;
            var persistedPath = PersistMarketingSiteHeroImage(dialog.FileName);
            _data.Settings.MarketingSiteHeroImagePath = persistedPath;
            _onlineBookingCachedCatalogHeroFingerprint = "";
            _onlineBookingLastSyncedCatalogHeroFingerprint = "";
            ApplyMarketingSiteHeroImage(persistedPath);
            SaveMarketingSiteSettings(markAsPublished: false);
            DeleteManagedMarketingSiteImageIfUnused(previousPath);
            MarketingSiteSavedStatusText.Text = "Sua imagem foi adicionada e salva";
            ShowStatus("Imagem do site atualizada com sucesso.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or FileFormatException or ArgumentException or NotSupportedException)
        {
            MessageBox.Show(
                this,
                $"Não foi possível usar essa imagem.\n\n{exception.Message}",
                "Editar meu site",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private string PersistMarketingSiteHeroImage(string sourcePath)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("A imagem escolhida não foi encontrada.", fullSourcePath);
        }

        if (!MarketingSiteImageExtensions.Contains(Path.GetExtension(fullSourcePath)))
        {
            throw new InvalidDataException("Escolha uma imagem PNG, JPG, JPEG ou BMP.");
        }

        var sourceInfo = new FileInfo(fullSourcePath);
        if (sourceInfo.Length <= 0 || sourceInfo.Length > MarketingSiteImageMaximumSourceBytes)
        {
            throw new InvalidDataException("A imagem deve ter no máximo 30 MB.");
        }

        BitmapFrame sourceFrame;
        using (var input = new FileStream(fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            sourceFrame = decoder.Frames.FirstOrDefault()
                ?? throw new InvalidDataException("Não foi possível ler a imagem escolhida.");
            sourceFrame.Freeze();
        }

        if (sourceFrame.PixelWidth < 320 || sourceFrame.PixelHeight < 180)
        {
            throw new InvalidDataException("Escolha uma imagem com pelo menos 320 × 180 pixels.");
        }

        BitmapSource normalizedSource = sourceFrame;
        var longestSide = Math.Max(sourceFrame.PixelWidth, sourceFrame.PixelHeight);
        if (longestSide > MarketingSiteImageMaximumDimension)
        {
            var scale = (double)MarketingSiteImageMaximumDimension / longestSide;
            normalizedSource = new TransformedBitmap(sourceFrame, new ScaleTransform(scale, scale));
            normalizedSource.Freeze();
        }

        var directory = Path.Combine(_store.DataRoot, "marketing-site");
        Directory.CreateDirectory(directory);
        var destinationPath = Path.Combine(directory, $"site-hero-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.png");
        var temporaryPath = $"{destinationPath}.tmp";
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(normalizedSource));
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                encoder.Save(output);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath);
            return destinationPath;
        }
        catch
        {
            TryDeleteFile(temporaryPath);
            TryDeleteFile(destinationPath);
            throw;
        }
    }

    private void ApplyMarketingSiteHeroImage(string? imagePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(imagePath)
            ? _marketingSiteHeroImages[0]
            : imagePath;
        var image = LoadMarketingSiteBitmap(resolvedPath)
            ?? LoadMarketingSiteBitmap(_marketingSiteHeroImages[0]);
        MarketingSiteHeroImage.Source = image;
        MarketingSiteInspectorThumbnail.Source = image;
    }

    private static BitmapSource? LoadMarketingSiteBitmap(string path)
    {
        try
        {
            if (!Path.IsPathRooted(path))
            {
                var normalizedResourcePath = path.Replace('\\', '/').TrimStart('/');
                var resourceImage = new BitmapImage(new Uri(
                    $"pack://application:,,,/AgendaLivreWindows;component/{normalizedResourcePath}",
                    UriKind.Absolute));
                resourceImage.Freeze();
                return resourceImage;
            }

            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = input;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or FileFormatException or ArgumentException)
        {
            return null;
        }
    }

    private void DeleteManagedMarketingSiteImageIfUnused(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            string.Equals(path, _data.Settings.MarketingSiteHeroImagePath, StringComparison.OrdinalIgnoreCase) ||
            _data.Settings.MarketingSiteSections
                .SelectMany(section => section.Items)
                .Any(item => string.Equals(item.ImagePath, path, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(
                _data.Settings.PublishedMarketingCatalog?.HeroImagePath,
                path,
                StringComparison.OrdinalIgnoreCase) ||
            (_data.Settings.PublishedMarketingCatalog?.Sections ?? [])
                .SelectMany(section => section.Items)
                .Any(item => string.Equals(item.ImagePath, path, StringComparison.OrdinalIgnoreCase)) ||
            !IsManagedMarketingSiteImagePath(path))
        {
            return;
        }

        TryDeleteFile(path);
    }

    private bool IsManagedMarketingSiteImagePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetFullPath(Path.Combine(_store.DataRoot, "marketing-site"))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase) &&
                   (Path.GetFileName(fullPath).StartsWith("site-hero-", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(fullPath).StartsWith("site-section-", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private void MarketingSitePaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string color })
        {
            ApplyMarketingSiteAccentColor(color);
            ScheduleMarketingSiteSave();
        }
    }

    private void MarketingSiteCustomAccentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingMarketingSiteEditor ||
            MarketingSiteCustomAccentTextBox is null ||
            MarketingSiteStyleBookingButtonPreviewBorder is null)
        {
            return;
        }

        var candidate = MarketingSiteCustomAccentTextBox.Text.Trim();
        if (candidate.Length != 7 ||
            candidate[0] != '#' ||
            candidate.Skip(1).Any(character => !Uri.IsHexDigit(character)))
        {
            return;
        }

        ApplyMarketingSiteAccentColor(candidate);
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteStyleButtonTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingMarketingSiteEditor ||
            MarketingSiteStyleButtonTextBox is null ||
            MarketingSiteButtonTextBox is null ||
            MarketingSiteStyleBookingButtonPreviewText is null)
        {
            return;
        }

        var text = MarketingSiteStyleButtonTextBox.Text;
        if (!string.Equals(MarketingSiteButtonTextBox.Text, text, StringComparison.Ordinal))
        {
            MarketingSiteButtonTextBox.Text = text;
        }
        MarketingSiteStyleBookingButtonPreviewText.Text = string.IsNullOrWhiteSpace(text)
            ? "Agendar agora"
            : text.Trim();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteStyleShowButtonToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingMarketingSiteEditor ||
            MarketingSiteStyleShowButtonToggle is null ||
            MarketingSiteShowButtonToggle is null ||
            MarketingSiteStyleBookingButtonPreviewBorder is null)
        {
            return;
        }

        MarketingSiteShowButtonToggle.IsChecked = MarketingSiteStyleShowButtonToggle.IsChecked == true;
        MarketingSiteStyleBookingButtonPreviewBorder.Opacity =
            MarketingSiteStyleShowButtonToggle.IsChecked == true ? 1 : 0.42;
        ScheduleMarketingSiteSave();
    }

    private void ApplyMarketingSiteAccentColor(string? colorValue)
    {
        var colorText = string.IsNullOrWhiteSpace(colorValue) ? "#FF6B4A" : colorValue.Trim();
        Color color;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(colorText);
        }
        catch (FormatException)
        {
            colorText = "#FF6B4A";
            color = (Color)ColorConverter.ConvertFromString(colorText);
        }

        _marketingSiteAccentColor = colorText.ToUpperInvariant();
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        MarketingSiteHeaderButtonBorder.Background = brush;
        MarketingSitePreviewButtonBorder.Background = brush;
        MarketingSitePreviewSectionBadge.Background = brush;
        MarketingSitePreviewTitleUnderline.Background = brush;
        MarketingSitePreviewServicesUnderline.Background = brush;
        MarketingSiteAccentSwatch.Background = brush;
        MarketingSiteAccentHexText.Text = _marketingSiteAccentColor;
        if (MarketingSiteStyleBookingButtonPreviewBorder is not null)
        {
            MarketingSiteStyleBookingButtonPreviewBorder.Background = brush;
        }
        if (MarketingSiteCustomAccentTextBox is not null &&
            !string.Equals(MarketingSiteCustomAccentTextBox.Text, _marketingSiteAccentColor, StringComparison.OrdinalIgnoreCase))
        {
            MarketingSiteCustomAccentTextBox.Text = _marketingSiteAccentColor;
        }

        var foreground = (color.R * 299 + color.G * 587 + color.B * 114) / 1000 >= 150
            ? Brushes.Black
            : Brushes.White;
        MarketingSiteHeaderButtonText.Foreground = foreground;
        MarketingSitePreviewButtonText.Foreground = foreground;
        if (MarketingSiteStyleBookingButtonPreviewText is not null)
        {
            MarketingSiteStyleBookingButtonPreviewText.Foreground = foreground;
        }
        foreach (var text in MarketingSiteStyleBookingButtonPreviewBorder is null
                     ? Enumerable.Empty<TextBlock>()
                     : FindVisualChildren<TextBlock>(MarketingSiteStyleBookingButtonPreviewBorder))
        {
            text.Foreground = foreground;
        }

        foreach (var button in FindVisualChildren<Button>(MarketingSiteStyleInspector).Where(item => item.Tag is string))
        {
            var selected = string.Equals(button.Tag as string, _marketingSiteAccentColor, StringComparison.OrdinalIgnoreCase);
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(0);
            button.BorderBrush = selected ? AccentBrush : Brushes.Transparent;
        }
        RebuildMarketingSitePreviewSections();
    }

    private void MarketingSiteContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ApplyMarketingSiteContrast();
        ScheduleMarketingSiteSave();
    }

    private void ApplyMarketingSiteContrast()
    {
        if (MarketingSiteHeroImage is null || MarketingSiteContrastSlider is null)
        {
            return;
        }

        MarketingSiteHeroImage.Opacity = 0.80 + (MarketingSiteContrastSlider.Value / 100d * 0.20);
    }

    private void MarketingSiteSpacingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyMarketingSiteSpacing();
        ScheduleMarketingSiteSave();
    }

    private void ApplyMarketingSiteSpacing()
    {
        if (MarketingSitePreviewCopyStack is null || MarketingSiteSpacingCombo is null)
        {
            return;
        }

        MarketingSitePreviewCopyStack.Margin = SelectedTag(MarketingSiteSpacingCombo, "compact") switch
        {
            "compact" => new Thickness(30, 20, 0, 0),
            "comfortable" => new Thickness(36, 25, 0, 0),
            "wide" => new Thickness(42, 30, 0, 0),
            _ => new Thickness(30, 20, 0, 0)
        };
        RebuildMarketingSitePreviewSections();
    }

    private void MarketingSiteFontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyMarketingSiteFont();
        ScheduleMarketingSiteSave();
    }

    private void ApplyMarketingSiteFont()
    {
        if (MarketingSitePreviewTitleText is null || MarketingSiteFontCombo is null)
        {
            return;
        }

        var font = new FontFamily(SelectedTag(MarketingSiteFontCombo, "Georgia"));
        MarketingSitePreviewTitleText.FontFamily = font;
        MarketingSitePreviewBusinessNameText.FontFamily = font;
        MarketingSitePreviewFooterBusinessNameText.FontFamily = font;
        RebuildMarketingSitePreviewSections();
    }

    private string MarketingCatalogHeroFingerprint(string? imagePath)
    {
        var path = string.IsNullOrWhiteSpace(imagePath) ? _marketingSiteHeroImages[0] : imagePath;
        if (!Path.IsPathRooted(path))
        {
            return $"resource:{path}";
        }
        try
        {
            var info = new FileInfo(path);
            return info.Exists
                ? $"file:{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}"
                : $"missing:{path}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return $"invalid:{path}";
        }
    }

    private string BuildMarketingCatalogHeroDataUrl(string? imagePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(imagePath) ? _marketingSiteHeroImages[0] : imagePath;
        var source = LoadMarketingSiteBitmap(resolvedPath) ?? LoadMarketingSiteBitmap(_marketingSiteHeroImages[0]);
        if (source is null)
        {
            return "";
        }

        foreach (var maximumDimension in new[] { 1600, 1400, 1200, 1000 })
        {
            var longestSide = Math.Max(source.PixelWidth, source.PixelHeight);
            BitmapSource scaled = source;
            if (longestSide > maximumDimension)
            {
                var scale = (double)maximumDimension / longestSide;
                scaled = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                scaled.Freeze();
            }

            foreach (var quality in new[] { 84, 76, 68, 60 })
            {
                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(scaled));
                using var output = new MemoryStream();
                encoder.Save(output);
                if (output.Length <= 1_000_000)
                {
                    return $"data:image/jpeg;base64,{Convert.ToBase64String(output.ToArray())}";
                }
            }
        }

        return "";
    }

    private void ShowMarketingSitePreviewWindow()
    {
        PersistSelectedMarketingSitePartFromControls();
        UpdateMarketingSiteBuilderPreview();
        MarketingSitePreviewSectionsScroll.ScrollToTop();
        MarketingSitePreviewSectionsScroll.UpdateLayout();

        if (MarketingSitePreviewSectionsScroll.Content is not FrameworkElement pageVisual)
        {
            return;
        }

        var editorOnlyElements = FindVisualChildren<Button>(pageVisual)
            .Cast<UIElement>()
            .Concat(
                FindVisualChildren<PackIcon>(pageVisual)
                    .Where(icon => icon.Kind == PackIconKind.DragHorizontal)
                    .Select(icon => VisualTreeHelper.GetParent(icon))
                    .OfType<UIElement>())
            .Append(MarketingSitePreviewSectionBadge)
            .Distinct()
            .ToList();
        var visibilitySnapshot = editorOnlyElements
            .ToDictionary(element => element, element => element.Visibility);
        var selectionBorders = new[]
            {
                MarketingSitePreviewHeaderSelectionBorder,
                MarketingSiteHeroSelectionBorder,
                MarketingSitePreviewFooterSelectionBorder
            }
            .Concat(MarketingSiteDynamicSectionsPanel.Children.OfType<Border>())
            .Distinct()
            .ToList();
        var borderSnapshot = selectionBorders.ToDictionary(
            border => border,
            border => (border.BorderBrush, border.BorderThickness));
        RenderTargetBitmap bitmap;
        try
        {
            foreach (var element in editorOnlyElements)
            {
                element.Visibility = Visibility.Collapsed;
            }
            MarketingSitePreviewHeaderSelectionBorder.BorderBrush = LineBrush;
            MarketingSitePreviewHeaderSelectionBorder.BorderThickness = new Thickness(0, 0, 0, 1);
            MarketingSiteHeroSelectionBorder.BorderBrush = Brushes.Transparent;
            MarketingSiteHeroSelectionBorder.BorderThickness = new Thickness(0);
            MarketingSitePreviewFooterSelectionBorder.BorderBrush = LineBrush;
            MarketingSitePreviewFooterSelectionBorder.BorderThickness = new Thickness(1);
            foreach (var border in MarketingSiteDynamicSectionsPanel.Children.OfType<Border>())
            {
                border.BorderBrush = LineBrush;
                border.BorderThickness = new Thickness(1);
            }

            var pageWidth = Math.Max(1, pageVisual.ActualWidth);
            pageVisual.Measure(new Size(pageWidth, double.PositiveInfinity));
            var pageHeight = Math.Max(pageVisual.ActualHeight, pageVisual.DesiredSize.Height);
            pageVisual.Arrange(new Rect(0, 0, pageWidth, pageHeight));
            pageVisual.UpdateLayout();

            var dpi = VisualTreeHelper.GetDpi(pageVisual);
            bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(pageWidth)),
                Math.Max(1, (int)Math.Ceiling(pageHeight)),
                dpi.PixelsPerInchX,
                dpi.PixelsPerInchY,
                PixelFormats.Pbgra32);
            bitmap.Render(pageVisual);
            bitmap.Freeze();
        }
        finally
        {
            foreach (var (element, visibility) in visibilitySnapshot)
            {
                element.Visibility = visibility;
            }
            foreach (var (border, snapshot) in borderSnapshot)
            {
                border.BorderBrush = snapshot.BorderBrush;
                border.BorderThickness = snapshot.BorderThickness;
            }
            pageVisual.UpdateLayout();
        }

        var previewImage = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };
        var previewWindow = new Window
        {
            Owner = this,
            Title = "Prévia do meu site",
            Width = Math.Min(1180, SystemParameters.WorkArea.Width * 0.92),
            Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.9),
            MinWidth = 720,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Solid("#ECE8E4"),
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        WindowChrome.SetWindowChrome(
            previewWindow,
            new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                CornerRadius = new CornerRadius(10),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });

        var previewRoot = new Border
        {
            Background = Solid("#ECE8E4"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            SnapsToDevicePixels = true
        };
        var previewLayout = new Grid();
        previewLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
        previewLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        previewRoot.Child = previewLayout;

        var previewTitleBar = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            Cursor = Cursors.Arrow
        };
        AutomationProperties.SetName(previewTitleBar, "Barra da janela de prévia");
        var previewTitleGrid = new Grid { Margin = new Thickness(16, 0, 7, 0) };
        previewTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        previewTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        previewTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        previewTitleBar.Child = previewTitleGrid;

        var previewIdentity = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        previewIdentity.Children.Add(new Image
        {
            Source = LoadMarketingSiteBitmap("Assets/agenda-livre-mark.png"),
            Width = 27,
            Height = 27,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 10, 0)
        });
        var previewTitleCopy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        previewTitleCopy.Children.Add(new TextBlock
        {
            Text = "Prévia do meu site",
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        });
        previewTitleCopy.Children.Add(new TextBlock
        {
            Text = MarketingSiteDisplayUrl(),
            Foreground = MutedBrush,
            FontSize = 9.5,
            Margin = new Thickness(0, 2, 0, 0)
        });
        previewIdentity.Children.Add(previewTitleCopy);
        previewTitleGrid.Children.Add(previewIdentity);

        var previewModeBadge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Solid("#FFF4ED"),
            BorderBrush = Solid("#F6C8AE"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(previewModeBadge, 1);
        var previewModeContent = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        previewModeContent.Children.Add(new PackIcon
        {
            Kind = PackIconKind.EyeOutline,
            Foreground = AccentTextBrush,
            Width = 14,
            Height = 14,
            Margin = new Thickness(0, 0, 6, 0)
        });
        previewModeContent.Children.Add(new TextBlock
        {
            Text = "SOMENTE VISUALIZAÇÃO",
            Foreground = AccentTextBrush,
            FontSize = 8.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        previewModeBadge.Child = previewModeContent;
        previewTitleGrid.Children.Add(previewModeBadge);

        Button CreatePreviewChromeButton(PackIconKind iconKind, string automationName, bool closeButton = false)
        {
            var icon = new PackIcon
            {
                Kind = iconKind,
                Width = 16,
                Height = 16,
                Foreground = InkBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var button = new Button
            {
                Width = 40,
                Height = 36,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Content = icon
            };
            AutomationProperties.SetName(button, automationName);
            button.MouseEnter += (_, _) =>
            {
                button.Background = closeButton ? Solid("#DC2626") : Solid("#F3F1EF");
                icon.Foreground = closeButton ? Brushes.White : InkBrush;
            };
            button.MouseLeave += (_, _) =>
            {
                button.Background = Brushes.Transparent;
                icon.Foreground = InkBrush;
            };
            return button;
        }

        var previewWindowButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(previewWindowButtons, 2);
        var minimizeButton = CreatePreviewChromeButton(PackIconKind.WindowMinimize, "Minimizar janela");
        var maximizeButton = CreatePreviewChromeButton(PackIconKind.WindowMaximize, "Maximizar janela");
        var maximizeIcon = (PackIcon)maximizeButton.Content;
        var closeButton = CreatePreviewChromeButton(PackIconKind.Close, "Fechar prévia", closeButton: true);
        minimizeButton.Click += (_, _) => previewWindow.WindowState = WindowState.Minimized;
        maximizeButton.Click += (_, _) =>
            previewWindow.WindowState = previewWindow.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        closeButton.Click += (_, _) => previewWindow.Close();
        previewWindowButtons.Children.Add(minimizeButton);
        previewWindowButtons.Children.Add(maximizeButton);
        previewWindowButtons.Children.Add(closeButton);
        previewTitleGrid.Children.Add(previewWindowButtons);

        previewTitleBar.MouseLeftButtonDown += (_, eventArgs) =>
        {
            if (eventArgs.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (eventArgs.ClickCount == 2)
            {
                previewWindow.WindowState = previewWindow.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                return;
            }

            try
            {
                previewWindow.DragMove();
            }
            catch (InvalidOperationException)
            {
                // O botão pode ser solto antes de o movimento nativo começar.
            }
        };
        previewWindow.StateChanged += (_, _) =>
        {
            var maximized = previewWindow.WindowState == WindowState.Maximized;
            maximizeIcon.Kind = maximized ? PackIconKind.WindowRestore : PackIconKind.WindowMaximize;
            AutomationProperties.SetName(maximizeButton, maximized ? "Restaurar janela" : "Maximizar janela");
            previewRoot.CornerRadius = maximized ? new CornerRadius(0) : new CornerRadius(10);
            previewTitleBar.CornerRadius = maximized
                ? new CornerRadius(0)
                : new CornerRadius(10, 10, 0, 0);
        };
        previewWindow.PreviewKeyDown += (_, eventArgs) =>
        {
            if (eventArgs.Key == Key.Escape)
            {
                previewWindow.Close();
                eventArgs.Handled = true;
            }
        };
        previewLayout.Children.Add(previewTitleBar);

        var previewScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Content = new Border
            {
                Background = Brushes.White,
                Margin = new Thickness(22),
                BorderBrush = Solid("#DED8D3"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Child = previewImage
            }
        };
        Grid.SetRow(previewScroll, 1);
        previewLayout.Children.Add(previewScroll);
        previewWindow.Content = previewRoot;

        AnimateMarketingSiteWindowIn(previewWindow);
        previewWindow.ShowDialog();
    }

    private void ShowMarketingSitePreviewWindowLegacy()
    {
        PersistSelectedMarketingSitePartFromControls();
        var settings = _data.Settings;
        var page = new StackPanel
        {
            Width = Math.Min(1040, SystemParameters.WorkArea.Width * 0.82),
            Background = Brushes.White
        };

        var header = new Grid
        {
            Height = 68,
            Background = settings.MarketingSiteHeader.Background == "soft"
                ? Solid("#FFF5F0")
                : Brushes.White,
            Margin = new Thickness(0)
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock
        {
            Text = FirstFilled(settings.MarketingSiteHeader.BusinessName, BusinessDisplayName()),
            Foreground = InkBrush,
            FontFamily = new FontFamily(settings.MarketingSiteTitleFont),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(28, 0, 0, 0)
        });
        if (settings.MarketingSiteHeader.ShowButton)
        {
            var headerButton = new Border
            {
                Background = AccentBrush,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(18, 10, 18, 10),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 28, 0),
                Child = new TextBlock
                {
                    Text = FirstFilled(settings.MarketingSiteHeader.ButtonText, "Agendar agora"),
                    Foreground = OnAccentBrush,
                    FontWeight = FontWeights.SemiBold
                }
            };
            Grid.SetColumn(headerButton, 1);
            header.Children.Add(headerButton);
        }
        page.Children.Add(header);

        var hero = new Grid { Height = 430, Background = AccentBrush };
        if (LoadMarketingSiteBitmap(settings.MarketingSiteHeroImagePath) is { } heroImage)
        {
            hero.Children.Add(new Image { Source = heroImage, Stretch = Stretch.UniformToFill });
            hero.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(
                    (byte)Math.Clamp(settings.MarketingSiteImageContrast * 1.75, 45, 190),
                    21,
                    16,
                    14))
            });
        }
        var heroCopy = new StackPanel
        {
            Width = 660,
            HorizontalAlignment = settings.MarketingSiteAlignment == "center"
                ? HorizontalAlignment.Center
                : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(48)
        };
        heroCopy.Children.Add(new TextBlock
        {
            Text = settings.MarketingSiteTitle,
            Foreground = Brushes.White,
            FontFamily = new FontFamily(settings.MarketingSiteTitleFont),
            FontSize = 46,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = settings.MarketingSiteAlignment == "center"
                ? TextAlignment.Center
                : TextAlignment.Left
        });
        heroCopy.Children.Add(new TextBlock
        {
            Text = settings.MarketingSiteSupportText,
            Foreground = Solid("#F7EEEA"),
            FontSize = 17,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 16, 0, 0),
            TextAlignment = settings.MarketingSiteAlignment == "center"
                ? TextAlignment.Center
                : TextAlignment.Left
        });
        if (settings.MarketingSiteShowButton)
        {
            heroCopy.Children.Add(new Border
            {
                Background = AccentBrush,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(20, 11, 20, 11),
                HorizontalAlignment = settings.MarketingSiteAlignment == "center"
                    ? HorizontalAlignment.Center
                    : HorizontalAlignment.Left,
                Margin = new Thickness(0, 22, 0, 0),
                Child = new TextBlock
                {
                    Text = FirstFilled(settings.MarketingSiteButtonText, "Agendar agora"),
                    Foreground = OnAccentBrush,
                    FontWeight = FontWeights.SemiBold
                }
            });
        }
        hero.Children.Add(heroCopy);
        page.Children.Add(hero);

        var sectionStack = new StackPanel { Margin = new Thickness(28, 28, 28, 18) };
        foreach (var section in settings.MarketingSiteSections.Where(section => section.Enabled))
        {
            sectionStack.Children.Add(BuildMarketingSitePreviewSection(section));
        }
        page.Children.Add(sectionStack);

        var footerContent = new StackPanel();
        footerContent.Children.Add(new TextBlock
        {
            Text = FirstFilled(settings.MarketingSiteFooter.BusinessName, BusinessDisplayName()),
            Foreground = Brushes.White,
            FontFamily = new FontFamily(settings.MarketingSiteTitleFont),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        });
        footerContent.Children.Add(new TextBlock
        {
            Text = settings.MarketingSiteFooter.Description,
            Foreground = Solid("#D9CCC5"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 18)
        });
        footerContent.Children.Add(new TextBlock
        {
            Text = "Desenvolvido por Agenda Livre",
            Foreground = Solid("#AFA09A"),
            FontSize = 11
        });
        page.Children.Add(new Border
        {
            Background = Solid("#211C1A"),
            Padding = new Thickness(34, 28, 34, 22),
            Child = footerContent
        });

        var previewWindow = new Window
        {
            Owner = this,
            Title = "Prévia do meu site",
            Width = Math.Min(1180, SystemParameters.WorkArea.Width * 0.9),
            Height = Math.Min(760, SystemParameters.WorkArea.Height * 0.88),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(242, 239, 236)),
            Content = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = new Border { Margin = new Thickness(18), Child = page }
            }
        };
        previewWindow.ShowDialog();
    }
}
