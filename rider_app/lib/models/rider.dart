class Rider {
  final String id;
  final String fullName;
  final String phone;
  final String vehiclePlate;
  final bool isOnline;
  final bool isBusy;
  final double? lat;
  final double? lng;

  Rider({
    required this.id,
    required this.fullName,
    required this.phone,
    required this.vehiclePlate,
    this.isOnline = false,
    this.isBusy = false,
    this.lat,
    this.lng,
  });

  factory Rider.fromJson(Map<String, dynamic> json) => Rider(
        id: json['id'] ?? '',
        fullName: json['fullName'] ?? '',
        phone: json['phone'] ?? '',
        vehiclePlate: json['vehiclePlate'] ?? '',
        isOnline: json['isOnline'] ?? false,
        isBusy: json['isBusy'] ?? false,
        lat: json['lat']?.toDouble(),
        lng: json['lng']?.toDouble(),
      );
}
