import 'package:flutter_test/flutter_test.dart';
import 'package:rider_app/main.dart';

void main() {
  testWidgets('App renders login screen', (WidgetTester tester) async {
    await tester.pumpWidget(const RiderApp());
    expect(find.text('Puyo Delivery'), findsOneWidget);
    // Avanza el reloj para que el Future.delayed del splash se complete.
    await tester.pump(const Duration(seconds: 2));
  });
}
