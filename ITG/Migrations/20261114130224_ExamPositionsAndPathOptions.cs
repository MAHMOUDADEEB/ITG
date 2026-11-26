using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITG.Migrations
{
    /// <inheritdoc />
    public partial class ExamPositionsAndPathOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Certificates_CertificateId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_CertificateId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "CertificateId",
                table: "Exams");

            migrationBuilder.CreateTable(
                name: "EmployeeCareerPathOptions",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CareerPathId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCareerPathOptions", x => new { x.EmployeeId, x.CareerPathId });
                    table.ForeignKey(
                        name: "FK_EmployeeCareerPathOptions_CareerPaths_CareerPathId",
                        column: x => x.CareerPathId,
                        principalTable: "CareerPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCareerPathOptions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamPositions",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPositions", x => new { x.ExamId, x.PositionId });
                    table.ForeignKey(
                        name: "FK_ExamPositions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamPositions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCareerPathOptions_CareerPathId",
                table: "EmployeeCareerPathOptions",
                column: "CareerPathId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPositions_PositionId",
                table: "ExamPositions",
                column: "PositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeCareerPathOptions");

            migrationBuilder.DropTable(
                name: "ExamPositions");

            migrationBuilder.AddColumn<int>(
                name: "CertificateId",
                table: "Exams",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CertificateId",
                table: "Exams",
                column: "CertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Certificates_CertificateId",
                table: "Exams",
                column: "CertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
