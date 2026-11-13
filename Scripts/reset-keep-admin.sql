/*
    Reset the TiaC database: delete all data except the Admin account(s).

    - Run it against the TiaC database (change the USE line, or pick the database in SSMS).
    - TRUNCATE can't be used on tables that other tables point to, so this uses DELETE in
      foreign key order, then restarts the ID counters.
    - Everything runs in one transaction: if any step fails, nothing is changed.
    - Run the "BEFORE" check first, then the whole script, then look at the "AFTER" result.
*/

-- USE [TiaC];
-- GO

SET NOCOUNT ON;

-- Set to 1 to keep these, 0 to wipe them
DECLARE @KeepCategories int = 0;   -- the app re-creates the 6 default categories on its next start when this table is empty
DECLARE @KeepSettings   int = 1;   -- borrowing rules (system_settings)

PRINT 'BEFORE';
SELECT id, user_id, role FROM [user] ORDER BY id;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Info change requests (items and files go with them)
    DELETE FROM info_change_request_items;
    DELETE FROM info_change_request_files;
    DELETE FROM info_change_requests;

    -- 2. Borrowing and reservations
    DELETE FROM BorrowingTransactions;
    DELETE FROM Reservations;

    -- 3. Equipment
    DELETE FROM EquipmentItems;
    IF @KeepCategories = 0 DELETE FROM EquipmentCategories;

    -- 4. Settings
    IF @KeepSettings = 0 DELETE FROM system_settings;

    -- 5. Every user except admins
    DELETE FROM [user] WHERE role <> 'Admin';

    -- 6. Restart ID counters so new rows start from 1 again
    DBCC CHECKIDENT ('info_change_request_items', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('info_change_request_files', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('info_change_requests',      RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('BorrowingTransactions',     RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('Reservations',              RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('EquipmentItems',            RESEED, 0) WITH NO_INFOMSGS;
    IF @KeepCategories = 0 DBCC CHECKIDENT ('EquipmentCategories', RESEED, 0) WITH NO_INFOMSGS;

    -- The admin keeps its id; the next new user continues after it
    DECLARE @maxUserId int = (SELECT ISNULL(MAX(id), 0) FROM [user]);
    DBCC CHECKIDENT ('[user]', RESEED, @maxUserId) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;
    PRINT 'Done.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Failed. Nothing was changed.';
    THROW;
END CATCH;

PRINT 'AFTER';
SELECT id, user_id, role FROM [user] ORDER BY id;
