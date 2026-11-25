using System.Data;
using Chinnaphat_8272_L005.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinnaphat_8272_L005.Database;

public static class BookingRefundSchemaInitializer
{
    public static async Task EnsureAsync(Csi402dbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS booking_refund_requests (
                request_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                booking_id INT UNSIGNED NULL, queue_id INT UNSIGNED NULL, customer_user_id INT UNSIGNED NULL,
                reference_code VARCHAR(60) NOT NULL, customer_name VARCHAR(150) NOT NULL, customer_phone VARCHAR(30) NULL,
                service_date DATE NOT NULL, amount DECIMAL(10,2) NOT NULL, reason VARCHAR(500) NOT NULL,
                status VARCHAR(30) NOT NULL DEFAULT 'PENDING_SALES', sales_note VARCHAR(500) NULL,
                sales_reviewed_by INT UNSIGNED NULL, sales_reviewed_at DATETIME NULL, accounting_note VARCHAR(500) NULL,
                refund_method VARCHAR(40) NULL, refund_reference VARCHAR(100) NULL,
                processed_by INT UNSIGNED NULL, processed_at DATETIME NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (request_id), KEY ix_booking_refund_booking (booking_id), KEY ix_booking_refund_queue (queue_id),
                KEY ix_booking_refund_customer (customer_user_id), KEY ix_booking_refund_status (status)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
            await cmd.ExecuteNonQueryAsync();
            foreach (var (columnName, definition) in new[] { ("booking_item_id", "INT UNSIGNED NULL"), ("sale_id", "INT UNSIGNED NULL"), ("product_id", "INT UNSIGNED NULL"), ("product_name", "VARCHAR(200) NULL"), ("product_quantity", "INT UNSIGNED NULL") })
            {
                await using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'booking_refund_requests' AND COLUMN_NAME = @name";
                var parameter = check.CreateParameter(); parameter.ParameterName = "@name"; parameter.Value = columnName; check.Parameters.Add(parameter);
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) continue;
                await using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE booking_refund_requests ADD COLUMN {columnName} {definition}";
                await alter.ExecuteNonQueryAsync();
            }
        }
        finally { if (openedHere) await connection.CloseAsync(); }
    }
}
