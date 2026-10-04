import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../services/location_service.dart';
import '../services/notification_service.dart';
import '../widgets/new_order_dialog.dart';
import 'active_delivery_screen.dart';
import 'history_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final _locationService = LocationService();
  final _notificationService = NotificationService();
  bool _isOnline = false;
  int _selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    _notificationService.initialize();
  }

  void _toggleOnline() {
    setState(() {
      _isOnline = !_isOnline;
    });

    if (_isOnline) {
      _locationService.startTracking('current-rider-id');
    } else {
      _locationService.stopTracking();
    }
  }

  void _showNewOrderDialog() {
    showDialog(
      context: context,
      builder: (_) => const NewOrderDialog(
        restaurantName: 'Restaurante Ejemplo',
        pickupAddress: 'Av. Principal 123',
        deliveryAddress: 'Calle Secundaria 456',
        distanceKm: 2.5,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Puyo Delivery'),
        actions: [
          Switch(
            value: _isOnline,
            onChanged: (_) => _toggleOnline(),
            activeColor: Colors.green,
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await AuthService().logout();
              if (mounted) {
                Navigator.of(context).popUntil((route) => route.isFirst);
              }
            },
          ),
        ],
      ),
      body: IndexedStack(
        index: _selectedIndex,
        children: const [
          Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.map, size: 64, color: Colors.grey),
                SizedBox(height: 16),
                Text('Mapa en tiempo real', style: TextStyle(fontSize: 18)),
              ],
            ),
          ),
          ActiveDeliveryScreen(),
          HistoryScreen(),
        ],
      ),
      floatingActionButton: _isOnline
          ? FloatingActionButton(
              onPressed: _showNewOrderDialog,
              child: const Icon(Icons.notifications_active),
            )
          : null,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) => setState(() => _selectedIndex = index),
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.map), label: 'Mapa'),
          BottomNavigationBarItem(icon: Icon(Icons.delivery_dining), label: 'Activo'),
          BottomNavigationBarItem(icon: Icon(Icons.history), label: 'Historial'),
        ],
      ),
    );
  }
}
