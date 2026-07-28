import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/features/onboarding/wpf_onboarding_templates.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('salão cria o mesmo catálogo e os mesmos recursos do WPF', () {
    final data = AgendaData();

    applyWpfOnboardingTemplate(
      data,
      segment: 'Salão de Beleza',
      teamSize: '2 profissionais',
    );

    expect(data.settings.businessSegment, 'Salão de Beleza');
    expect(
      data.settings.clientDetailLabel,
      'Preferência / química / alergia / estilo',
    );
    expect(data.settings.resourceLabel, 'Mesa, cadeira ou lavatório');
    expect(data.settings.workdayStartHour, 9);
    expect(data.settings.workdayEndHour, 20);
    expect(data.settings.workdays, <int>[1, 2, 3, 4, 5, 6]);
    expect(data.settings.workdayBreakStartHour, 12);
    expect(data.settings.workdayBreakEndHour, 13);
    expect(data.settings.resources, <String>[
      'Mesa 1',
      'Mesa 2',
      'Cadeira 1',
      'Cadeira 2',
      'Lavatório',
      'Coloração',
    ]);
    expect(data.services, hasLength(8));
    expect(data.services.map((item) => item.name), contains('Coloração'));
    expect(data.professionals, hasLength(2));
    expect(data.professionals.last.name, 'Designer 1');
  });

  test('clínica, petshop e oficina preservam os contratos do Windows', () {
    final clinic = AgendaData();
    applyWpfOnboardingTemplate(
      clinic,
      segment: 'Clínica médica',
      teamSize: '1 profissional',
    );
    expect(clinic.settings.clientLabel, 'Paciente');
    expect(clinic.settings.resourceLabel, 'Sala ou consultório');
    expect(clinic.services.first.name, 'Consulta médica');
    expect(clinic.professionals.single.role, 'Médico');

    final petshop = AgendaData();
    applyWpfOnboardingTemplate(
      petshop,
      segment: 'Petshop',
      teamSize: '2 profissionais',
    );
    expect(petshop.settings.clientLabel, 'Tutor / pet');
    expect(petshop.services, hasLength(4));
    expect(petshop.professionals.last.name, 'Veterinário 1');

    final workshop = AgendaData();
    applyWpfOnboardingTemplate(
      workshop,
      segment: 'Oficina',
      teamSize: '10 ou mais profissionais',
    );
    expect(workshop.settings.businessSegment, 'Oficina');
    expect(workshop.settings.clientLabel, 'Cliente / veículo');
    expect(workshop.settings.resources, contains('Elevador 1'));
    expect(workshop.professionals.single.name, 'Mecânico 1');
  });

  test('nomes padrão acompanham o segmento escolhido', () {
    expect(
      wpfDefaultBusinessNameForSegment('Centro de Estética'),
      'Meu centro de estética',
    );
    expect(
      wpfDefaultBusinessNameForSegment('Podologia'),
      'Minha clínica de podologia',
    );
    expect(wpfDefaultBusinessNameForSegment('Spa'), 'Meu spa');
    expect(wpfDefaultBusinessNameForSegment('Oficina'), 'Minha oficina');
  });
}
