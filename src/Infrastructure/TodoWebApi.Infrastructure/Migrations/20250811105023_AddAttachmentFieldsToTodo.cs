using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoWebApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentFieldsToTodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Todos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "Todos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Todos");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "Todos");
        }
    }
}
