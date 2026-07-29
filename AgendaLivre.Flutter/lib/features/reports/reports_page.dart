import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import 'reports_wpf_page.dart';

class ReportsPage extends StatelessWidget {
  const ReportsPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  Widget build(BuildContext context) => WpfReportsPage(controller: controller);
}
