class User {
  final String id;
  final String email;
  final String fullName;
  final String role;
  final String? tenantId;

  User({
    required this.id,
    required this.email,
    required this.fullName,
    required this.role,
    this.tenantId,
  });

  factory User.fromJson(Map<String, dynamic> json) => User(
        id: json['userId'] ?? json['id'] ?? '',
        email: json['email'] ?? '',
        fullName: json['fullName'] ?? '',
        role: json['role'] ?? '',
        tenantId: json['tenantId'],
      );
}
