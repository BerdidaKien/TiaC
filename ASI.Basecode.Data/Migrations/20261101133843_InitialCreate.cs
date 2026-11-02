using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentCategories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    category_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    updated_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Equipmen__3213E83FB3B8F0CA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "user"),
                    first_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    middle_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    contact_number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    borrower_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    updated_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user__3213E83F202ED11A", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    item_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    brand_model = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    serial_number = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    condition_rating = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "good"),
                    storage_location = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "available"),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    updated_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Equipmen__3213E83F8AE4C201", x => x.id);
                    table.ForeignKey(
                        name: "FK_EquipmentItems_Categories",
                        column: x => x.category_id,
                        principalTable: "EquipmentCategories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    borrower_id = table.Column<int>(type: "int", nullable: false),
                    equipment_item_id = table.Column<int>(type: "int", nullable: false),
                    requested_pickup_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    estimated_return_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "pending"),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    review_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Reservat__3213E83F95CD2E81", x => x.id);
                    table.ForeignKey(
                        name: "FK_Reservations_Borrower",
                        column: x => x.borrower_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Reservations_Item",
                        column: x => x.equipment_item_id,
                        principalTable: "EquipmentItems",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Reservations_Reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "BorrowingTransactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reservation_id = table.Column<int>(type: "int", nullable: true),
                    equipment_item_id = table.Column<int>(type: "int", nullable: false),
                    borrower_id = table.Column<int>(type: "int", nullable: false),
                    released_by = table.Column<int>(type: "int", nullable: false),
                    actual_pickup_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    returned_to = table.Column<int>(type: "int", nullable: true),
                    actual_return_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    return_condition = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    damage_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_overdue = table.Column<bool>(type: "bit", nullable: false),
                    delay_duration_hours = table.Column<int>(type: "int", nullable: false),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Borrowin__3213E83F58D0F761", x => x.id);
                    table.ForeignKey(
                        name: "FK_Transactions_Borrower",
                        column: x => x.borrower_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Transactions_Item",
                        column: x => x.equipment_item_id,
                        principalTable: "EquipmentItems",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Transactions_ReleasedBy",
                        column: x => x.released_by,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Transactions_Reservation",
                        column: x => x.reservation_id,
                        principalTable: "Reservations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Transactions_ReturnedTo",
                        column: x => x.returned_to,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowingTransactions_borrower_id",
                table: "BorrowingTransactions",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowingTransactions_equipment_item_id",
                table: "BorrowingTransactions",
                column: "equipment_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowingTransactions_released_by",
                table: "BorrowingTransactions",
                column: "released_by");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowingTransactions_returned_to",
                table: "BorrowingTransactions",
                column: "returned_to");

            migrationBuilder.CreateIndex(
                name: "UQ_Transaction_Reservation",
                table: "BorrowingTransactions",
                column: "reservation_id",
                unique: true,
                filter: "[reservation_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Category_Code",
                table: "EquipmentCategories",
                column: "category_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_category_id",
                table: "EquipmentItems",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Item_Code",
                table: "EquipmentItems",
                column: "item_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Item_Serial",
                table: "EquipmentItems",
                column: "serial_number",
                unique: true,
                filter: "[serial_number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_borrower_id",
                table: "Reservations",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_equipment_item_id",
                table: "Reservations",
                column: "equipment_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_reviewed_by",
                table: "Reservations",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "UQ_User_Email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_User_UserId",
                table: "user",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BorrowingTransactions");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "EquipmentItems");

            migrationBuilder.DropTable(
                name: "EquipmentCategories");
        }
    }
}
