import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';

class LocationService {
  final _apiService = ApiService();
  Timer? _locationTimer;
  bool _isTracking = false;

  bool get isTracking => _isTracking;

  Future<void> startTracking(String riderId) async {
    if (riderId.isEmpty) return;

    try {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        debugPrint('Location services are disabled');
        return;
      }

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          debugPrint('Location permissions are denied');
          return;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        debugPrint('Location permissions are permanently denied');
        return;
      }

      _isTracking = true;
      _sendLocation(riderId);

      _locationTimer = Timer.periodic(
        const Duration(seconds: 5),
        (_) => _sendLocation(riderId),
      );

      debugPrint('Location tracking started for rider: $riderId');
    } catch (e) {
      debugPrint('Error starting location tracking: $e');
    }
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
          'Location sent: ${position.latitude}, ${position.longitude}');
    } catch (e) {
      debugPrint('Error sending location: $e');
    }
  }

  void stopTracking() {
    _locationTimer?.cancel();
    _locationTimer = null;
    _isTracking = false;
    debugPrint('Location tracking stopped');
  }
}
