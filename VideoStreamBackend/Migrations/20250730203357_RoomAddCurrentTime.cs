using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamBackend.Migrations
{
    /// <inheritdoc />
    public partial class RoomAddCurrentTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentTime",
                table: "Rooms",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentTime",
                table: "Rooms");
        }
    }
}
