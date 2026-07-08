using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 定义 AddBusinessTaskUpdatedTimeLocalIndex 类型。
    /// </summary>
    public partial class AddBusinessTaskUpdatedTimeLocalIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "UpdatedTimeLocal");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}

