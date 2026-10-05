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
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Riders_CurrentLocation";
                DROP INDEX IF EXISTS "IX_Restaurants_Location";
                DROP INDEX IF EXISTS "IX_DeliveryRequests_DeliveryLocation";

                ALTER TABLE "Riders" ALTER COLUMN "CurrentLocation" TYPE geometry(Point, 4326)
                    USING st_setsrid("CurrentLocation"::geometry, 4326);
                ALTER TABLE "Restaurants" ALTER COLUMN "Location" TYPE geometry(Point, 4326)
                    USING st_setsrid("Location"::geometry, 4326);
                ALTER TABLE "DeliveryRequests" ALTER COLUMN "DeliveryLocation" TYPE geometry(Point, 4326)
                    USING st_setsrid("DeliveryLocation"::geometry, 4326);

                CREATE INDEX "IX_Riders_CurrentLocation" ON "Riders" USING gist ("CurrentLocation");
                CREATE INDEX "IX_Restaurants_Location" ON "Restaurants" USING gist ("Location");
                CREATE INDEX "IX_DeliveryRequests_DeliveryLocation" ON "DeliveryRequests" USING gist ("DeliveryLocation");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Riders" ALTER COLUMN "CurrentLocation" TYPE geography(Point, 4326)
                    USING st_setsrid("CurrentLocation"::geometry, 4326)::geography;
                ALTER TABLE "Restaurants" ALTER COLUMN "Location" TYPE geography(Point, 4326)
                    USING st_setsrid("Location"::geometry, 4326)::geography;
                ALTER TABLE "DeliveryRequests" ALTER COLUMN "DeliveryLocation" TYPE geography(Point, 4326)
                    USING st_setsrid("DeliveryLocation"::geometry, 4326)::geography;
                """);
        }
    }
}
