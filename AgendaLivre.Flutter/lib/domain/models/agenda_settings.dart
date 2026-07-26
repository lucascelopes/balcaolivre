import 'json_helpers.dart';
import 'marketing_catalog_publication.dart';

class AgendaSettings {
  AgendaSettings({
    this.accountFullName = '',
    this.accountPhone = '',
    this.accountEmail = '',
    this.businessName = 'Balcão Livre',
    this.businessDocument = '',
    this.businessPhone = '',
    this.pixKey = '',
    this.businessAddress = '',
    this.businessLogoPath = '',
    this.publicBookingSlug = '',
    this.publicBookingUrl = '',
    this.publicBookingApiUrl = 'https://minhaagendalivre.com.br',
    this.publicBookingLastSyncAt,
    this.publicBookingCustomDomain = '',
    this.publicBookingCustomDomainStatus = '',
    this.publicBookingCustomDomainProviderStatus = '',
    this.publicBookingCustomDomainSslStatus = '',
    this.publicBookingCustomDomainCnameTarget = '',
    this.publicBookingCustomDomainValidationRecordName = '',
    this.publicBookingCustomDomainValidationRecordType = '',
    this.publicBookingCustomDomainValidationRecordValue = '',
    this.publicBookingCustomDomainLastError = '',
    this.marketingSiteDraftSlug = '',
    this.marketingSiteDraftCustomDomain = '',
    this.marketingSiteTitle = 'Sua beleza, do seu jeito',
    this.marketingSiteSupportText =
        'Realce sua essência com cuidados personalizados para você se sentir incrível todos os dias.',
    this.marketingSiteButtonText = 'Agendar agora',
    this.marketingSiteHeroImagePath = '',
    this.marketingSiteAccentColor = '#FF6B4A',
    this.marketingSiteAlignment = 'left',
    this.marketingSiteSpacing = 'compact',
    this.marketingSiteTitleFont = 'Georgia',
    this.marketingSiteImageContrast = 64,
    this.marketingSiteShowButton = true,
    MarketingCatalogHeader? marketingSiteHeader,
    MarketingCatalogFooter? marketingSiteFooter,
    MarketingCatalogDesign? marketingSiteDesign,
    List<MarketingCatalogSection>? marketingSiteSections,
    this.marketingSiteSeoTitle = '',
    this.marketingSiteSeoDescription = '',
    this.marketingSiteBuilderVersion = 0,
    this.marketingSitePublishedAt,
    MarketingSitePromotion? marketingSitePromotion,
    this.publishedMarketingCatalog,
    this.businessSegment = '',
    this.themeId = '',
    this.clientLabel = 'Cliente',
    this.clientDetailLabel = 'Paciente / pet / veículo / preferência',
    this.resourceLabel = 'Sala, box ou cadeira',
    this.onboardingCompleted = true,
    this.workdayStartHour = 8,
    this.workdayEndHour = 20,
    List<int>? workdays,
    this.workdayBreakEnabled = true,
    this.workdayBreakStartHour = 12,
    this.workdayBreakEndHour = 13,
    List<String>? resources,
    this.professionalCountRange = '',
    this.mainObjective = '',
    this.monthlyRevenueGoal = 2000,
    this.postalCode = '',
    this.neighborhood = '',
    this.street = '',
    this.addressNumber = '',
    this.addressComplement = '',
    this.accountPasswordHash = '',
    DateTime? accountCreatedAt,
    this.whatsAppEnabled = true,
    this.whatsAppLinked = false,
    this.whatsAppStorePhone = '',
    this.whatsAppConnectedName = '',
    this.whatsAppLinkedAt,
    this.whatsAppLastMessageAt,
    this.whatsAppAutoConfirmationsEnabled = true,
    this.whatsAppEvolutionBaseUrl =
        'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp',
    this.whatsAppEvolutionApiKey = '',
    this.whatsAppEvolutionInstanceName = 'agenda-livre',
    this.whatsAppEvolutionState = '',
    this.whatsAppEvolutionQrBase64 = '',
    this.whatsAppEvolutionLastCheckedAt,
    this.instagramEnabled = true,
    this.instagramLinked = false,
    this.instagramApiUrl =
        'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram',
    this.instagramUsername = '',
    this.instagramDisplayName = '',
    this.instagramAccountId = '',
    this.instagramState = '',
    this.instagramLastError = '',
    this.instagramLinkedAt,
    this.instagramLastCheckedAt,
    this.mercadoPagoEnabled = false,
    this.mercadoPagoConnected = false,
    this.mercadoPagoLicenseKey = '',
    this.mercadoPagoPaymentsApiUrl =
        'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments',
    this.mercadoPagoSellerUserId = '',
    this.mercadoPagoDefaultTerminalId = '',
    this.mercadoPagoDefaultTerminalLabel = '',
    this.mercadoPagoLastError = '',
    this.mercadoPagoLastSyncAt,
  }) : marketingSiteHeader = marketingSiteHeader ?? MarketingCatalogHeader(),
       marketingSiteFooter = marketingSiteFooter ?? MarketingCatalogFooter(),
       marketingSiteDesign = marketingSiteDesign ?? MarketingCatalogDesign(),
       marketingSiteSections = List<MarketingCatalogSection>.of(
         marketingSiteSections ?? const <MarketingCatalogSection>[],
       ),
       marketingSitePromotion =
           marketingSitePromotion ?? MarketingSitePromotion(),
       workdays = List<int>.of(workdays ?? const <int>[1, 2, 3, 4, 5, 6]),
       resources = List<String>.of(resources ?? const <String>[]),
       accountCreatedAt = accountCreatedAt ?? DateTime(1);

