class OrderLine {
  final String name;
  final int quantity;
  final double unitPrice;

  OrderLine({required this.name, required this.quantity, required this.unitPrice});

  factory OrderLine.fromJson(Map<String, dynamic> json) => OrderLine(
        name: json['name'] ?? '',
        quantity: (json['quantity'] ?? 0).toInt(),
        unitPrice: (json['unitPrice'] ?? 0).toDouble(),
      );

  double get subtotal => quantity * unitPrice;
}

class RiderOffer {
  final String offerId;
  final String orderId;
  final String type; // restaurant | compra | encargo
  final String originName;
  final String originAddress;
  final String destinationAddress;
  final String? description;
  final double deliveryFeeAmount;
  final List<OrderLine> items;
  final DateTime expiresAt;

  RiderOffer({
    required this.offerId,
    required this.orderId,
    required this.type,
    required this.originName,
    required this.originAddress,
    required this.destinationAddress,
    this.description,
    required this.deliveryFeeAmount,
    required this.items,
    required this.expiresAt,
  });

  factory RiderOffer.fromJson(Map<String, dynamic> json) => RiderOffer(
        offerId: json['offerId'] ?? '',
        orderId: json['orderId'] ?? '',
        type: json['type'] ?? '',
        originName: json['originName'] ?? '',
        originAddress: json['originAddress'] ?? '',
        destinationAddress: json['destinationAddress'] ?? '',
        description: json['description'],
        deliveryFeeAmount: (json['deliveryFeeAmount'] ?? 0).toDouble(),
        items: ((json['items'] ?? []) as List)
            .map((i) => OrderLine.fromJson(i as Map<String, dynamic>))
            .toList(),
        expiresAt: DateTime.parse(json['expiresAt']).toUtc(),
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

  String get typeLabel {
    switch (type) {
      case 'restaurant':
        return 'Restaurante';
      case 'compra':
        return 'Compra';
      case 'encargo':
        return 'Encargo';
      default:
        return type;
    }
  }

  double get productsAmount => items.fold(0.0, (sum, i) => sum + i.subtotal);
}
