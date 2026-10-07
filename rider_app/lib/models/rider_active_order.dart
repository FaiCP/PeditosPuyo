import 'rider_offer.dart';

class OrderTracking {
  final String id;
  final String type;
  final String status;
  final String originName;
  final String originAddress;
  final String destinationAddress;
  final String? description;
  final double productsAmount;
  final double deliveryFeeAmount;
  final double totalAmount;
  final String paymentMethod;
  final String? deliveryCode;
  final String? riderName;
  final String? cancelDetail;
  final List<OrderLine> items;

  OrderTracking({
    required this.id,
    required this.type,
    required this.status,
    required this.originName,
    required this.originAddress,
    required this.destinationAddress,
    this.description,
    required this.productsAmount,
    required this.deliveryFeeAmount,
    required this.totalAmount,
    required this.paymentMethod,
    this.deliveryCode,
    this.riderName,
    this.cancelDetail,
    required this.items,
  });

  factory OrderTracking.fromJson(Map<String, dynamic> json) => OrderTracking(
        id: json['id'] ?? '',
        type: json['type'] ?? '',
        status: json['status'] ?? '',
        originName: json['originName'] ?? '',
        originAddress: json['originAddress'] ?? '',
        destinationAddress: json['destinationAddress'] ?? '',
        description: json['description'],
        productsAmount: (json['productsAmount'] ?? 0).toDouble(),
        deliveryFeeAmount: (json['deliveryFeeAmount'] ?? 0).toDouble(),
        totalAmount: (json['totalAmount'] ?? 0).toDouble(),
        paymentMethod: json['paymentMethod'] ?? '',
        deliveryCode: json['deliveryCode'],
        riderName: json['riderName'],
        cancelDetail: json['cancelDetail'],
        items: ((json['items'] ?? []) as List)
            .map((i) => OrderLine.fromJson(i as Map<String, dynamic>))
            .toList(),
      );

  String get typeEmoji {
    switch (type) {
      case 'restaurant':
        return '🍔';
      case 'compra':
        return '🛒';
      case 'encargo':
        return '📦';
      default:
        return '🛵';
    }
  }
}

class RiderActiveOrder {
  final OrderTracking order;
  final double originLat;
  final double originLng;
  final double destinationLat;
  final double destinationLng;
  final String? pickupCode;

  RiderActiveOrder({
    required this.order,
    required this.originLat,
    required this.originLng,
    required this.destinationLat,
    required this.destinationLng,
    this.pickupCode,
  });

  factory RiderActiveOrder.fromJson(Map<String, dynamic> json) =>
      RiderActiveOrder(
        order: OrderTracking.fromJson(json['order'] as Map<String, dynamic>),
        originLat: (json['originLat'] ?? 0).toDouble(),
        originLng: (json['originLng'] ?? 0).toDouble(),
        destinationLat: (json['destinationLat'] ?? 0).toDouble(),
        destinationLng: (json['destinationLng'] ?? 0).toDouble(),
        pickupCode: json['pickupCode'],
      );
}
