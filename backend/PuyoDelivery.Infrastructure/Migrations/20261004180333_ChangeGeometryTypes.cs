using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PuyoDelivery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGeometryTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "CurrentLocation",
                table: "Riders",
                type: "geometry(point, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography(point, 4326)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Restaurants",
                type: "geometry(point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography(point, 4326)");

            migrationBuilder.AlterColumn<Point>(
                name: "DeliveryLocation",
                table: "DeliveryRequests",
                type: "geometry(point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography(point, 4326)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "CurrentLocation",
                table: "Riders",
                type: "geography(point, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry(point, 4326)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Restaurants",
                type: "geography(point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry(point, 4326)");

            migrationBuilder.AlterColumn<Point>(
                name: "DeliveryLocation",
                table: "DeliveryRequests",
                type: "geography(point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry(point, 4326)");
        }
    }
}
