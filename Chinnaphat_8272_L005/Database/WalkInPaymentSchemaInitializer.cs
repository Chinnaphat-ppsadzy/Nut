using System.Data;
using Chinnaphat_8272_L005.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinnaphat_8272_L005.Database;

public static class WalkInPaymentSchemaInitializer
{
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
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
