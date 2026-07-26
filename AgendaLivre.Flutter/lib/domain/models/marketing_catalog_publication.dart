import 'json_helpers.dart';

DateTime _marketingToday() {
  final now = DateTime.now();
  return DateTime(now.year, now.month, now.day);
}

class MarketingCatalogPublication {
  MarketingCatalogPublication({
    this.addressSnapshotVersion = 0,
    this.slug = '',
    this.customDomain = '',
    this.title = 'Sua beleza, do seu jeito',
    this.supportText = '',
    this.buttonText = 'Agendar agora',
    this.heroImagePath = '',
    this.accentColor = '#FF6B4A',
    this.alignment = 'left',
    this.spacing = 'compact',
    this.titleFont = 'Georgia',
    this.imageContrast = 64,
    this.showButton = true,
    MarketingCatalogHeader? header,
    MarketingCatalogFooter? footer,
    MarketingCatalogDesign? design,
    List<MarketingCatalogSection>? sections,
    this.seoTitle = '',
    this.seoDescription = '',
    this.promotion,
    this.publishedAt,
  }) : header = header ?? MarketingCatalogHeader(),
       footer = footer ?? MarketingCatalogFooter(),
       design = design ?? MarketingCatalogDesign(),
       sections = List<MarketingCatalogSection>.of(
         sections ?? const <MarketingCatalogSection>[],
       );

  int addressSnapshotVersion;
  String slug;
  String customDomain;
  String title;
  String supportText;
  String buttonText;
  String heroImagePath;
  String accentColor;
  String alignment;
  String spacing;
  String titleFont;
  double imageContrast;
  bool showButton;
  MarketingCatalogHeader header;
  MarketingCatalogFooter footer;
  MarketingCatalogDesign design;
  List<MarketingCatalogSection> sections;
  String seoTitle;
  String seoDescription;
  MarketingSitePromotion? promotion;
  DateTime? publishedAt;

  factory MarketingCatalogPublication.fromJson(JsonMap json) =>
      MarketingCatalogPublication(
        addressSnapshotVersion: jsonInt(json, 'AddressSnapshotVersion'),
        slug: jsonString(json, 'Slug'),
        customDomain: jsonString(json, 'CustomDomain'),
        title: jsonString(json, 'Title', fallback: 'Sua beleza, do seu jeito'),
        supportText: jsonString(json, 'SupportText'),
        buttonText: jsonString(json, 'ButtonText', fallback: 'Agendar agora'),
        heroImagePath: jsonString(json, 'HeroImagePath'),
        accentColor: jsonString(json, 'AccentColor', fallback: '#FF6B4A'),
        alignment: jsonString(json, 'Alignment', fallback: 'left'),
        spacing: jsonString(json, 'Spacing', fallback: 'compact'),
        titleFont: jsonString(json, 'TitleFont', fallback: 'Georgia'),
        imageContrast: jsonDouble(json, 'ImageContrast', fallback: 64),
        showButton: jsonBool(json, 'ShowButton', fallback: true),
        header: MarketingCatalogHeader.fromJson(jsonObject(json, 'Header')),
        footer: MarketingCatalogFooter.fromJson(jsonObject(json, 'Footer')),
        design: MarketingCatalogDesign.fromJson(jsonObject(json, 'Design')),
        sections: jsonObjectList(
          json,
          'Sections',
        ).map(MarketingCatalogSection.fromJson).toList(growable: true),
        seoTitle: jsonString(json, 'SeoTitle'),
        seoDescription: jsonString(json, 'SeoDescription'),
        promotion: () {
          final value = jsonObject(json, 'Promotion');
          return value.isEmpty ? null : MarketingSitePromotion.fromJson(value);
        }(),
        publishedAt: jsonNullableDateTime(json, 'PublishedAt'),
      );

  JsonMap toJson() => <String, dynamic>{
    'AddressSnapshotVersion': addressSnapshotVersion,
    'Slug': slug,
    'CustomDomain': customDomain,
    'Title': title,
    'SupportText': supportText,
    'ButtonText': buttonText,
    'HeroImagePath': heroImagePath,
    'AccentColor': accentColor,
    'Alignment': alignment,
    'Spacing': spacing,
    'TitleFont': titleFont,
    'ImageContrast': imageContrast,
    'ShowButton': showButton,
    'Header': header.toJson(),
    'Footer': footer.toJson(),
    'Design': design.toJson(),
    'Sections': sections.map((section) => section.toJson()).toList(),
    'SeoTitle': seoTitle,
    'SeoDescription': seoDescription,
    'Promotion': promotion?.toJson(),
    'PublishedAt': dateTimeToJson(publishedAt),
  };
}

