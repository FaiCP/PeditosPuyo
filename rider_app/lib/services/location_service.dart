import 'dart:async';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';
import 'signalr_service.dart';
import '../config/constants.dart';

class LocationService {
  final ApiService _api = ApiService();
  final SignalRService _signalR = SignalRService();
  StreamSubscription<Position>? _subscription;
  bool _isTracking = false;

  bool get isTracking => _isTracking;

  Future<void> startTracking(String riderId) async {
    if (_isTracking) return;

    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      throw Exception('Location services are disabled');
    }

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        throw Exception('Location permissions denied');
      }
    }

    if (permission == LocationPermission.deniedForever) {
      throw Exception('Location permissions permanently denied');
    }

    _isTracking = true;

    _subscription = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 10,
      ),
    ).listen((position) {
      _sendLocation(position.latitude, position.longitude, riderId);
    });
  }

  Future<void> stopTracking() async {
    _subscription?.cancel();
    _subscription = null;
    _isTracking = false;
  }

  Future<void> _sendLocation(double lat, double lng, String riderId) async {
    try {
      await _api.post('/driver/location', {'lat': lat, 'lng': lng});
      _signalR.sendLocation(riderId, lat, lng);
    } catch (e) {
      // Silently fail, will retry on next update
    }
  }
}
