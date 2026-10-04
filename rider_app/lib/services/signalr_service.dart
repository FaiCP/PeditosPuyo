import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:web_socket_channel/web_socket_channel.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';
import '../config/constants.dart';

class SignalRService {
  WebSocketChannel? _channel;
  final StreamController<Map<String, dynamic>> _messageController =
      StreamController<Map<String, dynamic>>.broadcast();

  Stream<Map<String, dynamic>> get messages => _messageController.stream;

  Future<void> connect(String riderId) async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(Constants.tokenKey);

    final uri = Uri.parse('${ApiConfig.wsUrl}/hubs/rider?access_token=$token');
    _channel = WebSocketChannel.connect(uri);

    // Join rider group
    _channel!.sink.add(jsonEncode({
      'target': 'JoinRider',
      'arguments': [riderId],
    }));

    _channel!.stream.listen(
      (data) {
        final json = jsonDecode(data as String);
        if (json['type'] == '1' && json['target'] != null) {
          _messageController.add({
            'event': json['target'],
            'data': json['arguments']?.isNotEmpty == true ? json['arguments'][0] : null,
          });
        }
      },
      onError: (error) {
        debugPrint('WebSocket error: $error');
      },
      onDone: () {
        debugPrint('WebSocket closed');
      },
    );
  }

  void sendLocation(String riderId, double lat, double lng) {
    _channel?.sink.add(jsonEncode({
      'target': 'UpdateLocation',
      'arguments': [riderId, lat, lng],
    }));
  }

  void disconnect() {
    _channel?.sink.close();
    _channel = null;
  }
}
