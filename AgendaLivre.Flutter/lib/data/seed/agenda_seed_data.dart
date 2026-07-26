import '../../domain/models/models.dart';

abstract final class AgendaSeedData {
  static AgendaData salon({DateTime? referenceDate}) {
    final now = referenceDate ?? DateTime.now();

    final services = <ServiceItem>[
      ServiceItem(
        id: 'service-manicure',
        segment: 'Centro de Estética',
        name: 'Manicure',
        category: 'Unhas',
        description: 'Cuidado completo das unhas das mãos.',
        durationMinutes: 45,
        price: 55,
        defaultResource: 'Mesa 1',
      ),
      ServiceItem(
        id: 'service-pedicure',
        segment: 'Centro de Estética',
        name: 'Pedicure',
        category: 'Unhas',
        description: 'Cuidado completo das unhas dos pés.',
        durationMinutes: 45,
        price: 60,
        defaultResource: 'Mesa 1',
      ),
      ServiceItem(
        id: 'service-alongamento',
        segment: 'Centro de Estética',
        name: 'Alongamento de unha',
        category: 'Unhas',
        description: 'Aplicação e acabamento de alongamento.',
        durationMinutes: 120,
        price: 180,
        defaultResource: 'Mesa 2',
      ),
      ServiceItem(
        id: 'service-sobrancelha',
        segment: 'Centro de Estética',
        name: 'Sobrancelha',
        category: 'Design',
        description: 'Design de sobrancelha personalizado.',
        durationMinutes: 30,
        price: 45,
        defaultResource: 'Cadeira beleza',
      ),
    ];

    final professionals = <Professional>[
      Professional(
        id: 'professional-manicure-1',
        name: 'Manicure 1',
        role: 'Manicure',
        segments: const <String>['Centro de Estética'],
      ),
      Professional(
        id: 'professional-designer-1',
        name: 'Designer 1',
        role: 'Designer',
        segments: const <String>['Centro de Estética'],
      ),
    ];

    return AgendaData(
      settings: AgendaSettings(
        accountFullName: 'Lucas Cesar Lopes',
        accountPhone: '(33) 99800-7983',
        businessName: 'Lucas Barbearia',
        businessPhone: '(33) 99800-7983',
        businessAddress: 'Rua Piracicaba, Lourdes',
        businessSegment: 'Centro de Estética',
        themeId: 'aesthetic-coral',
        clientLabel: 'Cliente',
        clientDetailLabel: 'Preferência ou observação',
        resourceLabel: 'Mesa ou cadeira',
        onboardingCompleted: true,
        workdayStartHour: 9,
        workdayEndHour: 20,
        resources: const <String>['Cadeira beleza', 'Mesa 1', 'Mesa 2'],
        professionalCountRange: '2 profissionais',
        mainObjective: 'Implementar agendamento online',
        postalCode: '35032390',
        neighborhood: 'Lourdes',
        street: 'Rua Piracicaba',
        accountCreatedAt: now.subtract(const Duration(days: 30)),
      ),
      services: services,
      professionals: professionals,
      manualPayments: <ManualPayment>[
        ManualPayment(
          id: 'payment-opening-history',
          description: 'Pagamento avulso',
          category: 'Agendamento',
          paymentMethod: 'Pix',
          value: 45,
          paidAt: now.subtract(const Duration(days: 25)),
        ),
      ],
    );
  }
}
