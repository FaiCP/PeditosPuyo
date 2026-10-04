class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'http://localhost:5001/api',
  );
  static const String wsUrl = String.fromEnvironment(
    'WS_URL',
    defaultValue: 'http://localhost:5001',
  );
}
