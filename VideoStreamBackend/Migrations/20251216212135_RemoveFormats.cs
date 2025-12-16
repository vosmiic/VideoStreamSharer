using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioFormatId",
                table: "QueueItems");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "QueueItems");

            migrationBuilder.DropColumn(
                name: "VideoFormatId",
                table: "QueueItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioFormatId",
                table: "QueueItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "QueueItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFormatId",
                table: "QueueItems",
                type: "TEXT",
                nullable: true);
        }
    }
}