  String accountFullName;
  String accountPhone;
  String accountEmail;
  String businessName;
  String businessDocument;
  String businessPhone;
  String pixKey;
  String businessAddress;
  String businessLogoPath;
  String publicBookingSlug;
  String publicBookingUrl;
  String publicBookingApiUrl;
  DateTime? publicBookingLastSyncAt;
  String publicBookingCustomDomain;
  String publicBookingCustomDomainStatus;
  String publicBookingCustomDomainProviderStatus;
  String publicBookingCustomDomainSslStatus;
  String publicBookingCustomDomainCnameTarget;
  String publicBookingCustomDomainValidationRecordName;
  String publicBookingCustomDomainValidationRecordType;
  String publicBookingCustomDomainValidationRecordValue;
  String publicBookingCustomDomainLastError;
  String marketingSiteDraftSlug;
  String marketingSiteDraftCustomDomain;
  String marketingSiteTitle;
  String marketingSiteSupportText;
  String marketingSiteButtonText;
  String marketingSiteHeroImagePath;
  String marketingSiteAccentColor;
  String marketingSiteAlignment;
  String marketingSiteSpacing;
  String marketingSiteTitleFont;
  double marketingSiteImageContrast;
  bool marketingSiteShowButton;
  MarketingCatalogHeader marketingSiteHeader;
  MarketingCatalogFooter marketingSiteFooter;
  MarketingCatalogDesign marketingSiteDesign;
  List<MarketingCatalogSection> marketingSiteSections;
  String marketingSiteSeoTitle;
  String marketingSiteSeoDescription;
  int marketingSiteBuilderVersion;
  DateTime? marketingSitePublishedAt;
  MarketingSitePromotion marketingSitePromotion;
  MarketingCatalogPublication? publishedMarketingCatalog;
  String businessSegment;
  String themeId;
  String clientLabel;
  String clientDetailLabel;
  String resourceLabel;
  bool onboardingCompleted;
  int workdayStartHour;
  int workdayEndHour;
  List<int> workdays;
  bool workdayBreakEnabled;
  int workdayBreakStartHour;
  int workdayBreakEndHour;
  List<String> resources;
  String professionalCountRange;
  String mainObjective;
  double monthlyRevenueGoal;
  String postalCode;
  String neighborhood;
  String street;
  String addressNumber;
  String addressComplement;
  String accountPasswordHash;
  DateTime accountCreatedAt;
  bool whatsAppEnabled;
  bool whatsAppLinked;
  String whatsAppStorePhone;
  String whatsAppConnectedName;
  DateTime? whatsAppLinkedAt;
  DateTime? whatsAppLastMessageAt;
  bool whatsAppAutoConfirmationsEnabled;
  String whatsAppEvolutionBaseUrl;
  String whatsAppEvolutionApiKey;
  String whatsAppEvolutionInstanceName;
  String whatsAppEvolutionState;
  String whatsAppEvolutionQrBase64;
  DateTime? whatsAppEvolutionLastCheckedAt;
  bool instagramEnabled;
  bool instagramLinked;
  String instagramApiUrl;
  String instagramUsername;
  String instagramDisplayName;
  String instagramAccountId;
  String instagramState;
  String instagramLastError;
  DateTime? instagramLinkedAt;
  DateTime? instagramLastCheckedAt;
  bool mercadoPagoEnabled;
  bool mercadoPagoConnected;
  String mercadoPagoLicenseKey;
  String mercadoPagoPaymentsApiUrl;
  String mercadoPagoSellerUserId;
  String mercadoPagoDefaultTerminalId;
  String mercadoPagoDefaultTerminalLabel;
  String mercadoPagoLastError;
  DateTime? mercadoPagoLastSyncAt;

