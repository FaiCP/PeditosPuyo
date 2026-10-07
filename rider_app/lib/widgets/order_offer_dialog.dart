import 'dart:async';
import 'package:flutter/material.dart';
import 'package:vibration/vibration.dart';
import '../models/rider_offer.dart';

class OrderOfferDialog extends StatefulWidget {
  final RiderOffer offer;

  const OrderOfferDialog({super.key, required this.offer});

  @override
  State<OrderOfferDialog> createState() => _OrderOfferDialogState();
}

class _OrderOfferDialogState extends State<OrderOfferDialog> {
  late int _secondsRemaining;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _secondsRemaining =
        widget.offer.expiresAt.difference(DateTime.now().toUtc()).inSeconds.clamp(0, 300);
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) return;
      setState(() => _secondsRemaining--);
      if (_secondsRemaining <= 0) {
        t.cancel();
        Navigator.of(context).pop('expired');
      }
    });
    _vibrate();
  }

  Future<void> _vibrate() async {
    try {
      if (await Vibration.hasVibrator()) {
        Vibration.vibrate(pattern: [0, 500, 300, 500], repeat: 1);
      }
    } catch (e) {
      debugPrint('Vibration error: $e');
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final offer = widget.offer;
    final urgent = _secondsRemaining <= 20;

    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      title: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text('${offer.typeEmoji} ${offer.typeLabel}'),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: urgent ? Colors.red : Colors.orange,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              '${_secondsRemaining}s',
              style: const TextStyle(
                  color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16),
            ),
          ),
        ],
      ),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Row(
                icon: Icons.storefront,
                label: 'Origen',
                value: '${offer.originName} — ${offer.originAddress}'),
            const SizedBox(height: 10),
            _Row(
                icon: Icons.home, label: 'Destino', value: offer.destinationAddress),
            if (offer.description != null && offer.description!.isNotEmpty) ...[
              const SizedBox(height: 10),
              _Row(icon: Icons.notes, label: 'Detalle', value: offer.description!),
            ],
            if (offer.items.isNotEmpty) ...[
              const SizedBox(height: 12),
              const Text('Artículos:',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              ...offer.items.map((i) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2),
                    child: Text('• ${i.quantity}× ${i.name}',
                        style: const TextStyle(fontSize: 13)),
                  )),
              if (offer.type == 'compra')
                Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(
                    'Compra estimada: \$${offer.productsAmount.toStringAsFixed(2)} (lleva efectivo)',
                    style: const TextStyle(
                        fontSize: 13, fontWeight: FontWeight.w600, color: Colors.brown),
                  ),
                ),
            ],
            const SizedBox(height: 12),
            Text(
              'Tarifa de delivery: \$${offer.deliveryFeeAmount.toStringAsFixed(2)}',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            LinearProgressIndicator(
              value: (_secondsRemaining / 120).clamp(0.0, 1.0),
              backgroundColor: Colors.grey.shade200,
              valueColor:
                  AlwaysStoppedAnimation(urgent ? Colors.red : Colors.orange),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop('rejected'),
          style: TextButton.styleFrom(foregroundColor: Colors.red),
          child: const Text('Rechazar'),
        ),
        ElevatedButton(
          onPressed: () => Navigator.of(context).pop('accepted'),
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.green,
            foregroundColor: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
          ),
          child: const Text('Aceptar'),
        ),
      ],
    );
  }
}

class _Row extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _Row({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Row(
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
      ],
    );
  }
}
