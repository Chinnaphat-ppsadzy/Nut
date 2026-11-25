using System.Data;
using Chinnaphat_8272_L005.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinnaphat_8272_L005.Database;

public static class WalkInPaymentSchemaInitializer
{
    public static async Task EnsurePromotionDaysColumnAsync(Csi402dbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'promotions' AND COLUMN_NAME = 'active_days'";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0)
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE promotions ADD COLUMN active_days VARCHAR(32) NOT NULL DEFAULT '1,2,3,4,5' AFTER time_end";
                await alter.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    public static async Task EnsureCourtIssueBlockColumnAsync(Csi402dbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'court_issues' AND COLUMN_NAME = 'maintenance_block_id'";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0)
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE court_issues ADD COLUMN maintenance_block_id INT UNSIGNED NULL AFTER resolved_by";
                await alter.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    public static async Task EnsureRentalDepositRefundTableAsync(Csi402dbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            await using var create = connection.CreateCommand();
            create.CommandText = @"CREATE TABLE IF NOT EXISTS rental_deposit_refund_requests (
                request_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                booking_item_id INT UNSIGNED NULL,
                sale_id INT UNSIGNED NULL,
                product_id INT UNSIGNED NOT NULL,
                receipt_code VARCHAR(40) NOT NULL,
                customer_name VARCHAR(150) NOT NULL,
                customer_phone VARCHAR(20) NULL,
                refund_destination VARCHAR(100) NULL,
                product_name VARCHAR(200) NOT NULL,
                quantity INT NOT NULL,
                deposit_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                damage_description VARCHAR(500) NULL,
                fine_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                refunded_amount DECIMAL(10,2) NULL,
                refund_method VARCHAR(30) NULL,
                refund_reference VARCHAR(100) NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
                requested_by INT UNSIGNED NOT NULL,
                processed_by INT UNSIGNED NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at DATETIME NULL,
                PRIMARY KEY (request_id),
                KEY ix_rental_refund_status (status),
                KEY ix_rental_refund_booking_item (booking_item_id),
                KEY ix_rental_refund_sale (sale_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
            await create.ExecuteNonQueryAsync();

            var requiredColumns = new (string Name, string Definition)[]
            {
                ("refund_destination", "VARCHAR(100) NULL"),
                ("refund_method", "VARCHAR(30) NULL"),
                ("refund_reference", "VARCHAR(100) NULL"),
                ("damage_photo_url", "VARCHAR(255) NULL"),
                ("damage_assessment_status", "VARCHAR(20) NULL"),
                ("damage_assessed_by", "INT UNSIGNED NULL"),
                ("damage_assessed_at", "DATETIME NULL")
            };
            foreach (var column in requiredColumns)
            {
                await using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rental_deposit_refund_requests' AND COLUMN_NAME = @columnName";
                var parameter = check.CreateParameter();
                parameter.ParameterName = "@columnName";
                parameter.Value = column.Name;
                check.Parameters.Add(parameter);
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) continue;

                await using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE rental_deposit_refund_requests ADD COLUMN {column.Name} {column.Definition}";
                await alter.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    private static readonly (string Name, string Definition)[] Columns =
    {
        ("court_fee", "DECIMAL(10,2) NOT NULL DEFAULT 0.00"),
        ("payment_status", "ENUM('UNPAID','PENDING_VERIFY','REJECTED','PAID') NOT NULL DEFAULT 'UNPAID'"),
        ("payment_method", "VARCHAR(20) NULL"),
        ("paid_at", "DATETIME NULL"),
        ("paid_by", "INT UNSIGNED NULL"),
        ("slip_image_url", "VARCHAR(255) NULL")
    };

    public static async Task EnsureColumnsAsync(Csi402dbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync();

        try
        {
            foreach (var column in Columns)
            {
                await using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'walk_in_queues' AND COLUMN_NAME = @columnName";
                var parameter = check.CreateParameter();
                parameter.ParameterName = "@columnName";
                parameter.Value = column.Name;
                check.Parameters.Add(parameter);

                var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
                if (exists) continue;

                await using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE walk_in_queues ADD COLUMN {column.Name} {column.Definition}";
                await alter.ExecuteNonQueryAsync();
            }

            await using var updateStatus = connection.CreateCommand();
            updateStatus.CommandText = "ALTER TABLE walk_in_queues MODIFY COLUMN payment_status ENUM('UNPAID','PENDING_VERIFY','REJECTED','PAID') NOT NULL DEFAULT 'UNPAID'";
            await updateStatus.ExecuteNonQueryAsync();

            await using var createPosSales = connection.CreateCommand();
            createPosSales.CommandText = @"CREATE TABLE IF NOT EXISTS pos_sale_transactions (
                sale_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                receipt_code VARCHAR(40) NOT NULL,
                customer_name VARCHAR(150) NOT NULL,
                customer_phone VARCHAR(20) NULL,
                items_summary TEXT NOT NULL,
                sale_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                rental_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                discount_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                net_total DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                payment_method ENUM('CASH','QR') NOT NULL,
                payment_status ENUM('PENDING_VERIFY','PAID','REJECTED','VOID') NOT NULL DEFAULT 'PENDING_VERIFY',
                paid_at DATETIME NULL,
                created_by INT UNSIGNED NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                items_json LONGTEXT NULL,
                slip_image_url VARCHAR(255) NULL,
                verified_by INT UNSIGNED NULL,
                verified_at DATETIME NULL,
                reject_reason VARCHAR(500) NULL,
                PRIMARY KEY (sale_id),
                UNIQUE KEY uq_pos_sale_receipt (receipt_code),
                KEY ix_pos_sale_paid_at (paid_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
            await createPosSales.ExecuteNonQueryAsync();

            var posColumns = new (string Name, string Definition)[]
            {
                ("items_json", "LONGTEXT NULL"), ("slip_image_url", "VARCHAR(255) NULL"),
                ("verified_by", "INT UNSIGNED NULL"), ("verified_at", "DATETIME NULL"), ("reject_reason", "VARCHAR(500) NULL")
            };
            foreach (var column in posColumns)
            {
                await using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pos_sale_transactions' AND COLUMN_NAME = @columnName";
                var parameter = check.CreateParameter(); parameter.ParameterName = "@columnName"; parameter.Value = column.Name; check.Parameters.Add(parameter);
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) continue;
                await using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE pos_sale_transactions ADD COLUMN {column.Name} {column.Definition}";
                await alter.ExecuteNonQueryAsync();
            }
            await using var updatePosSchema = connection.CreateCommand();
            updatePosSchema.CommandText = "ALTER TABLE pos_sale_transactions MODIFY COLUMN payment_status ENUM('PENDING_VERIFY','PAID','REJECTED','VOID') NOT NULL DEFAULT 'PENDING_VERIFY', MODIFY COLUMN paid_at DATETIME NULL";
            await updatePosSchema.ExecuteNonQueryAsync();

            await EnsurePromotionDaysColumnAsync(db);
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
