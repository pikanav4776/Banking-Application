using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bankingapplication.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    accountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    accountType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    routingNumber = table.Column<long>(type: "bigint", nullable: true),
                    accountNumber = table.Column<long>(type: "bigint", nullable: true),
                    accName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    accBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    email = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    homeAddress = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    SSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.accountId);
                });

            migrationBuilder.CreateTable(
                name: "CheckbookRecords",
                columns: table => new
                {
                    checkbookId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Payee = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    routingNumber = table.Column<long>(type: "bigint", nullable: false),
                    accountNumber = table.Column<long>(type: "bigint", nullable: false),
                    userAccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckbookRecords", x => x.checkbookId);
                    table.ForeignKey(
                        name: "FK_CheckbookRecords_Accounts_userAccountId",
                        column: x => x.userAccountId,
                        principalTable: "Accounts",
                        principalColumn: "accountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    serviceRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    accountNumber = table.Column<long>(type: "bigint", nullable: false),
                    requestor = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    requestType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dateSent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dateResponded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    accepted = table.Column<bool>(type: "bit", nullable: true),
                    userAccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.serviceRequestId);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Accounts_userAccountId",
                        column: x => x.userAccountId,
                        principalTable: "Accounts",
                        principalColumn: "accountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionRecords",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    accName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    accountNumber = table.Column<long>(type: "bigint", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    userAccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_TransactionRecords_Accounts_userAccountId",
                        column: x => x.userAccountId,
                        principalTable: "Accounts",
                        principalColumn: "accountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_accountNumber",
                table: "Accounts",
                column: "accountNumber",
                unique: true,
                filter: "[accountNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_username",
                table: "Accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckbookRecords_userAccountId",
                table: "CheckbookRecords",
                column: "userAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_userAccountId",
                table: "ServiceRequests",
                column: "userAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecords_userAccountId",
                table: "TransactionRecords",
                column: "userAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckbookRecords");

            migrationBuilder.DropTable(
                name: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "TransactionRecords");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
