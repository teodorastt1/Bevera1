using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bevera.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderHistoryRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "OrderStatusHistories",
                newName: "ChangedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Orders",
                newName: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChangedAt",
                table: "OrderStatusHistories",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ChangedAt",
                table: "Orders",
                newName: "CreatedAt");
        }
    }
}
