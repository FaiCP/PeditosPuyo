import 'dart:async';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../config/constants.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/location_service.dart';
import '../services/notification_service.dart';
import '../services/signalr_service.dart';
import '../services/firebase_messaging_service.dart';
import '../models/rider_offer.dart';
import '../widgets/order_offer_dialog.dart';
import 'active_orders_screen.dart';
import 'history_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final _authService = AuthService();
  final _apiService = ApiService();
  final _locationService = LocationService();
  final _notificationService = NotificationService();
  final _signalRService = SignalRService();

  bool _isOnline = false;
  int _selectedIndex = 0;
  String _riderId = '';
  final Set<String> _dialogOpenOfferIds = {};
  StreamSubscription? _signalRSubscription;
  Timer? _offerPoll;

  @override
  void initState() {
    super.initState();
    _loadRiderInfo();
    _notificationService.initialize();
  }

  Future<void> _loadRiderInfo() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      _riderId = prefs.getString(Constants.riderIdKey) ?? '';
    });
  }

  Future<void> _toggleOnline() async {
    if (_riderId.isEmpty) {
      _snack('No se encontró el ID del rider. Cierra sesión y vuelve a ingresar.');
      return;
    }

    final newStatus = !_isOnline;

    try {
      await _apiService.put('/riders/$_riderId/status', {
        'isOnline': newStatus,
      });
    } catch (e) {
      debugPrint('Error toggling online status: $e');
      _snack('No se pudo cambiar el estado: $e');
      return;
    }

    setState(() => _isOnline = newStatus);

      if (newStatus) {
        try {
          await _locationService.startTracking(_riderId);
        } catch (e) {
          debugPrint('Error starting location: $e');
        }
        try {
          await _signalRService.connect(_riderId);
          _listenToSignalR();
        } catch (e) {
          debugPrint('Error connecting SignalR: $e');
          _snack('Canal de ofertas no disponible; se usará consulta periódica.');
        }
        // Fallback: consultar ofertas cada 10s aunque SignalR falle.
        _offerPoll?.cancel();
        _offerPoll = Timer.periodic(const Duration(seconds: 10), (_) => _checkPendingOffers());
        try {
          await FirebaseMessagingService.refreshToken();
        } catch (e) {
          debugPrint('Error refreshing FCM token: $e');
        }
        _notificationService.showNotification(
          'En línea',
          'Estás recibiendo nuevos pedidos',
        );
        await _checkPendingOffers();
      } else {
        _offerPoll?.cancel();
        _locationService.stopTracking();
        _signalRService.disconnect();
        _signalRSubscription?.cancel();
        _notificationService.showNotification(
          'Fuera de línea',
          'No recibirás nuevos pedidos',
        );
      }
  }

  void _listenToSignalR() {
    _signalRSubscription?.cancel();
    _signalRSubscription = _signalRService.messages.listen((message) {
      final event = message['event'];
      final data = message['data'];

      if (event == 'newOrderOffer' && data != null) {
        _checkPendingOffers();
      } else if (event == 'orderGreenLight' && data != null) {
        _notificationService.showNotification(
          'Luz verde 🟢',
          'Pedido listo en el origen — código: ${data['pickupCode'] ?? ''}',
        );
      }
    });
  }

  /// Trae las ofertas pendientes del backend y las muestra una por una.
  Future<void> _checkPendingOffers() async {
    try {
      final list = await _apiService.getList('/rider/orders/offers');
      final offers =
          list.map((j) => RiderOffer.fromJson(j)).where((o) => o.offerId.isNotEmpty).toList();

      for (final offer in offers) {
        if (_dialogOpenOfferIds.contains(offer.offerId)) continue;
        _dialogOpenOfferIds.add(offer.offerId);

        _notificationService.showNotification(
          'Nuevo ${offer.typeLabel.toLowerCase()}',
          '${offer.originName} → ${offer.destinationAddress}',
        );

        if (!mounted) return;
        final result = await showDialog<String>(
          context: context,
          barrierDismissible: false,
          builder: (_) => OrderOfferDialog(offer: offer),
        );

        _dialogOpenOfferIds.remove(offer.offerId);

        if (result == 'accepted') {
          await _acceptOffer(offer);
        } else if (result == 'rejected') {
          await _rejectOffer(offer);
        }
      }
    } catch (e) {
      debugPrint('Error loading offers: $e');
    }
  }

  Future<void> _acceptOffer(RiderOffer offer) async {
    try {
      await _apiService.postEmpty('/rider/orders/offers/${offer.offerId}/accept');
      _notificationService.showNotification(
        'Aceptado',
        offer.type == 'restaurant'
            ? 'Ve al restaurante — espera la confirmación'
            : 'Ve al punto de origen',
      );
    } catch (e) {
      debugPrint('Error accepting offer: $e');
      if (mounted) {
        _snack('No se pudo aceptar: $e');
      }
    }
  }

  Future<void> _rejectOffer(RiderOffer offer) async {
    try {
      await _apiService.post('/rider/orders/offers/${offer.offerId}/reject', {
        'reason': 'Rechazado por el rider',
      });
    } catch (e) {
      debugPrint('Error rejecting offer: $e');
    }
  }

  void _snack(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(msg),
      backgroundColor: Colors.red,
    ));
  }

  Future<void> _logout() async {
    await _authService.logout();
    _locationService.stopTracking();
    _signalRService.disconnect();
    _signalRSubscription?.cancel();

    if (mounted) {
      Navigator.of(context).pushNamedAndRemoveUntil('/', (route) => false);
    }
  }

  @override
  void dispose() {
    _signalRSubscription?.cancel();
    _offerPoll?.cancel();
    _locationService.stopTracking();
    _signalRService.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Puyo Rider'),
        actions: [
          Container(
            margin: const EdgeInsets.only(right: 8),
            child: Row(
              children: [
                Text(
                  _isOnline ? 'En línea' : 'Fuera',
                  style: TextStyle(
                    color: _isOnline ? Colors.green : Colors.grey,
                    fontSize: 12,
                  ),
                ),
                Switch(
                  value: _isOnline,
                  onChanged: (_) => _toggleOnline(),
                  activeThumbColor: Colors.green,
                ),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: _logout,
          ),
        ],
      ),
      body: IndexedStack(
        index: _selectedIndex,
        children: [
          _buildMapTab(),
          const ActiveOrdersScreen(),
          const HistoryScreen(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) => setState(() => _selectedIndex = index),
        type: BottomNavigationBarType.fixed,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.map), label: 'Mapa'),
          BottomNavigationBarItem(icon: Icon(Icons.local_shipping), label: 'Pedidos'),
          BottomNavigationBarItem(icon: Icon(Icons.history), label: 'Historial'),
        ],
      ),
    );
  }

  Widget _buildMapTab() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            _isOnline ? Icons.gps_fixed : Icons.gps_off,
            size: 80,
            color: _isOnline ? Colors.green : Colors.grey,
          ),
          const SizedBox(height: 16),
          Text(
            _isOnline ? 'En línea — recibiendo pedidos' : 'Fuera de línea',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w500),
          ),
          const SizedBox(height: 8),
          Text(
            _isOnline
                ? 'Tu ubicación se está compartiendo'
                : 'Actívate para recibir pedidos',
            style: const TextStyle(color: Colors.grey),
          ),
          const SizedBox(height: 24),
          TextButton.icon(
            onPressed: _checkPendingOffers,
            icon: const Icon(Icons.refresh),
            label: const Text('Revisar ofertas ahora'),
          ),
        ],
      ),
    );
  }
}
