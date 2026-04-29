using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace glint_backend.Migrations
{
    /// <inheritdoc />
    public partial class RevertCascadeToNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses");

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses",
                column: "ResumeId",
                principalTable: "Resumes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses");

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses",
                column: "ResumeId",
                principalTable: "Resumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