class MarketingSitePromotion {
  MarketingSitePromotion({
    this.name = 'Semana do autocuidado',
    DateTime? startDate,
    DateTime? endDate,
    this.limitPerCustomer = 1,
    this.highlightInCatalog = true,
    this.isPublished = false,
    this.publishedAt,
    List<MarketingSitePromotionItem>? items,
  }) : startDate = startDate ?? _marketingToday(),
       endDate = endDate ?? _marketingToday().add(const Duration(days: 7)),
       items = List<MarketingSitePromotionItem>.of(
         items ?? const <MarketingSitePromotionItem>[],
       );

  String name;
  DateTime startDate;
  DateTime endDate;
  int limitPerCustomer;
  bool highlightInCatalog;
  bool isPublished;
  DateTime? publishedAt;
  List<MarketingSitePromotionItem> items;

  factory MarketingSitePromotion.fromJson(JsonMap json) {
    final today = _marketingToday();
    return MarketingSitePromotion(
      name: jsonString(json, 'Name', fallback: 'Semana do autocuidado'),
      startDate: jsonDateTime(json, 'StartDate', fallback: today),
      endDate: jsonDateTime(
        json,
        'EndDate',
        fallback: today.add(const Duration(days: 7)),
      ),
      limitPerCustomer: jsonInt(json, 'LimitPerCustomer', fallback: 1),
      highlightInCatalog: jsonBool(json, 'HighlightInCatalog', fallback: true),
      isPublished: jsonBool(json, 'IsPublished'),
      publishedAt: jsonNullableDateTime(json, 'PublishedAt'),
      items: jsonObjectList(
        json,
        'Items',
      ).map(MarketingSitePromotionItem.fromJson).toList(growable: true),
    );
  }

  JsonMap toJson() => <String, dynamic>{
    'Name': name,
    'StartDate': dateTimeToJson(startDate),
    'EndDate': dateTimeToJson(endDate),
    'LimitPerCustomer': limitPerCustomer,
    'HighlightInCatalog': highlightInCatalog,
    'IsPublished': isPublished,
    'PublishedAt': dateTimeToJson(publishedAt),
    'Items': items.map((item) => item.toJson()).toList(),
  };
}

class MarketingSitePromotionItem {
  MarketingSitePromotionItem({
    this.serviceId = '',
    this.serviceName = '',
    this.originalPrice = 0,
    this.promotionalPrice = 0,
  });

  String serviceId;
  String serviceName;
  double originalPrice;
  double promotionalPrice;

  factory MarketingSitePromotionItem.fromJson(JsonMap json) =>
      MarketingSitePromotionItem(
        serviceId: jsonString(json, 'ServiceId'),
        serviceName: jsonString(json, 'ServiceName'),
        originalPrice: jsonDouble(json, 'OriginalPrice'),
        promotionalPrice: jsonDouble(json, 'PromotionalPrice'),
      );

  JsonMap toJson() => <String, dynamic>{
    'ServiceId': serviceId,
    'ServiceName': serviceName,
    'OriginalPrice': originalPrice,
    'PromotionalPrice': promotionalPrice,
  };
}

class MarketingCatalogHeader {
  MarketingCatalogHeader({
    this.businessName = '',
    this.subtitle = '',
    this.buttonText = 'Agendar agora',
    this.showLogo = true,
    this.showNavigation = true,
    this.showButton = true,
    this.sticky = true,
    this.background = 'solid',
  });

  String businessName;
  String subtitle;
  String buttonText;
  bool showLogo;
  bool showNavigation;
  bool showButton;
  bool sticky;
  String background;

  factory MarketingCatalogHeader.fromJson(JsonMap json) =>
      MarketingCatalogHeader(
        businessName: jsonString(json, 'BusinessName'),
        subtitle: jsonString(json, 'Subtitle'),
        buttonText: jsonString(json, 'ButtonText', fallback: 'Agendar agora'),
        showLogo: jsonBool(json, 'ShowLogo', fallback: true),
        showNavigation: jsonBool(json, 'ShowNavigation', fallback: true),
        showButton: jsonBool(json, 'ShowButton', fallback: true),
        sticky: jsonBool(json, 'Sticky', fallback: true),
        background: jsonString(json, 'Background', fallback: 'solid'),
      );

  JsonMap toJson() => <String, dynamic>{
    'BusinessName': businessName,
    'Subtitle': subtitle,
    'ButtonText': buttonText,
    'ShowLogo': showLogo,
    'ShowNavigation': showNavigation,
    'ShowButton': showButton,
    'Sticky': sticky,
    'Background': background,
  };
}

class MarketingCatalogFooter {
  MarketingCatalogFooter({
    this.businessName = '',
    this.description = '',
    this.address = '',
    this.phone = '',
    this.hours = '',
    this.instagram = '',
    this.whatsApp = '',
    this.showContact = true,
    this.showHours = true,
    this.showSocial = true,
  });

  String businessName;
  String description;
  String address;
  String phone;
  String hours;
  String instagram;
  String whatsApp;
  bool showContact;
  bool showHours;
  bool showSocial;

