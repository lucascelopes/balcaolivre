import '../../domain/models/models.dart';

/// Applies the same starter catalogue that the Windows WPF onboarding creates.
///
/// This deliberately mirrors `MainWindow.ApplyOnboardingTemplate`: completing
/// onboarding replaces the starter services and professionals with the template
/// selected for the business segment.
void applyWpfOnboardingTemplate(
  AgendaData data, {
  required String segment,
  required String teamSize,
}) {
  final template = _templateFor(segment);
  final settings = data.settings;
  settings
    ..businessSegment = template.segment
    ..clientLabel = template.clientLabel
    ..clientDetailLabel = template.clientDetailLabel
    ..resourceLabel = template.resourceLabel
    ..workdayStartHour = template.startHour
    ..workdayEndHour = template.endHour
    ..workdays = <int>[1, 2, 3, 4, 5, 6]
    ..workdayBreakEnabled = true
    ..workdayBreakStartHour = 12
    ..workdayBreakEndHour = 13
    ..resources = List<String>.of(template.resources);

  data.services
    ..clear()
    ..addAll(
      template.services.map(
        (service) => ServiceItem(
          segment: template.segment,
          name: service.name,
          durationMinutes: service.durationMinutes,
          price: service.price,
          defaultResource: service.defaultResource,
        ),
      ),
    );

  final professionalLimit = teamSize.trim().startsWith('2') ? 2 : 1;
  data.professionals
    ..clear()
    ..addAll(
      template.professionals
          .take(professionalLimit)
          .map(
            (professional) => Professional(
              name: professional.name,
              role: professional.role,
              segments: <String>[template.segment],
            ),
          ),
    );
}

String wpfDefaultBusinessNameForSegment(String segment) =>
    _templateFor(segment).defaultBusinessName;

_OnboardingTemplate _templateFor(String segment) => switch (segment.trim()) {
  'Salão de Beleza' => _integratedBeauty.copyWith(
    segment: 'Salão de Beleza',
    defaultBusinessName: 'Meu salão de beleza',
  ),
  'Barbearia' => _barber.copyWith(segment: 'Barbearia'),
  'Centro de Estética' => _nails.copyWith(
    segment: 'Centro de Estética',
    defaultBusinessName: 'Meu centro de estética',
  ),
  'Podologia' => _nails.copyWith(
    segment: 'Podologia',
    defaultBusinessName: 'Minha clínica de podologia',
  ),
  'Spa' => _nails.copyWith(segment: 'Spa', defaultBusinessName: 'Meu spa'),
  'Clínica médica' => _medical,
  'Petshop' => _petshop,
  'Mecânica' || 'Oficina' => _workshop.copyWith(segment: 'Oficina'),
  _ => _generic,
};

class _OnboardingTemplate {
  const _OnboardingTemplate({
    required this.segment,
    required this.defaultBusinessName,
    required this.clientLabel,
    required this.clientDetailLabel,
    required this.resourceLabel,
    required this.startHour,
    required this.endHour,
    required this.resources,
    required this.services,
    required this.professionals,
  });

  final String segment;
  final String defaultBusinessName;
  final String clientLabel;
  final String clientDetailLabel;
  final String resourceLabel;
  final int startHour;
  final int endHour;
  final List<String> resources;
  final List<_ServiceTemplate> services;
  final List<_ProfessionalTemplate> professionals;

  _OnboardingTemplate copyWith({
    String? segment,
    String? defaultBusinessName,
  }) => _OnboardingTemplate(
    segment: segment ?? this.segment,
    defaultBusinessName: defaultBusinessName ?? this.defaultBusinessName,
    clientLabel: clientLabel,
    clientDetailLabel: clientDetailLabel,
    resourceLabel: resourceLabel,
    startHour: startHour,
    endHour: endHour,
    resources: resources,
    services: services,
    professionals: professionals,
  );
}

class _ServiceTemplate {
  const _ServiceTemplate(
    this.name,
    this.durationMinutes,
    this.price,
    this.defaultResource,
  );

  final String name;
  final int durationMinutes;
  final double price;
  final String defaultResource;
}

class _ProfessionalTemplate {
  const _ProfessionalTemplate(this.name, this.role);

  final String name;
  final String role;
}

const _integratedBeauty = _OnboardingTemplate(
  segment: 'Salão de Beleza',
  defaultBusinessName: 'Meu salão de beleza',
  clientLabel: 'Cliente',
  clientDetailLabel: 'Preferência / química / alergia / estilo',
  resourceLabel: 'Mesa, cadeira ou lavatório',
  startHour: 9,
  endHour: 20,
  resources: <String>[
    'Mesa 1',
    'Mesa 2',
    'Cadeira 1',
    'Cadeira 2',
    'Lavatório',
    'Coloração',
  ],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Manicure', 45, 55, 'Mesa 1'),
    _ServiceTemplate('Pedicure', 45, 60, 'Mesa 1'),
    _ServiceTemplate('Alongamento de unha', 120, 180, 'Mesa 2'),
    _ServiceTemplate('Sobrancelha', 30, 45, 'Mesa 2'),
    _ServiceTemplate('Escova', 45, 70, 'Cadeira 1'),
    _ServiceTemplate('Corte feminino', 50, 90, 'Cadeira 1'),
    _ServiceTemplate('Coloração', 120, 240, 'Coloração'),
    _ServiceTemplate('Hidratação', 60, 120, 'Lavatório'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Manicure 1', 'Manicure'),
    _ProfessionalTemplate('Designer 1', 'Designer'),
    _ProfessionalTemplate('Cabeleireiro 1', 'Cabeleireiro'),
    _ProfessionalTemplate('Colorista 1', 'Colorista'),
  ],
);

