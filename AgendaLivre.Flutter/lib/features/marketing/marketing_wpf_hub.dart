import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

import '../../app/theme/agenda_theme.dart';
import '../../domain/models/marketing_catalog_publication.dart';

class MarketingWpfHub extends StatefulWidget {
  const MarketingWpfHub({
    super.key,
    required this.catalog,
    required this.onStory,
    required this.onPost,
    required this.onWhatsApp,
    required this.onNewCustomers,
    required this.onDiscount,
    required this.onEditCatalog,
  });

  final MarketingCatalogPublication? catalog;
  final VoidCallback onStory;
  final VoidCallback onPost;
  final VoidCallback onWhatsApp;
  final VoidCallback onNewCustomers;
  final VoidCallback onDiscount;
  final VoidCallback onEditCatalog;

  @override
  State<MarketingWpfHub> createState() => _MarketingWpfHubState();
}

class _MarketingWpfHubState extends State<MarketingWpfHub> {
  String _filter = 'all';

  bool get _showCatalog =>
      widget.catalog != null && (_filter == 'all' || _filter == 'site');

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final desktop = MediaQuery.sizeOf(context).width >= 930;

    return ColoredBox(
      color: const Color(0xFFFAF9F7),
      child: SingleChildScrollView(
        key: const Key('marketing-hub-scroll'),
        padding: EdgeInsets.fromLTRB(
          desktop ? 18 : 14,
          desktop ? 16 : 14,
          desktop ? 18 : 14,
          72,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _heading(t),
            const SizedBox(height: 18),
            if (desktop)
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: _campaigns(t, desktop: true)),
                  const SizedBox(width: 18),
                  SizedBox(width: 316, child: _actions(t)),
                ],
              )
            else ...[
              _actions(t),
              const SizedBox(height: 18),
              _campaigns(t, desktop: false),
            ],
          ],
        ),
      ),
    );
  }

  Widget _heading(AgendaThemeTokens t) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Row(
        children: [
          Text(
            'MARKETING',
            style: TextStyle(
              color: t.accent,
              fontSize: 10,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(width: 12),
          Container(width: 44, height: 1, color: t.accent),
        ],
      ),
      const SizedBox(height: 4),
      Text(
        'Marketing',
        style: TextStyle(
          color: t.ink,
          fontSize: 28,
          fontWeight: FontWeight.w800,
        ),
      ),
      const SizedBox(height: 3),
      Text(
        'Crie campanhas, publique conteúdos e acompanhe os resultados.',
        style: TextStyle(color: t.muted, fontSize: 12.5),
      ),
    ],
  );

  Widget _campaigns(AgendaThemeTokens t, {required bool desktop}) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Text(
        'Suas campanhas',
        style: TextStyle(
          color: t.ink,
          fontSize: 21,
          fontWeight: FontWeight.w800,
        ),
      ),
      const SizedBox(height: 12),
      SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            _filterButton(t, 'all', 'Todas'),
            _filterButton(t, 'image', 'Imagem'),
            _filterButton(t, 'text', 'Texto'),
            _filterButton(t, 'site', 'Catálogo'),
          ],
        ),
      ),
      const SizedBox(height: 13),
      if (desktop) _tableHeader(t),
      ConstrainedBox(
        constraints: BoxConstraints(minHeight: desktop ? 352 : 260),
        child: _showCatalog ? _catalogRow(t, desktop: desktop) : _emptyState(t),
      ),
    ],
  );

  Widget _filterButton(AgendaThemeTokens t, String value, String label) {
    final selected = _filter == value;
    return Padding(
      padding: const EdgeInsets.only(right: 7),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => setState(() => _filter = value),
        showCheckmark: false,
        visualDensity: VisualDensity.compact,
        labelStyle: TextStyle(
          color: t.ink,
          fontSize: 11,
          fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
        ),
        backgroundColor: Colors.transparent,
        selectedColor: t.accentSoft,
        side: BorderSide(color: selected ? t.accent : t.line),
        shape: const StadiumBorder(),
      ),
    );
  }

  Widget _tableHeader(AgendaThemeTokens t) => Container(
    height: 38,
    decoration: BoxDecoration(
      border: Border(bottom: BorderSide(color: t.line)),
    ),
    child: Row(
      children: [
        Expanded(flex: 345, child: _headerText(t, 'Campanha', left: 12)),
        Expanded(flex: 115, child: _headerText(t, 'Canal')),
        Expanded(flex: 120, child: _headerText(t, 'Último envio')),
        Expanded(flex: 105, child: _headerText(t, 'Status')),
        Expanded(flex: 105, child: _headerText(t, 'Resultados')),
        const SizedBox(width: 30),
      ],
    ),
  );

  Widget _headerText(AgendaThemeTokens t, String value, {double left = 0}) =>
      Padding(
        padding: EdgeInsets.only(left: left),
        child: Text(value, style: TextStyle(color: t.muted, fontSize: 10.5)),
      );

  Widget _emptyState(AgendaThemeTokens t) => Center(
    child: Container(
      key: const Key('marketing-hub-empty'),
      constraints: const BoxConstraints(maxWidth: 470),
      padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 25),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF8F4),
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              color: t.accentSoft,
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.campaign_outlined, color: t.accent, size: 25),
          ),
          const SizedBox(height: 13),
          Text(
            _filter == 'all'
                ? 'Nenhuma campanha criada ainda'
                : 'Nenhuma campanha neste filtro',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 5),
          Text(
            'Crie sua primeira campanha para começar a acompanhar envios e resultados.',
            textAlign: TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 11.5, height: 1.45),
          ),
          const SizedBox(height: 17),
          FilledButton.icon(
            key: const Key('marketing-hub-empty-action'),
            onPressed: widget.onStory,
            icon: const Icon(Icons.add_circle_outline, size: 18),
            label: const Text('Criar primeira campanha'),
          ),
        ],
      ),
    ),
  );

  Widget _catalogRow(AgendaThemeTokens t, {required bool desktop}) {
    final catalog = widget.catalog!;
    final date = catalog.publishedAt;
    final dateText = date == null
        ? 'Não publicado'
        : '${_two(date.day)}/${_two(date.month)}/${date.year}';

    if (!desktop) {
      return InkWell(
        key: const Key('marketing-hub-catalog-row'),
        borderRadius: BorderRadius.circular(14),
        onTap: widget.onEditCatalog,
        child: Container(
          margin: const EdgeInsets.only(top: 12),
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Row(
            children: [
              _catalogImage(),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      catalog.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      'Catálogo · $dateText',
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, color: t.muted),
            ],
          ),
        ),
      );
    }

    return InkWell(
      key: const Key('marketing-hub-catalog-row'),
      onTap: widget.onEditCatalog,
      child: SizedBox(
        height: 88,
        child: Row(
          children: [
            Expanded(
              flex: 345,
              child: Row(
                children: [
                  _catalogImage(),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      catalog.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              flex: 115,
              child: Row(
                children: [
                  Icon(Icons.public, color: t.ink, size: 19),
                  const SizedBox(width: 7),
                  const Text('Catálogo', style: TextStyle(fontSize: 11)),
                ],
              ),
            ),
            Expanded(
              flex: 120,
              child: Text(dateText, style: const TextStyle(fontSize: 10.5)),
            ),
            Expanded(
              flex: 105,
              child: Row(
                children: [
                  Container(
                    width: 7,
                    height: 7,
                    decoration: const BoxDecoration(
                      color: Color(0xFF16A34A),
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 7),
                  const Text('Enviada', style: TextStyle(fontSize: 10.5)),
                ],
              ),
            ),
            const Expanded(
              flex: 105,
              child: Text('Catálogo ativo', style: TextStyle(fontSize: 10)),
            ),
            SizedBox(
              width: 30,
              child: Icon(Icons.chevron_right, color: t.muted),
            ),
          ],
        ),
      ),
    );
  }

  Widget _catalogImage() => ClipRRect(
    borderRadius: BorderRadius.circular(8),
    child: Image.asset(
      'assets/branding/marketing-campaign-spa.png',
      width: 108,
      height: 68,
      fit: BoxFit.cover,
    ),
  );

  Widget _actions(AgendaThemeTokens t) => Container(
    key: const Key('marketing-hub-actions'),
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: Colors.white,
      border: Border.all(color: t.line),
      borderRadius: BorderRadius.circular(16),
      boxShadow: const [
        BoxShadow(
          color: Color(0x0D000000),
          blurRadius: 18,
          offset: Offset(0, 6),
        ),
      ],
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Criar campanha',
          style: TextStyle(
            color: t.ink,
            fontSize: 18,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 14),
        _action(
          t,
          key: const Key('marketing-hub-story'),
          icon: Icons.add_circle_outline,
          label: 'Story',
          onTap: widget.onStory,
          accent: true,
        ),
        _action(
          t,
          key: const Key('marketing-hub-post'),
          iconWidget: const FaIcon(
            FontAwesomeIcons.instagram,
            color: Color(0xFFE1306C),
            size: 19,
          ),
          label: 'Post',
          onTap: widget.onPost,
        ),
        _action(
          t,
          key: const Key('marketing-hub-whatsapp'),
          iconWidget: const FaIcon(
            FontAwesomeIcons.whatsapp,
            color: Color(0xFF15803D),
            size: 19,
          ),
          label: 'WhatsApp',
          onTap: widget.onWhatsApp,
        ),
        Divider(height: 21, color: t.line),
        _action(
          t,
          key: const Key('marketing-hub-new-customers'),
          icon: Icons.message_outlined,
          label: 'Novos clientes',
          onTap: widget.onNewCustomers,
        ),
        _action(
          t,
          icon: Icons.groups_outlined,
          label: 'Todos os clientes',
          onTap: null,
          trailing: Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: const Color(0xFFEEECEA),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              'Em breve',
              style: TextStyle(color: t.muted, fontSize: 9),
            ),
          ),
        ),
        Divider(height: 21, color: t.line),
        _action(
          t,
          key: const Key('marketing-hub-discount'),
          icon: Icons.local_offer_outlined,
          label: 'Criar desconto',
          onTap: widget.onDiscount,
        ),
        _action(
          t,
          key: const Key('marketing-hub-edit-catalog'),
          icon: Icons.public,
          label: 'Editar catálogo',
          onTap: widget.onEditCatalog,
        ),
        Divider(height: 21, color: t.line),
        SizedBox(
          height: 42,
          child: FilledButton.icon(
            key: const Key('marketing-hub-create'),
            onPressed: widget.onStory,
            icon: const Icon(Icons.add_circle_outline, size: 19),
            label: const Text('Criar campanha'),
          ),
        ),
      ],
    ),
  );

  Widget _action(
    AgendaThemeTokens t, {
    Key? key,
    IconData? icon,
    Widget? iconWidget,
    required String label,
    required VoidCallback? onTap,
    Widget? trailing,
    bool accent = false,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 6),
    child: SizedBox(
      height: 42,
      child: OutlinedButton(
        key: key,
        onPressed: onTap,
        style: OutlinedButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          side: BorderSide(color: accent ? t.accent : t.line),
          foregroundColor: t.ink,
          disabledForegroundColor: t.muted,
        ),
        child: Row(
          children: [
            iconWidget ?? Icon(icon, size: 21),
            const SizedBox(width: 11),
            Expanded(
              child: Text(
                label,
                style: const TextStyle(fontSize: 13),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            trailing ?? const Icon(Icons.chevron_right, size: 18),
          ],
        ),
      ),
    ),
  );

  String _two(int value) => value.toString().padLeft(2, '0');
}
