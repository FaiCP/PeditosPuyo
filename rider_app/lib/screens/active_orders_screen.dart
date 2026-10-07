import 'dart:async';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../models/rider_active_order.dart';
import '../services/api_service.dart';

class ActiveOrdersScreen extends StatefulWidget {
  const ActiveOrdersScreen({super.key});

  @override
  State<ActiveOrdersScreen> createState() => _ActiveOrdersScreenState();
}

class _ActiveOrdersScreenState extends State<ActiveOrdersScreen> {
  final _api = ApiService();
  List<RiderActiveOrder> _orders = [];
  Timer? _poll;
  bool _loading = true;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _refresh();
    _poll = Timer.periodic(const Duration(seconds: 10), (_) => _refresh(silent: true));
  }

  @override
  void dispose() {
    _poll?.cancel();
    super.dispose();
  }

  Future<void> _refresh({bool silent = false}) async {
    try {
      final list = await _api.getList('/rider/orders/active');
      if (!mounted) return;
      setState(() {
        _orders = list.map((j) => RiderActiveOrder.fromJson(j)).toList();
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      if (!silent) _snack('Error cargando pedidos: $e', error: true);
    }
  }

  Future<void> _run(String label, Future<void> Function() action) async {
    setState(() => _busy = true);
    try {
      await action();
      await _refresh(silent: true);
      _snack(label);
    } catch (e) {
      _snack('$e', error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _snack(String msg, {bool error = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(msg),
      backgroundColor: error ? Colors.red : Colors.green.shade700,
    ));
  }

  Future<String?> _askCode(BuildContext context,
      {required int length, required String title}) async {
    final controller = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title, style: const TextStyle(fontSize: 18)),
        content: TextField(
          controller: controller,
          keyboardType: TextInputType.number,
          autofocus: true,
          maxLength: length,
          style: const TextStyle(fontSize: 28, letterSpacing: 8),
          decoration: InputDecoration(counterText: '', hintText: '•' * length),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: const Text('Cancelar')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, controller.text.trim()),
            child: const Text('Verificar'),
          ),
        ],
      ),
    );
  }

  Future<void> _navigate(double lat, double lng, String label) async {
    final uri = Uri.parse('https://www.google.com/maps/dir/?api=1&destination=$lat,$lng');
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      _snack('No se pudo abrir el mapa ($label)', error: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());

    if (_orders.isEmpty) {
      return RefreshIndicator(
        onRefresh: _refresh,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Icon(Icons.delivery_dining, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Center(child: Text('Sin pedidos activos', style: TextStyle(fontSize: 18))),
            SizedBox(height: 8),
            Center(
              child: Text('Cuando aceptes una oferta, aparecerá aquí',
                  style: TextStyle(color: Colors.grey)),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _refresh,
      child: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          for (final o in _orders) _orderCard(o),
        ],
      ),
    );
  }

  Widget _orderCard(RiderActiveOrder o) {
    final order = o.order;
    final status = order.status;

    return Card(
      elevation: 2,
      margin: const EdgeInsets.only(bottom: 14),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Text(order.typeEmoji, style: const TextStyle(fontSize: 26)),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(order.originName,
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ),
                _StatusChip(status: status),
              ],
            ),
            const Divider(),
            _Labeled(
                icon: Icons.storefront,
                label: 'Origen',
                value: order.originAddress,
                trailing: status != 'InTransit'
                    ? IconButton(
                        icon: const Icon(Icons.navigation),
                        color: Colors.blue,
                        onPressed: () =>
                            _navigate(o.originLat, o.originLng, 'origen'),
                      )
                    : null),
            _Labeled(
                icon: Icons.home,
                label: 'Cliente',
                value: order.destinationAddress,
                trailing: status == 'PickedUp' || status == 'InTransit'
                    ? IconButton(
                        icon: const Icon(Icons.navigation),
                        color: Colors.green,
                        onPressed: () =>
                            _navigate(o.destinationLat, o.destinationLng, 'destino'),
                      )
                    : null),
            if (order.description != null && order.description!.isNotEmpty)
              _Labeled(
                  icon: Icons.notes, label: 'Detalle', value: order.description!),
            if (order.items.isNotEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    for (final i in order.items)
                      Text('• ${i.quantity}× ${i.name}',
                          style: const TextStyle(fontSize: 13)),
                  ],
                ),
              ),
            Container(
              padding: const EdgeInsets.all(10),
              margin: const EdgeInsets.only(top: 4, bottom: 8),
              decoration: BoxDecoration(
                color: Colors.amber.shade50,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                order.paymentMethod == 'cash'
                    ? '💵 Cobrar \$${order.totalAmount.toStringAsFixed(2)} al entregar'
                    : '📲 Pago por QR del restaurante — verificar referencias',
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
            ),
            ..._actions(o),
          ],
        ),
      ),
    );
  }

  List<Widget> _actions(RiderActiveOrder o) {
    final status = o.order.status;

    switch (status) {
      case 'RiderAccepted':
        return [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.blue.shade50,
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Text(
              '⏳ Esperando que el restaurante confirme que está listo…',
              style: TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
        ];

      case 'ReadyForPickup':
        return [
          if (o.pickupCode != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(vertical: 12),
              margin: const EdgeInsets.only(bottom: 10),
              decoration: BoxDecoration(
                color: const Color(0xFF0D2318),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  const Text('DILE ESTE CÓDIGO EN EL ORIGEN',
                      style: TextStyle(
                          color: Colors.white70,
                          fontSize: 11,
                          letterSpacing: 1.5,
                          fontWeight: FontWeight.bold)),
                  Text(o.pickupCode!,
                      style: const TextStyle(
                          color: Colors.amberAccent,
                          fontSize: 34,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 6)),
                ],
              ),
            ),
          ElevatedButton.icon(
            onPressed: _busy
                ? null
                : () async {
                    final code = await _askCode(context,
                        length: 6, title: '¿El origen te dio el código de recolección?');
                    if (code == null || code.length != 6) return;
                    await _run('Recogido ✓',
                        () => _api.post('/rider/orders/${o.order.id}/pickup', {'code': code}));
                  },
            icon: const Icon(Icons.qr_code_2),
            label: const Text('Ingresar código y confirmar recojo'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Colors.orange,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16)),
          ),
        ];

      case 'PickedUp':
        return [
          ElevatedButton.icon(
            onPressed: _busy
                ? null
                : () => _run('En camino al cliente',
                    () => _api.postEmpty('/rider/orders/${o.order.id}/in-transit')),
            icon: const Icon(Icons.two_wheeler),
            label: const Text('Salir hacia el destino'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Colors.blue,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16)),
          ),
        ];

      case 'InTransit':
        return [
          ElevatedButton.icon(
            onPressed: _busy
                ? null
                : () async {
                    final code = await _askCode(context,
                        length: 4, title: 'Pídele al cliente su código de entrega');
                    if (code == null || code.length != 4) return;
                    await _run('¡Entregado! 🎉', () => _api
                        .post('/rider/orders/${o.order.id}/deliver', {'code': code}));
                  },
            icon: const Icon(Icons.check_circle),
            label: const Text('Entregar con código del cliente'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Colors.green,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16)),
          ),
        ];

      default:
        return const [];
    }
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  const _StatusChip({required this.status});

  static const _labels = {
    'RiderAccepted': ('Aceptado', Colors.blue),
    'ReadyForPickup': ('Listo en origen', Colors.orange),
    'PickedUp': ('Recogido', Colors.deepOrange),
    'InTransit': ('En camino', Colors.purple),
  };

  @override
  Widget build(BuildContext context) {
    final l = _labels[status] ?? (status, Colors.grey);
    return Chip(
      label: Text(l.$1, style: const TextStyle(color: Colors.white, fontSize: 12)),
      backgroundColor: l.$2,
      padding: const EdgeInsets.symmetric(horizontal: 6),
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
    );
  }
}

class _Labeled extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Widget? trailing;

  const _Labeled({
    required this.icon,
    required this.label,
    required this.value,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: Colors.grey),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label,
                    style: const TextStyle(fontSize: 12, color: Colors.grey)),
                Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
              ],
            ),
          ),
          if (trailing != null) trailing!,
        ],
      ),
    );
  }
}
