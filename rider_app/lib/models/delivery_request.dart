class DeliveryRequest {
  final String id;
  final String restaurantId;
  final String restaurantName;
  final String status;
  final String deliveryAddress;
  final double lat;
  final double lng;
  final String? notes;
  final DateTime createdAt;

  DeliveryRequest({
    required this.id,
    required this.restaurantId,
    required this.restaurantName,
    required this.status,
    required this.deliveryAddress,
    required this.lat,
    required this.lng,
    this.notes,
    required this.createdAt,
  });

  factory DeliveryRequest.fromJson(Map<String, dynamic> json) => DeliveryRequest(
        id: json['id'] ?? '',
        restaurantId: json['restaurantId'] ?? '',
        restaurantName: json['restaurantName'] ?? '',
        status: json['status'] ?? '',
        deliveryAddress: json['deliveryAddress'] ?? '',
        lat: json['lat']?.toDouble() ?? 0,
        lng: json['lng']?.toDouble() ?? 0,
        notes: json['notes'],
        createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
      );
}
