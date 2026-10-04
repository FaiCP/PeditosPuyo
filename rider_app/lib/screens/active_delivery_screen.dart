import 'package:flutter/material.dart';
import '../models/assignment.dart';
import '../services/api_service.dart';

class ActiveDeliveryScreen extends StatefulWidget {
  final Assignment? assignment;

  const ActiveDeliveryScreen({super.key, this.assignment});

  @override
  State<ActiveDeliveryScreen> createState() => _ActiveDeliveryScreenState();
}

class _ActiveDeliveryScreenState extends State<ActiveDeliveryScreen> {
  final _apiService = ApiService();

  @override
  void didUpdateWidget(ActiveDeliveryScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
  }

  Future<void> _updateStatus(String status) async {
    if (widget.assignment == null) return;

    try {
      await _apiService
          .put('/assignments/${widget.assignment!.id}/status', {
        'status': status,
      });

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_getStatusMessage(status)),
            backgroundColor: _getStatusColor(status),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Error al actualizar estado'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  String _getStatusMessage(String status) {
    switch (status) {
      case 'IN_TRANSIT':
        return 'En camino al restaurante';
      case 'DELIVERED':
        return '¡Delivery completado!';
      default:
        return 'Estado actualizado';
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'IN_TRANSIT':
        return Colors.orange;
      case 'DELIVERED':
        return Colors.green;
      default:
        return Colors.blue;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.assignment == null) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.delivery_dining, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Text('Sin deliveries activos',
                style: TextStyle(fontSize: 18)),
            SizedBox(height: 8),
            Text('Cuando aceptes una carrera, aparecerá aquí',
                style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }

    final assignment = widget.assignment!;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.store, color: Colors.blue),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          assignment.restaurantName,
                          style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      Chip(
                        label: Text(assignment.status),
                        backgroundColor: _getStatusChipColor(assignment.status),
                      ),
                    ],
                  ),
                  const Divider(),
                  const _InfoRow(
                    icon: Icons.location_on,
                    label: 'Recolección',
                    value: 'Restaurante',
                  ),
                  const SizedBox(height: 12),
                  const _InfoRow(
                    icon: Icons.location_on,
                    label: 'Entrega',
                    value: 'Destino del cliente',
                  ),
                  const SizedBox(height: 12),
                  _InfoRow(
                    icon: Icons.access_time,
                    label: 'Asignado',
                    value: _formatDate(assignment.assignedAt),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          if (assignment.status == 'Accepted') ...[
            ElevatedButton.icon(
              onPressed: () => _updateStatus('IN_TRANSIT'),
              icon: const Icon(Icons.directions_car),
              label: const Text('Iniciar viaje'),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.orange,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ] else if (assignment.status == 'InTransit') ...[
            ElevatedButton.icon(
              onPressed: () => _updateStatus('DELIVERED'),
              icon: const Icon(Icons.check_circle),
              label: const Text('Marcar como entregado'),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.green,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Color _getStatusChipColor(String status) {
    switch (status) {
      case 'Accepted':
        return Colors.green.shade100;
      case 'InTransit':
        return Colors.orange.shade100;
      case 'Delivered':
        return Colors.blue.shade100;
      default:
        return Colors.grey.shade100;
    }
  }

  String _formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year} ${date.hour}:${date.minute.toString().padLeft(2, '0')}';
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
      children: [
        Icon(icon, size: 20, color: Colors.grey),
        const SizedBox(width: 8),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: const TextStyle(fontSize: 12, color: Colors.grey)),
              Text(value,
                  style: const TextStyle(fontWeight: FontWeight.w500)),
            ],
          ),
        ),
      ],
    );
  }
}