  factory MarketingCatalogFooter.fromJson(JsonMap json) =>
      MarketingCatalogFooter(
        businessName: jsonString(json, 'BusinessName'),
        description: jsonString(json, 'Description'),
        address: jsonString(json, 'Address'),
        phone: jsonString(json, 'Phone'),
        hours: jsonString(json, 'Hours'),
        instagram: jsonString(json, 'Instagram'),
        whatsApp: jsonString(json, 'WhatsApp'),
        showContact: jsonBool(json, 'ShowContact', fallback: true),
        showHours: jsonBool(json, 'ShowHours', fallback: true),
        showSocial: jsonBool(json, 'ShowSocial', fallback: true),
      );

  JsonMap toJson() => <String, dynamic>{
    'BusinessName': businessName,
    'Description': description,
    'Address': address,
    'Phone': phone,
    'Hours': hours,
    'Instagram': instagram,
    'WhatsApp': whatsApp,
    'ShowContact': showContact,
    'ShowHours': showHours,
    'ShowSocial': showSocial,
  };
}

class MarketingCatalogDesign {
  MarketingCatalogDesign({
    this.colorScheme = 'warm',
    this.buttonStyle = 'rounded',
    this.cornerStyle = 'rounded',
    this.contentWidth = 'standard',
  });

  String colorScheme;
  String buttonStyle;
  String cornerStyle;
  String contentWidth;

  factory MarketingCatalogDesign.fromJson(JsonMap json) =>
      MarketingCatalogDesign(
        colorScheme: jsonString(json, 'ColorScheme', fallback: 'warm'),
        buttonStyle: jsonString(json, 'ButtonStyle', fallback: 'rounded'),
        cornerStyle: jsonString(json, 'CornerStyle', fallback: 'rounded'),
        contentWidth: jsonString(json, 'ContentWidth', fallback: 'standard'),
      );

  JsonMap toJson() => <String, dynamic>{
    'ColorScheme': colorScheme,
    'ButtonStyle': buttonStyle,
    'CornerStyle': cornerStyle,
    'ContentWidth': contentWidth,
  };
}

class MarketingCatalogSection {
  MarketingCatalogSection({
    this.id = '',
    this.type = 'benefits',
    this.title = '',
    this.subtitle = '',
    this.body = '',
    this.buttonText = '',
    this.buttonTarget = 'booking',
    this.layout = 'cards',
    this.background = 'light',
    this.alignment = 'left',
    this.enabled = true,
    this.automaticContent = false,
    List<MarketingCatalogSectionItem>? items,
  }) : items = List<MarketingCatalogSectionItem>.of(
         items ?? const <MarketingCatalogSectionItem>[],
       );

  String id;
  String type;
  String title;
  String subtitle;
  String body;
  String buttonText;
  String buttonTarget;
  String layout;
  String background;
  String alignment;
  bool enabled;
  bool automaticContent;
  List<MarketingCatalogSectionItem> items;

  factory MarketingCatalogSection.fromJson(JsonMap json) =>
      MarketingCatalogSection(
        id: jsonString(json, 'Id'),
        type: jsonString(json, 'Type', fallback: 'benefits'),
        title: jsonString(json, 'Title'),
        subtitle: jsonString(json, 'Subtitle'),
        body: jsonString(json, 'Body'),
        buttonText: jsonString(json, 'ButtonText'),
        buttonTarget: jsonString(json, 'ButtonTarget', fallback: 'booking'),
        layout: jsonString(json, 'Layout', fallback: 'cards'),
        background: jsonString(json, 'Background', fallback: 'light'),
        alignment: jsonString(json, 'Alignment', fallback: 'left'),
        enabled: jsonBool(json, 'Enabled', fallback: true),
        automaticContent: jsonBool(json, 'AutomaticContent'),
        items: jsonObjectList(
          json,
          'Items',
        ).map(MarketingCatalogSectionItem.fromJson).toList(growable: true),
      );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Type': type,
    'Title': title,
    'Subtitle': subtitle,
    'Body': body,
    'ButtonText': buttonText,
    'ButtonTarget': buttonTarget,
    'Layout': layout,
    'Background': background,
    'Alignment': alignment,
    'Enabled': enabled,
    'AutomaticContent': automaticContent,
    'Items': items.map((item) => item.toJson()).toList(),
  };
}

class MarketingCatalogSectionItem {
  MarketingCatalogSectionItem({
    this.id = '',
    this.title = '',
    this.text = '',
    this.detail = '',
    this.imagePath = '',
  });

  String id;
  String title;
  String text;
  String detail;
  String imagePath;

  factory MarketingCatalogSectionItem.fromJson(JsonMap json) =>
      MarketingCatalogSectionItem(
        id: jsonString(json, 'Id'),
        title: jsonString(json, 'Title'),
        text: jsonString(json, 'Text'),
        detail: jsonString(json, 'Detail'),
        imagePath: jsonString(json, 'ImagePath'),
      );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Title': title,
    'Text': text,
    'Detail': detail,
    'ImagePath': imagePath,
  };
}
