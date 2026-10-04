# Task Flutter - App Rider

## Modelo: Qwen3.7 Plus

## Estructura del Proyecto

```
rider_app/
├── android/
├── lib/
│   ├── main.dart
│   ├── app.dart
│   ├── config/
│   │   ├── api_config.dart
│   │   ├── theme.dart
│   │   └── constants.dart
│   ├── models/
│   │   ├── user.dart
│   │   ├── rider.dart
│   │   ├── assignment.dart
│   │   └── delivery_request.dart
│   ├── services/
│   │   ├── auth_service.dart
│   │   ├── location_service.dart
│   │   ├── background_location_service.dart
│   │   ├── notification_service.dart
│   │   ├── api_service.dart
│   │   └── signalr_service.dart
│   ├── screens/
│   │   ├── login_screen.dart
│   │   ├── home_screen.dart
│   │   ├── active_delivery_screen.dart
│   │   └── history_screen.dart
│   ├── widgets/
│   │   ├── new_order_dialog.dart
│   │   ├── rider_status_toggle.dart
│   │   ├── delivery_card.dart
│   │   └── map_widget.dart
│   └── utils/
│       ├── permissions.dart
│       └── helpers.dart
├── pubspec.yaml
└── README.md
```

## Dependencias (pubspec.yaml)

```yaml
name: rider_app
description: Puyo Delivery - App del Rider
publish_to: 'none'
version: 1.0.0+1

environment:
  sdk: '>=3.3.0 <4.0.0'

dependencies:
  flutter:
    sdk: flutter
  geolocator: ^11.0.0
  flutter_background_service: ^5.0.0
  firebase_core: ^2.32.0
  firebase_messaging: ^14.8.0
  flutter_local_notifications: ^17.1.0
  http: ^1.2.0
  shared_preferences: ^2.2.0
  flutter_map: ^6.1.0
  latlong2: ^0.9.0
  web_socket_channel: ^2.4.0
  permission_handler: ^11.1.0
  vibration: ^1.8.0

dev_dependencies:
  flutter_test:
    sdk: flutter
  flutter_lints: ^4.0.0

flutter:
  uses-material-design: true
  assets:
    - assets/sounds/
    - assets/images/
```

## Archivos Detallados

### background_location_service.dart
- Foreground Service con notificación persistente.
- Envía `POST /api/driver/location` cada 5 segundos con `{ lat, lng }`.
- Se une al grupo SignalR del rider.
- Se desactiva cuando el rider está offline.

### notification_service.dart
- Inicializa FCM.
- Registra el `fcm_token` con el backend.
- Muestra notificación con sonido de alta prioridad al recibir nueva asignación.
- Vibra el dispositivo.

### signalr_service.dart
- Conecta al hub `/hubs/rider`.
- Se une al grupo `rider-{riderId}`.
- Escucha `newAssignment` y `assignmentCancelled`.
- Envía `updateLocation` en tiempo real.

### new_order_dialog.dart
- BottomSheet modal.
- Muestra: restaurante, dirección recolección, dirección entrega, distancia estimada.
- Temporizador de 15 segundos (contador regresivo).
- Botones: Aceptar (verde) / Rechazar (rojo).
- Si no responde en 15s → rechazo automático.
- Si acepta → `PUT /api/assignments/{id}/accept`.

### login_screen.dart
- Email + password.
- Llama `POST /api/auth/login`.
- Guarda token en SharedPreferences.
- Redirige a HomeScreen.

### home_screen.dart
- Toggle online/offline.
- Mapa con ubicación actual.
- Lista de deliveries activos.
- Contador de carreras del día.

## Permisos Android (AndroidManifest.xml)

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
<uses-permission android:name="android.permission.VIBRATE" />
```

## Build y Distribución

```bash
# Build release APK
flutter build apk --release

# El archivo queda en:
# build/app/outputs/flutter-apk/app-release.apk

# Distribución: subir a cloud storage y compartir link
```

## Configuración Firebase

1. Crear proyecto en Firebase Console.
2. Agregar app Android con package name `com.puyo.riderapp`.
3. Descargar `google-services.json` → `android/app/`.
4. Configurar `main.dart` con `Firebase.initializeApp()`.
5. Configurar FCM en Firebase Console.
