using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolEduERP.App_Data
{
    /// <inheritdoc />
    public partial class FixUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_ClassSections_ClassSectionId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_ClassName_Section",
                table: "ClassSections",
                columns: new[] { "ClassName", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_ClassSectionId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "ClassSectionId", "Date" },
                unique: true,
                filter: "[ClassSectionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ClassSections_ClassSectionId",
                table: "AttendanceRecords",
                column: "ClassSectionId",
                principalTable: "ClassSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_ClassSections_ClassSectionId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClassSections_ClassName_Section",
                table: "ClassSections");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_ClassSectionId_Date",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ClassSections_ClassSectionId",
                table: "AttendanceRecords",
                column: "ClassSectionId",
                principalTable: "ClassSections",
                principalColumn: "Id");
        }
    }
}
