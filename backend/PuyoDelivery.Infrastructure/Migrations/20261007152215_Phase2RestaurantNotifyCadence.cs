using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuyoDelivery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RestaurantNotifyCadence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRestaurantNotifyAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantNotifyCount",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRestaurantNotifyAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RestaurantNotifyCount",
                table: "Orders");
        }
    }
}
