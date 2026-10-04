class Assignment {
  final String id;
  final String requestId;
  final String riderId;
  final String riderName;
  final String restaurantName;
  final String status;
  final String? rejectionReason;
  final DateTime assignedAt;
  final DateTime? acceptedAt;

  Assignment({
    required this.id,
    required this.requestId,
    required this.riderId,
    required this.riderName,
    required this.restaurantName,
    required this.status,
    this.rejectionReason,
    required this.assignedAt,
    this.acceptedAt,
  });

  factory Assignment.fromJson(Map<String, dynamic> json) => Assignment(
        id: json['id'] ?? '',
        requestId: json['requestId'] ?? '',
        riderId: json['riderId'] ?? '',
        riderName: json['riderName'] ?? '',
        restaurantName: json['restaurantName'] ?? '',
        status: json['status'] ?? '',
        rejectionReason: json['rejectionReason'],
        assignedAt: DateTime.parse(json['assignedAt'] ?? DateTime.now().toIso8601String()),
        acceptedAt: json['acceptedAt'] != null
            ? DateTime.parse(json['acceptedAt'])
            : null,
      );
}
