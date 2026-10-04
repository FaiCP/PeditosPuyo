import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:vibration/vibration.dart';
import '../config/constants.dart';

class NotificationService {
  final FlutterLocalNotificationsPlugin _notifications = FlutterLocalNotificationsPlugin();

  Future<void> initialize() async {
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings();
    const settings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );

    await _notifications.initialize(settings);

    await _notifications
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(
      const AndroidNotificationChannel(
        'new_assignments',
        'New Delivery Assignments',
        description: 'Alerts for new delivery assignments',
        importance: Importance.max,
        enableVibration: true,
      ),
    );
  }

  Future<void> showNewAssignmentNotification({
    required String restaurantName,
    required String deliveryAddress,
  }) async {
    const details = NotificationDetails(
      android: AndroidNotificationDetails(
        'new_assignments',
        'New Delivery Assignments',
        channelDescription: 'Alerts for new delivery assignments',
        importance: Importance.max,
        priority: Priority.max,
        sound: RawResourceAndroidNotificationSound('notification_sound'),
      ),
      iOS: DarwinNotificationDetails(
        presentAlert: true,
        presentBadge: true,
        presentSound: true,
      ),
    );

    await _notifications.show(
      DateTime.now().millisecondsSinceEpoch ~/ 1000,
      'Nueva carrera disponible',
      '$restaurantName → $deliveryAddress',
      details,
    );

    await vibrate();
  }

  Future<void> vibrate() async {
    if (await Vibration.hasVibrator() ?? false) {
      Vibration.vibrate(
        pattern: [0, 500, 200, 500, 200, 500],
        intensities: [0, 255, 0, 255, 0, 255],
      );
    }
  }
}
