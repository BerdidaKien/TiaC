using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Item_Serial",
                table: "EquipmentItems");

            migrationBuilder.DropIndex(
                name: "UQ_Transaction_Reservation",
                table: "BorrowingTransactions");

            migrationBuilder.AddColumn<string>(
                name: "account_status",
                table: "user",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<string>(
                name: "profile_image_path",
                table: "user",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "category_id",
                table: "EquipmentItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "category_review_note",
                table: "EquipmentItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_path",
                table: "EquipmentItems",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suggested_category",
                table: "EquipmentItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "info_change_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "pending"),
                    review_note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoChangeRequests", x => x.id);
                    table.ForeignKey(
                        name: "FK_InfoRequests_Reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_InfoRequests_User",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    setting_value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_by = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    updated_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.setting_key);
                });

            migrationBuilder.CreateTable(
                name: "info_change_request_files",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    stored_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    original_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_time = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoRequestFiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_InfoRequestFiles_Request",
                        column: x => x.request_id,
                        principalTable: "info_change_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "info_change_request_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    field_key = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    old_value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    new_value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    approved_value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoRequestItems", x => x.id);
                    table.ForeignKey(
                        name: "FK_InfoRequestItems_Request",
                        column: x => x.request_id,
                        principalTable: "info_change_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Item_Serial",
                table: "EquipmentItems",
                column: "serial_number",
                unique: true,
                filter: "([serial_number] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_Transaction_Reservation",
                table: "BorrowingTransactions",
                column: "reservation_id",
                unique: true,
                filter: "([reservation_id] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_info_change_request_files_request_id",
                table: "info_change_request_files",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_info_change_request_items_request_id",
                table: "info_change_request_items",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_info_change_requests_reviewed_by",
                table: "info_change_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_info_change_requests_user_id",
                table: "info_change_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "info_change_request_files");

            migrationBuilder.DropTable(
                name: "info_change_request_items");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "info_change_requests");

            migrationBuilder.DropIndex(
                name: "UQ_Item_Serial",
                table: "EquipmentItems");

            migrationBuilder.DropIndex(
                name: "UQ_Transaction_Reservation",
                table: "BorrowingTransactions");

            migrationBuilder.DropColumn(
                name: "account_status",
                table: "user");

            migrationBuilder.DropColumn(
                name: "profile_image_path",
                table: "user");

            migrationBuilder.DropColumn(
                name: "category_review_note",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "image_path",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "suggested_category",
                table: "EquipmentItems");

            migrationBuilder.AlterColumn<int>(
                name: "category_id",
                table: "EquipmentItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Item_Serial",
                table: "EquipmentItems",
                column: "serial_number",
                unique: true,
                filter: "[serial_number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Transaction_Reservation",
                table: "BorrowingTransactions",
                column: "reservation_id",
                unique: true,
                filter: "[reservation_id] IS NOT NULL");
        }
    }
}
