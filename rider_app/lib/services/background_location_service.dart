import 'dart:convert';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';
import '../config/constants.dart';

class BackgroundLocationService {
  static Future<void> initialize() async {
    await FlutterBackgroundService.configure(
      androidNotificationChannel: const AndroidNotificationChannel(
        'Puyo Delivery Location',
        'Puyo Delivery Location Service',
        description: 'Tracks your location while delivering orders',
        importance: Importance.low,
      ),
      iosNotificationOptions: const IOSNotificationOptions(),
      foregroundServiceNotificationOptions: const ForegroundServiceNotificationOptions(
        notificationId: 92901,
      ),
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        isForegroundMode: true,
        autoStart: false,
      ),
    );
  }

  static Future<void> onStart(ServiceInstance service) async {
    if (service is AndroidServiceInstance) {
      service.on('setAsForeground').listen((event) {
        service.setAsForegroundService();
      });
      service.on('setAsBackground').listen((event) {
        service.setAsBackgroundService();
      });
    }

    service.on('stopService').listen((event) {
      service.stopSelf();
    });

    Timer.periodic(const Duration(seconds: 5), (timer) async {
      try {
        final position = await Geolocator.getCurrentPosition(
          locationSettings: const LocationSettings(
            accuracy: LocationAccuracy.high,
          ),
        );

        final prefs = await SharedPreferences.getInstance();
        final token = prefs.getString(Constants.tokenKey);
        final riderId = prefs.getString(Constants.riderIdKey);

        if (token != null && riderId != null) {
          await http.post(
            Uri.parse('${ApiConfig.baseUrl}/driver/location'),
            headers: {
              'Content-Type': 'application/json',
              'Authorization': 'Bearer $token',
            },
            body: jsonEncode({
              'lat': position.latitude,
              'lng': position.longitude,
            }),
          );
        }

        service.invoke(
          'update',
          {
            'lat': position.latitude,
            'lng': position.longitude,
          },
        );
      } catch (e) {
        // Handle error silently
      }
    });
  }

  static Future<void> startService() async {
    await FlutterBackgroundService().startService();
  }

  static Future<void> stopService() async {
    await FlutterBackgroundService().stopService();
  }
}
