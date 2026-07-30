import 'package:flutter/material.dart';

import '../domain/models/agenda_settings.dart';

/// Operational language used across the app for the selected business type.
///
/// The onboarding already creates a segment-specific starter catalogue. This
/// profile keeps the rest of the product consistent with that choice instead
/// of showing beauty-salon language to clinics, pet shops, or workshops.
@immutable
class AgendaBusinessProfile {
  const AgendaBusinessProfile({
    required this.segment,
    required this.icon,
    required this.customerSingular,
    required this.customerPlural,
    required this.activitySingular,
    required this.activityPlural,
    required this.newActivityLabel,
    required this.nextActivityLabel,
    required this.serviceSingular,
    required this.servicePlural,
    required this.professionalSingular,
    required this.professionalPlural,
    required this.waitingStage,
    required this.activeStage,
    required this.paymentStage,
    required this.emptyDayTitle,
    required this.emptyDayMessage,
    required this.completedRevenueLabel,
    required this.marketingReturnTitle,
    required this.marketingReturnDetail,
    required this.defaultPromotionOffer,
  });

  final String segment;
  final IconData icon;
  final String customerSingular;
  final String customerPlural;
  final String activitySingular;
  final String activityPlural;
  final String newActivityLabel;
  final String nextActivityLabel;
  final String serviceSingular;
  final String servicePlural;
  final String professionalSingular;
  final String professionalPlural;
  final String waitingStage;
  final String activeStage;
  final String paymentStage;
  final String emptyDayTitle;
  final String emptyDayMessage;
  final String completedRevenueLabel;
  final String marketingReturnTitle;
  final String marketingReturnDetail;
  final String defaultPromotionOffer;

  String activityCount(int count) =>
      count == 1 ? '1 $activitySingular' : '$count $activityPlural';

  String customerCount(int count) =>
      count == 1 ? '1 $customerSingular' : '$count $customerPlural';

  String serviceCount(int count) =>
      count == 1 ? '1 $serviceSingular' : '$count $servicePlural';

