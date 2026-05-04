using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace glint_backend.Migrations
{
    /// <inheritdoc />
    public partial class labelToJobTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                table: "Analyses",
                newName: "JobTitle");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "JobAdvertisements",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "JobAdvertisements");

            migrationBuilder.RenameColumn(
                name: "JobTitle",
                table: "Analyses",
                newName: "Label");
        }
    }
}
