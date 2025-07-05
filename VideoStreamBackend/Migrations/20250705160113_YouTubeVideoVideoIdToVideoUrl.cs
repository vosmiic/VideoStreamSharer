using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamBackend.Migrations
{
    /// <inheritdoc />
    public partial class YouTubeVideoVideoIdToVideoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoId",
                table: "QueueItems",
                newName: "VideoUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "QueueItems",
                newName: "VideoId");
        }
    }
}