  factory AgendaSettings.fromJson(JsonMap json) => AgendaSettings(
    accountFullName: jsonString(json, 'AccountFullName'),
    accountPhone: jsonString(json, 'AccountPhone'),
    accountEmail: jsonString(json, 'AccountEmail'),
    businessName: jsonString(json, 'BusinessName', fallback: 'Balcão Livre'),
    businessDocument: jsonString(json, 'BusinessDocument'),
    businessPhone: jsonString(json, 'BusinessPhone'),
    pixKey: jsonString(json, 'PixKey'),
    businessAddress: jsonString(json, 'BusinessAddress'),
    businessLogoPath: jsonString(json, 'BusinessLogoPath'),
    publicBookingSlug: jsonString(json, 'PublicBookingSlug'),
    publicBookingUrl: jsonString(json, 'PublicBookingUrl'),
    publicBookingApiUrl: jsonString(json, 'PublicBookingApiUrl'),
    publicBookingLastSyncAt: jsonNullableDateTime(
      json,
      'PublicBookingLastSyncAt',
    ),
    publicBookingCustomDomain: jsonString(json, 'PublicBookingCustomDomain'),
    publicBookingCustomDomainStatus: jsonString(
      json,
      'PublicBookingCustomDomainStatus',
    ),
    publicBookingCustomDomainProviderStatus: jsonString(
      json,
      'PublicBookingCustomDomainProviderStatus',
    ),
    publicBookingCustomDomainSslStatus: jsonString(
      json,
      'PublicBookingCustomDomainSslStatus',
    ),
    publicBookingCustomDomainCnameTarget: jsonString(
      json,
      'PublicBookingCustomDomainCnameTarget',
    ),
    publicBookingCustomDomainValidationRecordName: jsonString(
      json,
      'PublicBookingCustomDomainValidationRecordName',
    ),
    publicBookingCustomDomainValidationRecordType: jsonString(
      json,
      'PublicBookingCustomDomainValidationRecordType',
    ),
    publicBookingCustomDomainValidationRecordValue: jsonString(
      json,
      'PublicBookingCustomDomainValidationRecordValue',
    ),
    publicBookingCustomDomainLastError: jsonString(
      json,
      'PublicBookingCustomDomainLastError',
    ),
    marketingSiteDraftSlug: jsonString(json, 'MarketingSiteDraftSlug'),
    marketingSiteDraftCustomDomain: jsonString(
      json,
      'MarketingSiteDraftCustomDomain',
    ),
    marketingSiteTitle: jsonString(
      json,
      'MarketingSiteTitle',
      fallback: 'Sua beleza, do seu jeito',
    ),
    marketingSiteSupportText: jsonString(
      json,
      'MarketingSiteSupportText',
      fallback:
          'Realce sua essência com cuidados personalizados para você se sentir incrível todos os dias.',
    ),
    marketingSiteButtonText: jsonString(
      json,
      'MarketingSiteButtonText',
      fallback: 'Agendar agora',
    ),
    marketingSiteHeroImagePath: jsonString(json, 'MarketingSiteHeroImagePath'),
    marketingSiteAccentColor: jsonString(
      json,
      'MarketingSiteAccentColor',
      fallback: '#FF6B4A',
    ),
    marketingSiteAlignment: jsonString(
      json,
      'MarketingSiteAlignment',
      fallback: 'left',
    ),
    marketingSiteSpacing: jsonString(
      json,
      'MarketingSiteSpacing',
      fallback: 'compact',
    ),
    marketingSiteTitleFont: jsonString(
      json,
      'MarketingSiteTitleFont',
      fallback: 'Georgia',
    ),
    marketingSiteImageContrast: jsonDouble(
      json,
      'MarketingSiteImageContrast',
      fallback: 64,
    ),
    marketingSiteShowButton: jsonBool(
      json,
      'MarketingSiteShowButton',
      fallback: true,
    ),
    marketingSiteHeader: MarketingCatalogHeader.fromJson(
      jsonObject(json, 'MarketingSiteHeader'),
    ),
    marketingSiteFooter: MarketingCatalogFooter.fromJson(
      jsonObject(json, 'MarketingSiteFooter'),
    ),
    marketingSiteDesign: MarketingCatalogDesign.fromJson(
      jsonObject(json, 'MarketingSiteDesign'),
    ),
    marketingSiteSections: jsonObjectList(
      json,
      'MarketingSiteSections',
    ).map(MarketingCatalogSection.fromJson).toList(growable: true),
    marketingSiteSeoTitle: jsonString(json, 'MarketingSiteSeoTitle'),
    marketingSiteSeoDescription: jsonString(
      json,
      'MarketingSiteSeoDescription',
    ),
    marketingSiteBuilderVersion: jsonInt(json, 'MarketingSiteBuilderVersion'),
    marketingSitePublishedAt: jsonNullableDateTime(
      json,
      'MarketingSitePublishedAt',
    ),
    marketingSitePromotion: MarketingSitePromotion.fromJson(
      jsonObject(json, 'MarketingSitePromotion'),
    ),
    publishedMarketingCatalog: () {
      final value = jsonObject(json, 'PublishedMarketingCatalog');
      return value.isEmpty ? null : MarketingCatalogPublication.fromJson(value);
    }(),
    businessSegment: jsonString(json, 'BusinessSegment'),
    themeId: jsonString(json, 'ThemeId'),
    clientLabel: jsonString(json, 'ClientLabel', fallback: 'Cliente'),
    clientDetailLabel: jsonString(
      json,
      'ClientDetailLabel',
      fallback: 'Paciente / pet / veículo / preferência',
    ),
    resourceLabel: jsonString(
      json,
      'ResourceLabel',
      fallback: 'Sala, box ou cadeira',
    ),
    onboardingCompleted: jsonBool(json, 'OnboardingCompleted', fallback: true),
    workdayStartHour: jsonInt(json, 'WorkdayStartHour', fallback: 8),
    workdayEndHour: jsonInt(json, 'WorkdayEndHour', fallback: 20),
    workdays: jsonField(json, 'Workdays') == null
        ? const <int>[1, 2, 3, 4, 5, 6]
        : jsonIntList(json, 'Workdays'),
    workdayBreakEnabled: jsonBool(json, 'WorkdayBreakEnabled', fallback: true),
    workdayBreakStartHour: jsonInt(json, 'WorkdayBreakStartHour', fallback: 12),
    workdayBreakEndHour: jsonInt(json, 'WorkdayBreakEndHour', fallback: 13),
    resources: jsonStringList(json, 'Resources'),
    professionalCountRange: jsonString(json, 'ProfessionalCountRange'),
    mainObjective: jsonString(json, 'MainObjective'),
    monthlyRevenueGoal: jsonDouble(json, 'MonthlyRevenueGoal', fallback: 2000),
    postalCode: jsonString(json, 'PostalCode'),
    neighborhood: jsonString(json, 'Neighborhood'),
    street: jsonString(json, 'Street'),
    addressNumber: jsonString(json, 'AddressNumber'),
    addressComplement: jsonString(json, 'AddressComplement'),
    accountPasswordHash: jsonString(json, 'AccountPasswordHash'),
    accountCreatedAt: jsonDateTime(
      json,
      'AccountCreatedAt',
      fallback: DateTime(1),
    ),
    whatsAppEnabled: jsonBool(json, 'WhatsAppEnabled', fallback: true),
    whatsAppLinked: jsonBool(json, 'WhatsAppLinked'),
    whatsAppStorePhone: jsonString(json, 'WhatsAppStorePhone'),
    whatsAppConnectedName: jsonString(json, 'WhatsAppConnectedName'),
    whatsAppLinkedAt: jsonNullableDateTime(json, 'WhatsAppLinkedAt'),
    whatsAppLastMessageAt: jsonNullableDateTime(json, 'WhatsAppLastMessageAt'),
    whatsAppAutoConfirmationsEnabled: jsonBool(
      json,
      'WhatsAppAutoConfirmationsEnabled',
      fallback: true,
    ),
    whatsAppEvolutionBaseUrl: jsonString(
      json,
      'WhatsAppEvolutionBaseUrl',
      fallback:
          'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp',
    ),
    whatsAppEvolutionApiKey: jsonString(json, 'WhatsAppEvolutionApiKey'),
    whatsAppEvolutionInstanceName: jsonString(
      json,
      'WhatsAppEvolutionInstanceName',
      fallback: 'agenda-livre',
    ),
    whatsAppEvolutionState: jsonString(json, 'WhatsAppEvolutionState'),
    whatsAppEvolutionQrBase64: jsonString(json, 'WhatsAppEvolutionQrBase64'),
    whatsAppEvolutionLastCheckedAt: jsonNullableDateTime(
      json,
      'WhatsAppEvolutionLastCheckedAt',
    ),
    instagramEnabled: jsonBool(json, 'InstagramEnabled', fallback: true),
    instagramLinked: jsonBool(json, 'InstagramLinked'),
    instagramApiUrl: jsonString(
      json,
      'InstagramApiUrl',
      fallback:
          'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram',
    ),
    instagramUsername: jsonString(json, 'InstagramUsername'),
    instagramDisplayName: jsonString(json, 'InstagramDisplayName'),
    instagramAccountId: jsonString(json, 'InstagramAccountId'),
    instagramState: jsonString(json, 'InstagramState'),
    instagramLastError: jsonString(json, 'InstagramLastError'),
    instagramLinkedAt: jsonNullableDateTime(json, 'InstagramLinkedAt'),
    instagramLastCheckedAt: jsonNullableDateTime(
      json,
      'InstagramLastCheckedAt',
    ),
    mercadoPagoEnabled: jsonBool(json, 'MercadoPagoEnabled'),
    mercadoPagoConnected: jsonBool(json, 'MercadoPagoConnected'),
    mercadoPagoLicenseKey: jsonString(json, 'MercadoPagoLicenseKey'),
    mercadoPagoPaymentsApiUrl: jsonString(
      json,
      'MercadoPagoPaymentsApiUrl',
      fallback:
          'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments',
    ),
    mercadoPagoSellerUserId: jsonString(json, 'MercadoPagoSellerUserId'),
    mercadoPagoDefaultTerminalId: jsonString(
      json,
      'MercadoPagoDefaultTerminalId',
    ),
    mercadoPagoDefaultTerminalLabel: jsonString(
      json,
      'MercadoPagoDefaultTerminalLabel',
    ),
    mercadoPagoLastError: jsonString(json, 'MercadoPagoLastError'),
    mercadoPagoLastSyncAt: jsonNullableDateTime(json, 'MercadoPagoLastSyncAt'),
  );

