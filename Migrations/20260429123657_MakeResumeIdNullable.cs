using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace glint_backend.Migrations
{
    /// <inheritdoc />
    public partial class MakeResumeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses");

            migrationBuilder.AlterColumn<Guid>(
                name: "ResumeId",
                table: "Analyses",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ResumeId",
                table: "Analyses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_Resumes_ResumeId",
                table: "Analyses",
                column: "ResumeId",
                principalTable: "Resumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
