using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamBackend.Migrations
{
    /// <inheritdoc />
    public partial class QueueItemAddProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "QueueItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "QueueItems");
        }
    }
}