  static AgendaBusinessProfile fromSettings(AgendaSettings settings) {
    final normalized = _normalize(settings.businessSegment);
    if (normalized.contains('oficina') || normalized.contains('mecanica')) {
      return const AgendaBusinessProfile(
        segment: 'Oficina',
        icon: Icons.car_repair_rounded,
        customerSingular: 'cliente / veículo',
        customerPlural: 'clientes e veículos',
        activitySingular: 'ordem de serviço',
        activityPlural: 'ordens de serviço',
        newActivityLabel: 'Nova ordem de serviço',
        nextActivityLabel: 'Próxima ordem de serviço',
        serviceSingular: 'serviço',
        servicePlural: 'serviços',
        professionalSingular: 'mecânico / técnico',
        professionalPlural: 'mecânicos e técnicos',
        waitingStage: 'Aguardando veículo',
        activeStage: 'Em execução',
        paymentStage: 'Pronto para faturar',
        emptyDayTitle: 'Nenhuma ordem para hoje',
        emptyDayMessage:
            'Abra uma ordem, registre o veículo e reserve o box ou elevador.',
        completedRevenueLabel: 'Ordens finalizadas',
        marketingReturnTitle: 'Revisão preventiva',
        marketingReturnDetail:
            'Chame veículos sem retorno e antecipe a próxima manutenção.',
        defaultPromotionOffer:
            'Check-up preventivo com condição especial nesta semana',
      );
    }
    if (normalized.contains('clinica') || normalized.contains('medic')) {
      return const AgendaBusinessProfile(
        segment: 'Clínica médica',
        icon: Icons.medical_services_rounded,
        customerSingular: 'paciente',
        customerPlural: 'pacientes',
        activitySingular: 'consulta',
        activityPlural: 'consultas',
        newActivityLabel: 'Nova consulta',
        nextActivityLabel: 'Próxima consulta',
        serviceSingular: 'procedimento',
        servicePlural: 'procedimentos',
        professionalSingular: 'profissional',
        professionalPlural: 'profissionais',
        waitingStage: 'Aguardando paciente',
        activeStage: 'Em consulta',
        paymentStage: 'Pronto para receber',
        emptyDayTitle: 'Nenhuma consulta para hoje',
        emptyDayMessage:
            'Agende uma consulta ou reserve um horário para encaixe.',
        completedRevenueLabel: 'Consultas realizadas',
        marketingReturnTitle: 'Acompanhamento de retorno',
        marketingReturnDetail:
            'Lembre pacientes sobre retornos e cuidados já programados.',
        defaultPromotionOffer:
            'Agenda aberta para avaliações e retornos nesta semana',
      );
    }
    if (normalized.contains('pet')) {
      return const AgendaBusinessProfile(
        segment: 'Petshop',
        icon: Icons.pets_rounded,
        customerSingular: 'tutor / pet',
        customerPlural: 'tutores e pets',
        activitySingular: 'atendimento pet',
        activityPlural: 'atendimentos pet',
        newActivityLabel: 'Novo atendimento pet',
        nextActivityLabel: 'Próximo pet',
        serviceSingular: 'serviço',
        servicePlural: 'serviços',
        professionalSingular: 'profissional',
        professionalPlural: 'profissionais',
        waitingStage: 'Aguardando pet',
        activeStage: 'Em atendimento',
        paymentStage: 'Pronto para receber',
        emptyDayTitle: 'Nenhum pet agendado hoje',
        emptyDayMessage:
            'Agende banho, tosa, consulta ou vacinação e reserve o espaço.',
        completedRevenueLabel: 'Atendimentos pet finalizados',
        marketingReturnTitle: 'Hora do próximo cuidado',
        marketingReturnDetail:
            'Lembre tutores sobre banho, tosa, vacina ou retorno.',
        defaultPromotionOffer: 'Cuidado especial para o pet nesta semana',
      );
    }
    if (normalized.contains('barbearia')) {
      return const AgendaBusinessProfile(
        segment: 'Barbearia',
        icon: Icons.content_cut_rounded,
        customerSingular: 'cliente',
        customerPlural: 'clientes',
        activitySingular: 'atendimento',
        activityPlural: 'atendimentos',
        newActivityLabel: 'Novo atendimento',
        nextActivityLabel: 'Próximo cliente',
        serviceSingular: 'serviço',
        servicePlural: 'serviços',
        professionalSingular: 'barbeiro',
        professionalPlural: 'barbeiros',
        waitingStage: 'Aguardando cliente',
        activeStage: 'Em atendimento',
        paymentStage: 'Pronto para receber',
        emptyDayTitle: 'Nenhum cliente agendado hoje',
        emptyDayMessage:
            'Crie um atendimento ou use um horário livre para encaixe.',
        completedRevenueLabel: 'Atendimentos finalizados',
        marketingReturnTitle: 'Hora de voltar',
        marketingReturnDetail:
            'Convide clientes no período certo para renovar corte ou barba.',
        defaultPromotionOffer:
            'Corte e barba com condição especial nesta semana',
      );
    }
    if (_isBeautyOrWellness(normalized)) {
      return AgendaBusinessProfile(
        segment: settings.businessSegment.trim().isEmpty
            ? 'Beleza e bem-estar'
            : settings.businessSegment.trim(),
        icon: normalized.contains('podolog')
            ? Icons.accessibility_new_rounded
            : normalized.contains('spa')
            ? Icons.spa_rounded
            : Icons.auto_awesome_rounded,
        customerSingular: 'cliente',
        customerPlural: 'clientes',
        activitySingular: 'atendimento',
        activityPlural: 'atendimentos',
        newActivityLabel: 'Novo atendimento',
        nextActivityLabel: 'Próximo atendimento',
        serviceSingular: 'serviço',
        servicePlural: 'serviços',
        professionalSingular: 'profissional',
        professionalPlural: 'profissionais',
        waitingStage: 'Aguardando cliente',
        activeStage: 'Em atendimento',
        paymentStage: 'Pronto para receber',
        emptyDayTitle: 'Nenhum atendimento hoje',
        emptyDayMessage:
            'Crie um atendimento ou use um horário livre para encaixe.',
        completedRevenueLabel: 'Atendimentos finalizados',
        marketingReturnTitle: 'Volta para agenda',
        marketingReturnDetail:
            'Convide clientes sem retorno para reservar o próximo cuidado.',
        defaultPromotionOffer:
            'Condição especial em serviços selecionados nesta semana',
      );
    }
    return const AgendaBusinessProfile(
      segment: 'Serviços',
      icon: Icons.storefront_rounded,
      customerSingular: 'cliente',
      customerPlural: 'clientes',
      activitySingular: 'atendimento',
      activityPlural: 'atendimentos',
      newActivityLabel: 'Novo atendimento',
      nextActivityLabel: 'Próximo atendimento',
      serviceSingular: 'serviço',
      servicePlural: 'serviços',
      professionalSingular: 'profissional',
      professionalPlural: 'profissionais',
      waitingStage: 'Aguardando chegada',
      activeStage: 'Em atendimento',
      paymentStage: 'Pronto para receber',
      emptyDayTitle: 'Nenhum atendimento hoje',
      emptyDayMessage:
          'Crie um atendimento ou use um horário livre para encaixe.',
      completedRevenueLabel: 'Atendimentos finalizados',
      marketingReturnTitle: 'Hora de voltar',
      marketingReturnDetail:
          'Convide clientes sem retorno para fazer um novo agendamento.',
      defaultPromotionOffer:
          'Condição especial em serviços selecionados nesta semana',
    );
  }
}

bool _isBeautyOrWellness(String value) =>
    value.contains('salao') ||
    value.contains('beleza') ||
    value.contains('estet') ||
    value.contains('esmalteria') ||
    value.contains('podolog') ||
    value.contains('spa');

String _normalize(String value) => value
    .trim()
    .toLowerCase()
    .replaceAll(RegExp('[áàâãä]'), 'a')
    .replaceAll(RegExp('[éèêë]'), 'e')
    .replaceAll(RegExp('[íìîï]'), 'i')
    .replaceAll(RegExp('[óòôõö]'), 'o')
    .replaceAll(RegExp('[úùûü]'), 'u')
    .replaceAll('ç', 'c');