const _nails = _OnboardingTemplate(
  segment: 'Unha e beleza',
  defaultBusinessName: 'Meu studio de beleza',
  clientLabel: 'Cliente',
  clientDetailLabel: 'Preferência / alergia / estilo',
  resourceLabel: 'Mesa ou cadeira',
  startHour: 9,
  endHour: 20,
  resources: <String>['Mesa 1', 'Mesa 2', 'Cadeira beleza'],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Manicure', 45, 55, 'Mesa 1'),
    _ServiceTemplate('Pedicure', 45, 60, 'Mesa 1'),
    _ServiceTemplate('Alongamento de unha', 120, 180, 'Mesa 2'),
    _ServiceTemplate('Sobrancelha', 30, 45, 'Cadeira beleza'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Manicure 1', 'Manicure'),
    _ProfessionalTemplate('Designer 1', 'Designer'),
  ],
);

const _barber = _OnboardingTemplate(
  segment: 'Barbearia',
  defaultBusinessName: 'Minha barbearia',
  clientLabel: 'Cliente',
  clientDetailLabel: 'Estilo / preferência / observação',
  resourceLabel: 'Cadeira',
  startHour: 9,
  endHour: 20,
  resources: <String>['Cadeira 1', 'Cadeira 2', 'Lavatorio'],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Corte masculino', 35, 45, 'Cadeira 1'),
    _ServiceTemplate('Barba', 25, 35, 'Cadeira 1'),
    _ServiceTemplate('Corte + barba', 60, 80, 'Cadeira 1'),
    _ServiceTemplate('Sobrancelha', 15, 20, 'Cadeira 2'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Barbeiro 1', 'Barbeiro'),
    _ProfessionalTemplate('Barbeiro 2', 'Barbeiro'),
  ],
);

const _medical = _OnboardingTemplate(
  segment: 'Clínica médica',
  defaultBusinessName: 'Minha clínica',
  clientLabel: 'Paciente',
  clientDetailLabel: 'Prontuário / convênio / motivo',
  resourceLabel: 'Sala ou consultório',
  startHour: 8,
  endHour: 18,
  resources: <String>['Consultório 1', 'Consultório 2', 'Sala de exames'],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Consulta médica', 45, 180, 'Consultório 1'),
    _ServiceTemplate('Retorno', 30, 90, 'Consultório 1'),
    _ServiceTemplate('Exame simples', 30, 120, 'Sala de exames'),
    _ServiceTemplate('Encaixe', 20, 80, 'Consultório 2'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Profissional 1', 'Médico'),
    _ProfessionalTemplate('Profissional 2', 'Médico'),
  ],
);

const _petshop = _OnboardingTemplate(
  segment: 'Petshop',
  defaultBusinessName: 'Meu petshop',
  clientLabel: 'Tutor / pet',
  clientDetailLabel: 'Raça / porte / observação do pet',
  resourceLabel: 'Sala, baia ou mesa',
  startHour: 8,
  endHour: 19,
  resources: <String>[
    'Banho 1',
    'Tosa 1',
    'Sala veterinária',
    'Baia de espera',
  ],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Banho', 60, 70, 'Banho 1'),
    _ServiceTemplate('Banho e tosa', 90, 110, 'Tosa 1'),
    _ServiceTemplate('Consulta veterinária', 40, 160, 'Sala veterinária'),
    _ServiceTemplate('Vacinação', 25, 85, 'Sala veterinária'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Tosador 1', 'Banho e tosa'),
    _ProfessionalTemplate('Veterinário 1', 'Veterinário'),
  ],
);

const _workshop = _OnboardingTemplate(
  segment: 'Oficina',
  defaultBusinessName: 'Minha oficina',
  clientLabel: 'Cliente / veículo',
  clientDetailLabel: 'Placa / modelo / problema',
  resourceLabel: 'Box ou elevador',
  startHour: 8,
  endHour: 18,
  resources: <String>['Box 1', 'Box 2', 'Elevador 1', 'Diagnóstico'],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Diagnóstico', 60, 120, 'Diagnóstico'),
    _ServiceTemplate('Troca de óleo', 45, 90, 'Box 1'),
    _ServiceTemplate('Revisão completa', 150, 420, 'Box 2'),
    _ServiceTemplate('Alinhamento', 50, 130, 'Elevador 1'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Mecânico 1', 'Mecânico'),
    _ProfessionalTemplate('Consultor técnico', 'Recepção técnica'),
  ],
);

const _generic = _OnboardingTemplate(
  segment: 'Outro segmento',
  defaultBusinessName: 'Meu negócio',
  clientLabel: 'Cliente',
  clientDetailLabel: 'Observação / preferência / motivo',
  resourceLabel: 'Sala ou local',
  startHour: 8,
  endHour: 18,
  resources: <String>['Sala 1', 'Sala 2', 'Atendimento 1'],
  services: <_ServiceTemplate>[
    _ServiceTemplate('Atendimento', 30, 0, 'Sala 1'),
    _ServiceTemplate('Retorno', 30, 0, 'Sala 1'),
    _ServiceTemplate('Encaixe', 20, 0, 'Atendimento 1'),
  ],
  professionals: <_ProfessionalTemplate>[
    _ProfessionalTemplate('Profissional 1', 'Atendimento'),
    _ProfessionalTemplate('Profissional 2', 'Atendimento'),
  ],
);
