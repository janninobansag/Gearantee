using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    /// <summary>
    /// Provides the per-item SQL Server lock shared by inventory and reservation workflows.
    /// Call within a transaction, before checking reservation state or changing inventory.
    /// </summary>
    public static class EquipmentItemConcurrencyLock
    {
        public static async Task<EquipmentItem> LoadForUpdateAsync(
            AsiBasecodeDBContext db,
            long equipmentId,
            bool includeArchived = false)
        {
            var transaction = db.Database.CurrentTransaction;
            if (transaction == null ||
                transaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
            {
                throw new InvalidOperationException(
                    "Equipment must be locked inside an active SERIALIZABLE transaction.");
            }

            if (db.Database.IsSqlServer())
            {
                var query = includeArchived
                    ? db.EquipmentItems.FromSqlInterpolated($"""
                        SELECT *
                        FROM [EquipmentItem] WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                        WHERE [EquipmentId] = {equipmentId}
                        """)
                    : db.EquipmentItems.FromSqlInterpolated($"""
                        SELECT *
                        FROM [EquipmentItem] WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                        WHERE [EquipmentId] = {equipmentId} AND [IsArchived] = 0
                        """);

                return await query.SingleOrDefaultAsync();
            }

            return await db.EquipmentItems.SingleOrDefaultAsync(item =>
                item.EquipmentId == equipmentId && (includeArchived || !item.IsArchived));
        }
    }
}
