import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';
import '../config/constants.dart';

class ApiService {
  static String get baseUrl => ApiConfig.baseUrl;

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(Constants.tokenKey);
  }

  Future<Map<String, String>> _headers() async {
    final token = await getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<Map<String, dynamic>> get(String path) async {
    final response = await http.get(
      Uri.parse('$baseUrl$path'),
      headers: await _headers(),
    );
    return _handleResponse(response, path);
  }

  Future<List<dynamic>> getList(String path) async {
    final response = await http.get(
      Uri.parse('$baseUrl$path'),
      headers: await _headers(),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return jsonDecode(response.body) as List<dynamic>;
    }
    debugPrint('API error GET $path -> ${response.statusCode}: ${response.body}');
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    throw Exception(json['error'] ?? 'Error ${response.statusCode}');
  }

  Future<Map<String, dynamic>> post(String path, Map<String, dynamic> body) async {
    final response = await http.post(
      Uri.parse('$baseUrl$path'),
      headers: await _headers(),
      body: jsonEncode(body),
    );
    return _handleResponse(response, path);
  }

  Future<void> postEmpty(String path) async {
    final response = await http.post(
      Uri.parse('$baseUrl$path'),
      headers: await _headers(),
    );
    if (!(response.statusCode >= 200 && response.statusCode < 300)) {
      debugPrint('API error POST $path -> ${response.statusCode}: ${response.body}');
      throw Exception('Error ${response.statusCode}');
    }
  }

  Future<Map<String, dynamic>> put(String path, Map<String, dynamic> body) async {
    final response = await http.put(
      Uri.parse('$baseUrl$path'),
      headers: await _headers(),
      body: jsonEncode(body),
    );
    return _handleResponse(response, path);
  }

  Map<String, dynamic> _handleResponse(http.Response response, String path) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    debugPrint('API error ${response.request?.method ?? ''} $path -> ${response.statusCode}: ${response.body}');
    final json = jsonDecode(response.body) as Map<String, dynamic>;
    throw Exception(json['error'] ?? 'Error ${response.statusCode}');
  }
}
