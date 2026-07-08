using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 定义 AddBusinessTaskWorkingArea 类型。
    /// </summary>
    public partial class AddBusinessTaskWorkingArea : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkingArea",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkingArea",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}

