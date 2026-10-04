import 'dart:async';
import 'package:flutter/material.dart';
import '../config/constants.dart';

class NewOrderDialog extends StatefulWidget {
  final String restaurantName;
  final String pickupAddress;
  final String deliveryAddress;
  final double distanceKm;

  const NewOrderDialog({
    super.key,
    required this.restaurantName,
    required this.pickupAddress,
    required this.deliveryAddress,
    required this.distanceKm,
  });

  @override
  State<NewOrderDialog> createState() => _NewOrderDialogState();
}

class _NewOrderDialogState extends State<NewOrderDialog> {
  Timer? _timer;
  int _secondsRemaining = 15;

  @override
  void initState() {
    super.initState();
    _startTimer();
  }

  void _startTimer() {
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_secondsRemaining > 0) {
        setState(() => _secondsRemaining--);
      } else {
        timer.cancel();
        _reject('Timeout');
      }
    });
  }

  void _accept() {
    _timer?.cancel();
    Navigator.of(context).pop('accepted');
  }

  void _reject(String reason) {
    _timer?.cancel();
    Navigator.of(context).pop('rejected');
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          const Text('Nueva Carrera'),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              color: _secondsRemaining <= 5 ? Colors.red : Colors.orange,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              '${_secondsRemaining}s',
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _InfoRow(
            icon: Icons.store,
            label: 'Restaurante',
            value: widget.restaurantName,
          ),
          const SizedBox(height: 12),
          _InfoRow(
            icon: Icons.location_on,
            label: 'Recolección',
            value: widget.pickupAddress,
          ),
          const SizedBox(height: 12),
          _InfoRow(
            icon: Icons.location_on,
            label: 'Entrega',
            value: widget.deliveryAddress,
          ),
          const SizedBox(height: 12),
          _InfoRow(
            icon: Icons.straighten,
            label: 'Distancia',
            value: '${widget.distanceKm} km',
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => _reject('Rechazado por el rider'),
          style: TextButton.styleFrom(foregroundColor: Colors.red),
          child: const Text('Rechazar'),
        ),
        ElevatedButton(
          onPressed: _accept,
          style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
          child: const Text('Aceptar'),
        ),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

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
              Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
              Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
            ],
          ),
        ),
      ],
    );
  }
}
