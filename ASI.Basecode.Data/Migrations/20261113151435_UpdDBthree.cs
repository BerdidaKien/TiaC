using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdDBthree : Migration
    {
        // Same table as 20261113124324_SyncModel (made on another branch), so it is created
        // only if it isn't there yet. See PasswordResetTokensSql in that file.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PasswordResetTokensSql.CreateIfMissing);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PasswordResetTokensSql.DropIfExists);
        }
    }
}
