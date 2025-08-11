using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoWebApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentFieldUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Todos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Todos");
        }
    }
}
