using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EShop.Infrastructure.Data.Migrations
{
    // CHG-20261106-003-DATABASE-INFRASTRUCTURE
    // Purpose: Creates and seeds the initial SQLite schema.
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CreatedAt", "CustomerEmail", "CustomerName", "Status", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 30, 12, 0, 0, 0, DateTimeKind.Utc), "jordan.lee@example.com", "Jordan Lee", 1, null, 0 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2026, 11, 1, 12, 0, 0, 0, DateTimeKind.Utc), "taylor.morgan@example.com", "Taylor Morgan", 2, null, 0 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2026, 11, 4, 12, 0, 0, 0, DateTimeKind.Utc), "casey.patel@example.com", "Casey Patel", 0, null, 0 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 25, 12, 0, 0, 0, DateTimeKind.Utc), "riley.chen@example.com", "Riley Chen", 3, null, 0 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTime(2026, 11, 5, 12, 0, 0, 0, DateTimeKind.Utc), "avery.brooks@example.com", "Avery Brooks", 4, null, 0 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "Price", "SKU", "StockQuantity", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Accessories", new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), "Ergonomic wireless mouse with adjustable DPI.", true, "Wireless Mouse", 29.99m, "ACC-MOU-001", 45, null, 0 },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Accessories", new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), "Compact mechanical keyboard with tactile switches.", true, "Mechanical Keyboard", 89.50m, "ACC-KEY-002", 20, null, 0 },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Accessories", new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), "Seven-port USB-C hub for laptops and tablets.", true, "USB-C Hub", 54.00m, "ACC-HUB-003", 32, null, 0 },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Displays", new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), "QHD IPS monitor with an adjustable stand.", true, "27-inch Monitor", 279.00m, "DIS-MON-004", 12, null, 0 },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Accessories", new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), "Aluminum stand with adjustable height and angle.", true, "Laptop Stand", 39.95m, "ACC-STD-005", 28, null, 0 }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "CreatedAt", "OrderId", "ProductId", "ProductName", "Quantity", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0001-000000000001"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), "Wireless Mouse", 2, 29.99m, null },
                    { new Guid("30000000-0000-0000-0001-000000000002"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002"), "Mechanical Keyboard", 1, 89.50m, null },
                    { new Guid("30000000-0000-0000-0002-000000000001"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000004"), "27-inch Monitor", 1, 279.00m, null },
                    { new Guid("30000000-0000-0000-0003-000000000001"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000003"), "USB-C Hub", 1, 54.00m, null },
                    { new Guid("30000000-0000-0000-0003-000000000002"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000005"), "Laptop Stand", 1, 39.95m, null },
                    { new Guid("30000000-0000-0000-0004-000000000001"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000002"), "Mechanical Keyboard", 1, 89.50m, null },
                    { new Guid("30000000-0000-0000-0004-000000000002"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000003"), "USB-C Hub", 1, 54.00m, null },
                    { new Guid("30000000-0000-0000-0005-000000000001"), new DateTime(2026, 11, 6, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000001"), "Wireless Mouse", 1, 29.99m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerEmail",
                table: "Orders",
                column: "CustomerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_IsActive",
                table: "Products",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
