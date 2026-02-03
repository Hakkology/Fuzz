using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fuzz.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddFuzzNorthwind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fuzz_Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryName = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Picture = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Customers",
                columns: table => new
                {
                    CustomerID = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactTitle = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    City = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Region = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Phone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Fax = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Customers", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Employees",
                columns: table => new
                {
                    EmployeeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TitleOfCourtesy = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    City = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Region = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    HomePhone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Extension = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Photo = table.Column<byte[]>(type: "bytea", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReportsTo = table.Column<int>(type: "integer", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Employees", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Fuzz_Employees_Fuzz_Employees_ReportsTo",
                        column: x => x.ReportsTo,
                        principalTable: "Fuzz_Employees",
                        principalColumn: "EmployeeID");
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Shippers",
                columns: table => new
                {
                    ShipperID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Phone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Shippers", x => x.ShipperID);
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactTitle = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    City = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Region = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Phone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Fax = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    HomePage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Suppliers", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Orders",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerID = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    EmployeeID = table.Column<int>(type: "integer", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShipVia = table.Column<int>(type: "integer", nullable: true),
                    Freight = table.Column<decimal>(type: "numeric", nullable: true),
                    ShipName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ShipAddress = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ShipCity = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    ShipRegion = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    ShipPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ShipCountry = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Orders", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Fuzz_Orders_Fuzz_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Fuzz_Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Fuzz_Orders_Fuzz_Employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Fuzz_Employees",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_Fuzz_Orders_Fuzz_Shippers_ShipVia",
                        column: x => x.ShipVia,
                        principalTable: "Fuzz_Shippers",
                        principalColumn: "ShipperID");
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SupplierID = table.Column<int>(type: "integer", nullable: true),
                    CategoryID = table.Column<int>(type: "integer", nullable: true),
                    QuantityPerUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    UnitsInStock = table.Column<short>(type: "smallint", nullable: true),
                    UnitsOnOrder = table.Column<short>(type: "smallint", nullable: true),
                    ReorderLevel = table.Column<short>(type: "smallint", nullable: true),
                    Discontinued = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Fuzz_Products_Fuzz_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Fuzz_Categories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_Fuzz_Products_Fuzz_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Fuzz_Suppliers",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "Fuzz_OrderDetails",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "integer", nullable: false),
                    ProductID = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<short>(type: "smallint", nullable: false),
                    Discount = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuzz_OrderDetails", x => new { x.OrderID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_Fuzz_OrderDetails_Fuzz_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Fuzz_Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fuzz_OrderDetails_Fuzz_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Fuzz_Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Employees_ReportsTo",
                table: "Fuzz_Employees",
                column: "ReportsTo");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_OrderDetails_ProductID",
                table: "Fuzz_OrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Orders_CustomerID",
                table: "Fuzz_Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Orders_EmployeeID",
                table: "Fuzz_Orders",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Orders_ShipVia",
                table: "Fuzz_Orders",
                column: "ShipVia");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Products_CategoryID",
                table: "Fuzz_Products",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Fuzz_Products_SupplierID",
                table: "Fuzz_Products",
                column: "SupplierID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fuzz_OrderDetails");

            migrationBuilder.DropTable(
                name: "Fuzz_Orders");

            migrationBuilder.DropTable(
                name: "Fuzz_Products");

            migrationBuilder.DropTable(
                name: "Fuzz_Customers");

            migrationBuilder.DropTable(
                name: "Fuzz_Employees");

            migrationBuilder.DropTable(
                name: "Fuzz_Shippers");

            migrationBuilder.DropTable(
                name: "Fuzz_Categories");

            migrationBuilder.DropTable(
                name: "Fuzz_Suppliers");
        }
    }
}
