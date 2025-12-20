using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace az_backend_new.Migrations
{
    /// <inheritdoc />
    public partial class CleanTraineeCertificateStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificatesNew");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_SerialNumber",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "PersonName",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "Certificates");

            migrationBuilder.AddColumn<int>(
                name: "TraineeId",
                table: "Certificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$BlUc0du4o071hjUXPqWLQevcUwtNDzFHoSM4KyPSSQ5EpJwpXs3zi");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TraineeId_ServiceMethod",
                table: "Certificates",
                columns: new[] { "TraineeId", "ServiceMethod" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Trainees_TraineeId",
                table: "Certificates",
                column: "TraineeId",
                principalTable: "Trainees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Trainees_TraineeId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_TraineeId_ServiceMethod",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "TraineeId",
                table: "Certificates");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Certificates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonName",
                table: "Certificates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Certificates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Certificates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "Certificates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificatesNew",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraineeId = table.Column<int>(type: "integer", nullable: false),
                    CertificateType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ServiceMethod = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificatesNew", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificatesNew_Trainees_TraineeId",
                        column: x => x.TraineeId,
                        principalTable: "Trainees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$r34gMUK1CtvViNy3.TSmmO29gYaQP/rlMrbtFxD4VUwLn8WVKcQE6");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_SerialNumber",
                table: "Certificates",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificatesNew_TraineeId_ServiceMethod",
                table: "CertificatesNew",
                columns: new[] { "TraineeId", "ServiceMethod" },
                unique: true);
        }
    }
}
