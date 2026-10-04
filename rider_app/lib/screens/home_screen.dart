import 'dart:async';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../config/constants.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/location_service.dart';
import '../services/notification_service.dart';
import '../services/signalr_service.dart';
import '../models/assignment.dart';
import '../widgets/new_order_dialog.dart';
import 'active_delivery_screen.dart';
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
  String _riderName = '';
  Assignment? _activeAssignment;
  final List<Assignment> _pendingAssignments = [];
  StreamSubscription? _signalRSubscription;

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
      _riderName = prefs.getString(Constants.userNameKey) ?? 'Rider';
    });

    await _loadActiveAssignment();
  }

  Future<void> _loadActiveAssignment() async {
    try {
      final response = await _apiService.get('/assignments/rider/active');
      final assignments = (response as List)
          .map((json) => Assignment.fromJson(json))
          .toList();
      if (assignments.isNotEmpty) {
        setState(() => _activeAssignment = assignments.first);
      }
    } catch (e) {
      debugPrint('Error loading assignments: $e');
    }
  }

  Future<void> _toggleOnline() async {
    final newStatus = !_isOnline;
    setState(() => _isOnline = newStatus);

    try {
      final prefs = await SharedPreferences.getInstance();
      final riderId = prefs.getString(Constants.riderIdKey) ?? '';

      if (riderId.isNotEmpty) {
        await _apiService.put('/riders/$riderId/status', {
          'isOnline': newStatus,
        });
      }

      if (newStatus) {
        await _locationService.startTracking(riderId);
        await _signalRService.connect(riderId);
        _listenToSignalR();
        _notificationService.showNotification(
          'En línea',
          'Estás recibiendo nuevas carreras',
        );
      } else {
        _locationService.stopTracking();
        _signalRService.disconnect();
        _signalRSubscription?.cancel();
        _notificationService.showNotification(
          'Fuera de línea',
          'No recibirás nuevas carreras',
        );
      }
    } catch (e) {
      debugPrint('Error toggling online: $e');
      setState(() => _isOnline = !newStatus);
    }
  }

  void _listenToSignalR() {
    _signalRSubscription?.cancel();
    _signalRSubscription = _signalRService.messages.listen((message) {
      final event = message['event'];
      final data = message['data'];

      if (event == 'newAssignment' && data != null) {
        _handleNewAssignment(data as Map<String, dynamic>);
      }
    });
  }

  void _handleNewAssignment(Map<String, dynamic> data) {
    final assignment = Assignment.fromJson({
      'id': data['assignmentId'] ?? '',
      'requestId': data['requestId'] ?? '',
      'riderId': _riderId,
      'riderName': _riderName,
      'restaurantName': data['restaurantName'] ?? '',
      'status': 'Pending',
      'assignedAt': DateTime.now().toIso8601String(),
    });

    setState(() => _pendingAssignments.add(assignment));

    _notificationService.showNotification(
      'Nueva carrera',
      'Desde ${assignment.restaurantName}',
    );

    _showNewOrderDialog(assignment, data);
  }

  Future<void> _showNewOrderDialog(
    Assignment assignment,
    Map<String, dynamic> data,
  ) async {
    final result = await showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (_) => NewOrderDialog(
        assignmentId: assignment.id,
        restaurantName: assignment.restaurantName,
        pickupAddress: data['pickupAddress'] ?? '',
        deliveryAddress: data['deliveryAddress'] ?? '',
        distanceKm: (data['distanceKm'] ?? 0).toDouble(),
      ),
    );

    if (result == 'accepted') {
      await _acceptAssignment(assignment.id);
    } else if (result == 'rejected') {
      await _rejectAssignment(assignment.id, 'Rechazado por el rider');
    }

    setState(() {
      _pendingAssignments.removeWhere((a) => a.id == assignment.id);
    });
  }

  Future<void> _acceptAssignment(String assignmentId) async {
    try {
      await _apiService.put('/assignments/$assignmentId/accept', {});
      await _loadActiveAssignment();
      _notificationService.showNotification(
        'Carrera aceptada',
        'Dirígete al restaurante',
      );
    } catch (e) {
      debugPrint('Error accepting assignment: $e');
    }
  }

  Future<void> _rejectAssignment(String assignmentId, String reason) async {
    try {
      await _apiService.put('/assignments/$assignmentId/reject', {
        'reason': reason,
      });
    } catch (e) {
      debugPrint('Error rejecting assignment: $e');
    }
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
    _locationService.stopTracking();
    _signalRService.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_riderName.isNotEmpty ? _riderName : 'Puyo Delivery'),
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
          ActiveDeliveryScreen(assignment: _activeAssignment),
          const HistoryScreen(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) => setState(() => _selectedIndex = index),
        type: BottomNavigationBarType.fixed,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.map), label: 'Mapa'),
          BottomNavigationBarItem(
              icon: Icon(Icons.delivery_dining), label: 'Activo'),
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
            _isOnline ? 'En línea - Recibiendo carreras' : 'Fuera de línea',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w500),
          ),
          const SizedBox(height: 8),
          Text(
            _isOnline
                ? 'Tu ubicación se está compartiendo'
                : 'Actívate para recibir carreras',
            style: const TextStyle(color: Colors.grey),
          ),
          if (_pendingAssignments.isNotEmpty) ...[
            const SizedBox(height: 24),
            Badge(
              label: Text('${_pendingAssignments.length}'),
              child: const Icon(Icons.notifications_active, size: 48),
            ),
          ],
        ],
      ),
    );
  }
}
