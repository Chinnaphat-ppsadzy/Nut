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
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
