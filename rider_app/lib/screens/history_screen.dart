import 'package:flutter/material.dart';

class HistoryScreen extends StatelessWidget {
  const HistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.history, size: 64, color: Colors.grey),
          SizedBox(height: 16),
          Text('Sin historial de deliveries', style: TextStyle(fontSize: 18)),
          SizedBox(height: 8),
          Text('Tus carreras entregadas aparecerán aquí', style: TextStyle(color: Colors.grey)),
        ],
      ),
    );
  }
}
