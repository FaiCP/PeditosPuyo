import 'package:flutter/material.dart';
import '../models/assignment.dart';
import '../services/api_service.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  final _apiService = ApiService();
  List<Assignment> _history = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadHistory();
  }

  Future<void> _loadHistory() async {
    try {
      final response = await _apiService.get('/assignments');
      final assignments = (response as List)
          .map((json) => Assignment.fromJson(json))
          .where((a) => a.status == 'Delivered' || a.status == 'Rejected')
          .toList();
      setState(() {
        _history = assignments;
        _loading = false;
      });
    } catch (e) {
      debugPrint('Error loading history: $e');
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
            Text('Sin historial', style: TextStyle(fontSize: 18)),
            SizedBox(height: 8),
            Text('Tus carreras completadas aparecerán aquí',
                style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadHistory,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _history.length,
        itemBuilder: (context, index) {
          final assignment = _history[index];
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor: assignment.status == 'Delivered'
                    ? Colors.green.shade100
                    : Colors.red.shade100,
                child: Icon(
                  assignment.status == 'Delivered'
                      ? Icons.check_circle
                      : Icons.cancel,
                  color: assignment.status == 'Delivered'
                      ? Colors.green
                      : Colors.red,
                ),
              ),
              title: Text(assignment.restaurantName),
              subtitle: Text(
                '${assignment.status == 'Delivered' ? 'Entregado' : 'Rechazado'} - ${_formatDate(assignment.assignedAt)}',
                style: const TextStyle(fontSize: 12),
              ),
              trailing: const Icon(Icons.chevron_right),
            ),
          );
        },
      ),
    );
  }

  String _formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }
}
