import 'package:flutter/material.dart';
import 'config/theme.dart';
import 'screens/login_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const RiderApp());
}

class RiderApp extends StatelessWidget {
  const RiderApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Puyo Delivery Rider',
      theme: AppTheme.theme,
      home: const LoginScreen(),
    );
  }
}
