import 'package:flutter/material.dart';
import '../models/rider_active_order.dart';
import '../services/api_service.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  final _apiService = ApiService();
  List<OrderTracking> _history = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final list = await _apiService.getList('/rider/orders/history');
      setState(() {
        _history = list.map((j) => OrderTracking.fromJson(j)).toList();
        _loading = false;
      });
    } catch (e) {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_history.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.history, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Text('Sin entregas todavía', style: TextStyle(fontSize: 18)),
          ],
        ),
      );
    }

    final earned = _history.fold(0.0, (sum, o) => sum + o.deliveryFeeAmount);

    return RefreshIndicator(
      onRefresh: _load,
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(16),
            color: Colors.green.shade700,
            child: Text(
              '${_history.length} entregas · \$${earned.toStringAsFixed(2)} en tarifas',
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
            ),
          ),
          Expanded(
            child: ListView.builder(
              itemCount: _history.length,
              itemBuilder: (context, index) {
                final o = _history[index];
                return ListTile(
                  leading: Text(o.typeEmoji, style: const TextStyle(fontSize: 24)),
                  title: Text(o.originName),
                  subtitle: Text(o.destinationAddress),
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text('\$${o.deliveryFeeAmount.toStringAsFixed(2)}',
                          style: const TextStyle(fontWeight: FontWeight.bold)),
                      Text('✓ Entregado',
                          style: TextStyle(fontSize: 11, color: Colors.green.shade700)),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
