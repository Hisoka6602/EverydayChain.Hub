using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskUpdatedTimeLocalIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "UpdatedTimeLocal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}
