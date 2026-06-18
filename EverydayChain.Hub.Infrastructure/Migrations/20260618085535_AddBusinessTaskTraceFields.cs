using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskTraceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickLocation",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderId",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "PickLocation",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "StoreId",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "StoreName",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}
