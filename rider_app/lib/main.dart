import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'config/theme.dart';
import 'config/constants.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';
import 'services/firebase_messaging_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await FirebaseMessagingService.initialize();
  } catch (e) {
    debugPrint('Firebase init error: $e');
  }
  runApp(const RiderApp());
}

class RiderApp extends StatelessWidget {
  const RiderApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Puyo Delivery Rider',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.theme,
      home: const SplashScreen(),
    );
  }
}

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    await Future.delayed(const Duration(seconds: 1));
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(Constants.tokenKey);

    if (!mounted) return;

    if (token != null && token.isNotEmpty) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const HomeScreen()),
      );
    } else {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Color(0xFF1971C2),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.motorcycle,
              size: 100,
              color: Colors.white,
            ),
            SizedBox(height: 24),
            Text(
              'Puyo Delivery',
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            ),
            SizedBox(height: 8),
            Text(
              'Rider App',
              style: TextStyle(
                fontSize: 18,
                color: Colors.white70,
              ),
            ),
            SizedBox(height: 48),
            CircularProgressIndicator(
              color: Colors.white,
            ),
          ],
        ),
      ),
    );
  }
}
