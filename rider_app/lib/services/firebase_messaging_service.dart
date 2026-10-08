import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'notification_service.dart';
import 'api_service.dart';

/// Handler top-level requerido por firebase_messaging para mensajes en segundo plano.
@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  debugPrint('FCM background: ${message.notification?.title}');
  final service = NotificationService();
  await service.showNotification(
    message.notification?.title ?? 'Nuevo pedido',
    message.notification?.body ?? 'Tienes una oferta disponible',
  );
}

class FirebaseMessagingService {
  static final _messaging = FirebaseMessaging.instance;
  static final _api = ApiService();
  static final _notifications = NotificationService();
  static bool _initialized = false;

  static bool get isInitialized => _initialized;

  static Future<void> initialize() async {
    await Firebase.initializeApp();
    _initialized = true;

    // Permisos
    final settings = await _messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );
    debugPrint('FCM permission: ${settings.authorizationStatus}');

    // Canal Android (para notificaciones en primer plano)
    await _messaging.setForegroundNotificationPresentationOptions(
      alert: true,
      badge: true,
      sound: true,
    );

    // Token inicial
    await _updateToken();

    // Refresco de token
    FirebaseMessaging.instance.onTokenRefresh.listen((token) async {
      debugPrint('FCM token refreshed');
      await _sendTokenToBackend(token);
    });

    // Mensajes en primer plano: mostrar notificación local
    FirebaseMessaging.onMessage.listen((message) {
      debugPrint('FCM foreground: ${message.notification?.title}');
      _notifications.showNotification(
        message.notification?.title ?? 'Nuevo pedido',
        message.notification?.body ?? 'Tienes una oferta disponible',
      );
    });

    FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
  }

  static Future<void> refreshToken() async {
    if (!_initialized) return;
    await _updateToken();
  }

  static Future<void> _updateToken() async {
    if (!_initialized) return;
    try {
      final token = await _messaging.getToken();
      if (token != null && token.isNotEmpty) {
        await _sendTokenToBackend(token);
      }
    } catch (e) {
      debugPrint('FCM getToken error: $e');
    }
  }

  static Future<void> _sendTokenToBackend(String token) async {
    try {
      await _api.post('/rider/orders/fcm-token', {'token': token});
      debugPrint('FCM token enviado al backend');
    } catch (e) {
      debugPrint('Error enviando FCM token: $e');
    }
  }
}
