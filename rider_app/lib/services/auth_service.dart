import 'package:shared_preferences/shared_preferences.dart';
import '../config/constants.dart';
import '../models/user.dart';
import 'api_service.dart';

class AuthService {
  final ApiService _api = ApiService();

  Future<User?> login(String email, String password) async {
    final response = await _api.post('/auth/login', {
      'email': email,
      'password': password,
    });

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(Constants.tokenKey, response['token'] ?? '');
    await prefs.setString(Constants.userIdKey, response['userId'] ?? '');
    await prefs.setString(Constants.riderIdKey, response['riderId'] ?? '');

    return User.fromJson(response);
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(Constants.tokenKey);
    await prefs.remove(Constants.userIdKey);
    await prefs.remove(Constants.riderIdKey);
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(Constants.tokenKey);
  }
}
