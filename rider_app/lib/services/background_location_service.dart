import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';

class BackgroundLocationService {
  static final BackgroundLocationService _instance =
      BackgroundLocationService._internal();
  factory BackgroundLocationService() => _instance;
  BackgroundLocationService._internal();

  final _apiService = ApiService();
  Timer? _timer;
  bool _isRunning = false;

  bool get isRunning => _isRunning;

  Future<void> startService(String riderId) async {
    if (riderId.isEmpty || _isRunning) return;

    try {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) return;

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) return;
      }

      if (permission == LocationPermission.deniedForever) return;

      _isRunning = true;
      _startPeriodicUpdates(riderId);

      debugPrint('Background location service started for rider: $riderId');
    } catch (e) {
      debugPrint('Error starting background location: $e');
    }
  }

  void _startPeriodicUpdates(String riderId) {
    _sendLocation(riderId);
    _timer = Timer.periodic(
      const Duration(seconds: 30),
      (_) => _sendLocation(riderId),
    );
  }

  Future<void> _sendLocation(String riderId) async {
    try {
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );

      await _apiService.put('/riders/$riderId/location', {
        'lat': position.latitude,
        'lng': position.longitude,
      });

      debugPrint(
          'Background location sent: ${position.latitude}, ${position.longitude}');
    } catch (e) {
      debugPrint('Error sending background location: $e');
    }
  }

  Future<void> stopService() async {
    _timer?.cancel();
    _timer = null;
    _isRunning = false;
    debugPrint('Background location service stopped');
  }
}
