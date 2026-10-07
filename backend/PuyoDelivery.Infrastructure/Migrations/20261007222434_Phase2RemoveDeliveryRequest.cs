using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PuyoDelivery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RemoveDeliveryRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAssignments");

            migrationBuilder.DropTable(
                name: "DeliveryRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: false),
                    DeliveryLocation = table.Column<Point>(type: "geometry(point, 4326)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRequests_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAssignments_DeliveryRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "DeliveryRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAssignments_Riders_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRequests_DeliveryLocation",
                table: "DeliveryRequests",
                column: "DeliveryLocation")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRequests_RestaurantId",
                table: "DeliveryRequests",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_RequestId",
                table: "OrderAssignments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_RiderId",
                table: "OrderAssignments",
                column: "RiderId");
        }
    }
}
