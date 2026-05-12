using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace glint_backend.Migrations
{
    /// <inheritdoc />
    public partial class JobArchiveAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "JobAdvertisements",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "JobAdvertisements");
        }
    }
}
