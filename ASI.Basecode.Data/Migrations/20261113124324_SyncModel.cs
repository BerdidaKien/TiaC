using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        // This migration and 20261113151435_UpdDBthree were made on two branches and both create
        // PasswordResetTokens. The table is created only if it isn't there yet, so the migrations
        // run in any order (a fresh database, or one that already has UpdDBthree).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PasswordResetTokensSql.CreateIfMissing);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // UpdDBthree owns the table; rolling back only this migration leaves it in place
        }
    }

    internal static class PasswordResetTokensSql
    {
        public const string CreateIfMissing = @"
IF OBJECT_ID(N'[PasswordResetTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY(1, 1),
        [UserId] varchar(50) NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        [CreatedTime] datetime2 NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id])
    );
END";

        public const string DropIfExists = @"
IF OBJECT_ID(N'[PasswordResetTokens]', N'U') IS NOT NULL
    DROP TABLE [PasswordResetTokens];";
    }
}
