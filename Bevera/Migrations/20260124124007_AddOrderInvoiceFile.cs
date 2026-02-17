using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bevera.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInvoiceFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceContentType",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceCreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceFileName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvoiceFileSize",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceStoredFileName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceContentType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InvoiceCreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InvoiceFileName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InvoiceFileSize",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InvoiceStoredFileName",
                table: "Orders");
        }
    }
}
