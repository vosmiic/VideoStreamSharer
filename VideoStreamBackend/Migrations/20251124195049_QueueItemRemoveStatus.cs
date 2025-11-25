using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamBackend.Migrations
{
    /// <inheritdoc />
    public partial class QueueItemRemoveStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Rooms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