  JsonMap toJson() => <String, dynamic>{
    'AccountFullName': accountFullName,
    'AccountPhone': accountPhone,
    'AccountEmail': accountEmail,
    'BusinessName': businessName,
    'BusinessDocument': businessDocument,
    'BusinessPhone': businessPhone,
    'PixKey': pixKey,
    'BusinessAddress': businessAddress,
    'BusinessLogoPath': businessLogoPath,
    'PublicBookingSlug': publicBookingSlug,
    'PublicBookingUrl': publicBookingUrl,
    'PublicBookingApiUrl': publicBookingApiUrl,
    'PublicBookingLastSyncAt': dateTimeToJson(publicBookingLastSyncAt),
    'PublicBookingCustomDomain': publicBookingCustomDomain,
    'PublicBookingCustomDomainStatus': publicBookingCustomDomainStatus,
    'PublicBookingCustomDomainProviderStatus':
        publicBookingCustomDomainProviderStatus,
    'PublicBookingCustomDomainSslStatus': publicBookingCustomDomainSslStatus,
    'PublicBookingCustomDomainCnameTarget':
        publicBookingCustomDomainCnameTarget,
    'PublicBookingCustomDomainValidationRecordName':
        publicBookingCustomDomainValidationRecordName,
    'PublicBookingCustomDomainValidationRecordType':
        publicBookingCustomDomainValidationRecordType,
    'PublicBookingCustomDomainValidationRecordValue':
        publicBookingCustomDomainValidationRecordValue,
    'PublicBookingCustomDomainLastError': publicBookingCustomDomainLastError,
    'MarketingSiteDraftSlug': marketingSiteDraftSlug,
    'MarketingSiteDraftCustomDomain': marketingSiteDraftCustomDomain,
    'MarketingSiteTitle': marketingSiteTitle,
    'MarketingSiteSupportText': marketingSiteSupportText,
    'MarketingSiteButtonText': marketingSiteButtonText,
    'MarketingSiteHeroImagePath': marketingSiteHeroImagePath,
    'MarketingSiteAccentColor': marketingSiteAccentColor,
    'MarketingSiteAlignment': marketingSiteAlignment,
    'MarketingSiteSpacing': marketingSiteSpacing,
    'MarketingSiteTitleFont': marketingSiteTitleFont,
    'MarketingSiteImageContrast': marketingSiteImageContrast,
    'MarketingSiteShowButton': marketingSiteShowButton,
    'MarketingSiteHeader': marketingSiteHeader.toJson(),
    'MarketingSiteFooter': marketingSiteFooter.toJson(),
    'MarketingSiteDesign': marketingSiteDesign.toJson(),
    'MarketingSiteSections': marketingSiteSections
        .map((section) => section.toJson())
        .toList(),
    'MarketingSiteSeoTitle': marketingSiteSeoTitle,
    'MarketingSiteSeoDescription': marketingSiteSeoDescription,
    'MarketingSiteBuilderVersion': marketingSiteBuilderVersion,
    'MarketingSitePublishedAt': dateTimeToJson(marketingSitePublishedAt),
    'MarketingSitePromotion': marketingSitePromotion.toJson(),
    'PublishedMarketingCatalog': publishedMarketingCatalog?.toJson(),
    'BusinessSegment': businessSegment,
    'ThemeId': themeId,
    'ClientLabel': clientLabel,
    'ClientDetailLabel': clientDetailLabel,
    'ResourceLabel': resourceLabel,
    'OnboardingCompleted': onboardingCompleted,
    'WorkdayStartHour': workdayStartHour,
    'WorkdayEndHour': workdayEndHour,
    'Workdays': workdays,
    'WorkdayBreakEnabled': workdayBreakEnabled,
    'WorkdayBreakStartHour': workdayBreakStartHour,
    'WorkdayBreakEndHour': workdayBreakEndHour,
    'Resources': resources,
    'ProfessionalCountRange': professionalCountRange,
    'MainObjective': mainObjective,
    'MonthlyRevenueGoal': monthlyRevenueGoal,
    'PostalCode': postalCode,
    'Neighborhood': neighborhood,
    'Street': street,
    'AddressNumber': addressNumber,
    'AddressComplement': addressComplement,
    'AccountPasswordHash': accountPasswordHash,
    'AccountCreatedAt': accountCreatedAt.toIso8601String(),
    'WhatsAppEnabled': whatsAppEnabled,
    'WhatsAppLinked': whatsAppLinked,
    'WhatsAppStorePhone': whatsAppStorePhone,
    'WhatsAppConnectedName': whatsAppConnectedName,
    'WhatsAppLinkedAt': dateTimeToJson(whatsAppLinkedAt),
    'WhatsAppLastMessageAt': dateTimeToJson(whatsAppLastMessageAt),
    'WhatsAppAutoConfirmationsEnabled': whatsAppAutoConfirmationsEnabled,
    'WhatsAppEvolutionBaseUrl': whatsAppEvolutionBaseUrl,
    'WhatsAppEvolutionApiKey': whatsAppEvolutionApiKey,
    'WhatsAppEvolutionInstanceName': whatsAppEvolutionInstanceName,
    'WhatsAppEvolutionState': whatsAppEvolutionState,
    'WhatsAppEvolutionQrBase64': whatsAppEvolutionQrBase64,
    'WhatsAppEvolutionLastCheckedAt': dateTimeToJson(
      whatsAppEvolutionLastCheckedAt,
    ),
    'InstagramEnabled': instagramEnabled,
    'InstagramLinked': instagramLinked,
    'InstagramApiUrl': instagramApiUrl,
    'InstagramUsername': instagramUsername,
    'InstagramDisplayName': instagramDisplayName,
    'InstagramAccountId': instagramAccountId,
    'InstagramState': instagramState,
    'InstagramLastError': instagramLastError,
    'InstagramLinkedAt': dateTimeToJson(instagramLinkedAt),
    'InstagramLastCheckedAt': dateTimeToJson(instagramLastCheckedAt),
    'MercadoPagoEnabled': mercadoPagoEnabled,
    'MercadoPagoConnected': mercadoPagoConnected,
    'MercadoPagoLicenseKey': mercadoPagoLicenseKey,
    'MercadoPagoPaymentsApiUrl': mercadoPagoPaymentsApiUrl,
    'MercadoPagoSellerUserId': mercadoPagoSellerUserId,
    'MercadoPagoDefaultTerminalId': mercadoPagoDefaultTerminalId,
    'MercadoPagoDefaultTerminalLabel': mercadoPagoDefaultTerminalLabel,
    'MercadoPagoLastError': mercadoPagoLastError,
    'MercadoPagoLastSyncAt': dateTimeToJson(mercadoPagoLastSyncAt),
  };
}
