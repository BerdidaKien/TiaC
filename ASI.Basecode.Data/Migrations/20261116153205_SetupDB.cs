using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetupDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Penalties",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_id = table.Column<int>(type: "int", nullable: false),
                    borrower_id = table.Column<int>(type: "int", nullable: false),
                    penalty_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "unresolved"),
                    due_by = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    resolved_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    resolved_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penalties", x => x.id);
                    table.ForeignKey(
                        name: "FK_Penalties_Borrower",
                        column: x => x.borrower_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Penalties_ResolvedBy",
                        column: x => x.resolved_by,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Penalties_Transaction",
                        column: x => x.transaction_id,
                        principalTable: "BorrowingTransactions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_borrower_id",
                table: "Penalties",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_resolved_by",
                table: "Penalties",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_transaction_id",
                table: "Penalties",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Penalties");
        }
    }
}
