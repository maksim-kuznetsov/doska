using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doska.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_subject",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_password",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Значение по умолчанию нужно только для заполнения существующих строк.
            // Дальше им управляет домен, поэтому убираем его из схемы.
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN has_password DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "ix_users_google_subject",
                table: "users",
                column: "google_subject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_google_subject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_subject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "has_password",
                table: "users");
        }
    }
}
