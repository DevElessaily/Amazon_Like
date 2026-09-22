using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace portofolio3.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SellerProducts",
                columns: table => new
                {
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerProducts", x => new { x.SellerId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_SellerProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerProducts_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerProducts_ProductId",
                table: "SellerProducts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerProducts");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Products");
        }
    }
}
