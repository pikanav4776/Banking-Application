using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bankingapplication.Migrations
{
    /// <inheritdoc />
    public partial class HashSsnAndDecimalBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SSN",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TransactionRecords",
                newName: "transactionId");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "Accounts",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssnHash",
                table: "Accounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ssnHash",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "transactionId",
                table: "TransactionRecords",
                newName: "id");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "Accounts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SSN",
                table: "Accounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
