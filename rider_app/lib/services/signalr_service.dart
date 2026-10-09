import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:web_socket_channel/web_socket_channel.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';
import '../config/constants.dart';

/// Implementación ligera del cliente SignalR JSON sobre WebSocket.
/// Incluye handshake, conversión https→wss y separador de mensajes (0x1e).
class SignalRService {
  WebSocketChannel? _channel;
  final StreamController<Map<String, dynamic>> _messageController =
      StreamController<Map<String, dynamic>>.broadcast();

  Stream<Map<String, dynamic>> get messages => _messageController.stream;

  static const String _recordSeparator = '\u001e';

  Future<void> connect(String riderId) async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(Constants.tokenKey);

    var wsUrl = ApiConfig.wsUrl;
    if (wsUrl.startsWith('https://')) {
      wsUrl = wsUrl.replaceFirst('https://', 'wss://');
    } else if (wsUrl.startsWith('http://')) {
      wsUrl = wsUrl.replaceFirst('http://', 'ws://');
    }

    final uri = Uri.parse('$wsUrl/hubs/rider?access_token=$token');
    debugPrint('SignalR connecting to $uri');
    _channel = WebSocketChannel.connect(uri);

    // Handshake requerido por SignalR.
    final handshake = jsonEncode({'protocol': 'json', 'version': 1});
    _channel!.sink.add('$handshake$_recordSeparator');

    final completer = Completer<void>();

    _channel!.stream.listen(
      (data) {
        final raw = data as String;
        for (final message in raw.split(_recordSeparator)..removeWhere((s) => s.isEmpty)) {
          final json = jsonDecode(message) as Map<String, dynamic>;
          debugPrint('SignalR message: $json');

          // Handshake response: {"error": null}
          if (json.containsKey('error') && !completer.isCompleted) {
            if (json['error'] != null) {
              completer.completeError(Exception(json['error']));
            } else {
              completer.complete();
              _invoke('JoinRider', [riderId]);
            }
            continue;
          }

          if (json['type'] == 1 && json['target'] != null) {
            _messageController.add({
              'event': json['target'],
              'data': (json['arguments'] as List?)?.isNotEmpty == true
                  ? json['arguments'][0]
                  : null,
            });
          }
        }
      },
      onError: (error) {
        debugPrint('WebSocket error: $error');
        if (!completer.isCompleted) completer.completeError(error);
      },
      onDone: () {
        debugPrint('WebSocket closed');
        if (!completer.isCompleted) {
          completer.completeError(Exception('WebSocket cerrado antes del handshake'));
        }
      },
    );

    return completer.future.timeout(const Duration(seconds: 8));
  }

  void sendLocation(String riderId, double lat, double lng) {
    _invoke('UpdateLocation', [riderId, lat, lng]);
  }

  void _invoke(String target, List<dynamic> arguments) {
    if (_channel == null) return;
    final payload = jsonEncode({'type': 1, 'target': target, 'arguments': arguments});
    _channel!.sink.add('$payload$_recordSeparator');
  }

  void disconnect() {
    _channel?.sink.close();
    _channel = null;
  }
}
