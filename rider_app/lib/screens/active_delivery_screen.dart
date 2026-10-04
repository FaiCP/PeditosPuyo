import 'package:flutter/material.dart';

class ActiveDeliveryScreen extends StatelessWidget {
  const ActiveDeliveryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.delivery_dining, size: 64, color: Colors.grey),
          SizedBox(height: 16),
          Text('Sin deliveries activos', style: TextStyle(fontSize: 18)),
          SizedBox(height: 8),
          Text('Cuando aceptes una carrera, aparecerá aquí', style: TextStyle(color: Colors.grey)),
        ],
      ),
    );
  }
}
