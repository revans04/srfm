using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Services
{
    /// <summary>
    /// CRUD for entity-scoped budget groups (the first-class group taxonomy
    /// that replaced the legacy per-category "group" text column).
    /// </summary>
    public class GroupService
    {
        private readonly SupabaseDbService _db;
        private readonly ILogger<GroupService> _logger;

        public GroupService(SupabaseDbService db, ILogger<GroupService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<BudgetGroup>> GetGroups(string entityId)
        {
            if (!Guid.TryParse(entityId, out var eid))
                throw new ArgumentException($"Invalid entity ID: {entityId}");

            await using var conn = await _db.GetOpenConnectionAsync();
            const string sql = @"SELECT id, entity_id, name, sort_order, archived, kind, color, icon, collapsed_default
                                 FROM budget_groups
                                 WHERE entity_id=@eid
                                 ORDER BY sort_order, name";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eid", eid);
            await using var reader = await cmd.ExecuteReaderAsync();

            var groups = new List<BudgetGroup>();
            while (await reader.ReadAsync())
            {
                groups.Add(ReadGroup(reader));
            }
            return groups;
        }

        public async Task<BudgetGroup> CreateGroup(string entityId, BudgetGroup payload)
        {
            if (!Guid.TryParse(entityId, out var eid))
                throw new ArgumentException($"Invalid entity ID: {entityId}");
            if (string.IsNullOrWhiteSpace(payload.Name))
                throw new ArgumentException("Group name is required");

            await using var conn = await _db.GetOpenConnectionAsync();
            const string sql = @"INSERT INTO budget_groups (entity_id, name, kind, sort_order, color, icon, collapsed_default, archived)
                                 VALUES (@eid, @name, @kind,
                                         COALESCE(@sort_order, (SELECT COALESCE(max(sort_order),-1) + 1 FROM budget_groups WHERE entity_id=@eid)),
                                         @color, @icon, @collapsed_default, @archived)
                                 RETURNING id, entity_id, name, sort_order, archived, kind, color, icon, collapsed_default";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eid", eid);
            cmd.Parameters.AddWithValue("name", payload.Name.Trim());
            cmd.Parameters.AddWithValue("kind", string.IsNullOrWhiteSpace(payload.Kind) ? "expense" : payload.Kind);
            var sortParam = cmd.Parameters.Add("sort_order", NpgsqlDbType.Integer);
            sortParam.Value = payload.SortOrder > 0 ? (object)payload.SortOrder : DBNull.Value;
            cmd.Parameters.AddWithValue("color", (object?)payload.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("icon", (object?)payload.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("collapsed_default", payload.CollapsedDefault);
            cmd.Parameters.AddWithValue("archived", payload.Archived);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to insert budget_group");
            return ReadGroup(reader);
        }

        public async Task<BudgetGroup?> UpdateGroup(string entityId, string groupId, BudgetGroup payload)
        {
            if (!Guid.TryParse(entityId, out var eid))
                throw new ArgumentException($"Invalid entity ID: {entityId}");
            if (!Guid.TryParse(groupId, out var gid))
                throw new ArgumentException($"Invalid group ID: {groupId}");

            await using var conn = await _db.GetOpenConnectionAsync();
            const string sql = @"UPDATE budget_groups
                                    SET name = COALESCE(@name, name),
                                        kind = COALESCE(@kind, kind),
                                        color = @color,
                                        icon = @icon,
                                        collapsed_default = @collapsed_default,
                                        archived = @archived,
                                        sort_order = COALESCE(@sort_order, sort_order),
                                        updated_at = now()
                                  WHERE id=@gid AND entity_id=@eid
                                  RETURNING id, entity_id, name, sort_order, archived, kind, color, icon, collapsed_default";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gid", gid);
            cmd.Parameters.AddWithValue("eid", eid);
            cmd.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(payload.Name) ? (object)DBNull.Value : payload.Name.Trim());
            cmd.Parameters.AddWithValue("kind", string.IsNullOrWhiteSpace(payload.Kind) ? (object)DBNull.Value : payload.Kind);
            cmd.Parameters.AddWithValue("color", (object?)payload.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("icon", (object?)payload.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("collapsed_default", payload.CollapsedDefault);
            cmd.Parameters.AddWithValue("archived", payload.Archived);
            var sortParam = cmd.Parameters.Add("sort_order", NpgsqlDbType.Integer);
            sortParam.Value = payload.SortOrder > 0 ? (object)payload.SortOrder : DBNull.Value;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return ReadGroup(reader);
        }

        public async Task ReorderGroups(string entityId, List<string> groupIds)
        {
            if (!Guid.TryParse(entityId, out var eid))
                throw new ArgumentException($"Invalid entity ID: {entityId}");
            if (groupIds == null || groupIds.Count == 0) return;

            var guidIds = new List<Guid>(groupIds.Count);
            foreach (var idStr in groupIds)
            {
                if (!Guid.TryParse(idStr, out var g))
                    throw new ArgumentException($"Invalid group ID in reorder list: {idStr}");
                guidIds.Add(g);
            }

            await using var conn = await _db.GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            const string sql = "UPDATE budget_groups SET sort_order=@sort_order, updated_at=now() WHERE id=@gid AND entity_id=@eid";
            for (var i = 0; i < guidIds.Count; i++)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("gid", guidIds[i]);
                cmd.Parameters.AddWithValue("eid", eid);
                cmd.Parameters.AddWithValue("sort_order", i);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }

        public async Task<bool> DeleteGroup(string entityId, string groupId)
        {
            if (!Guid.TryParse(entityId, out var eid))
                throw new ArgumentException($"Invalid entity ID: {entityId}");
            if (!Guid.TryParse(groupId, out var gid))
                throw new ArgumentException($"Invalid group ID: {groupId}");

            await using var conn = await _db.GetOpenConnectionAsync();

            // Refuse to delete a group that still owns categories. Forces the
            // caller to reassign first; FK ON DELETE SET NULL would orphan rows
            // and break the NOT NULL invariant on group_id.
            const string countSql = "SELECT COUNT(*) FROM budget_categories WHERE group_id=@gid";
            await using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddWithValue("gid", gid);
                var count = (long)(await countCmd.ExecuteScalarAsync() ?? 0L);
                if (count > 0)
                    throw new InvalidOperationException(
                        $"Cannot delete group {gid}: {count} categories still reference it. Reassign or delete those first.");
            }

            const string sql = "DELETE FROM budget_groups WHERE id=@gid AND entity_id=@eid";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gid", gid);
            cmd.Parameters.AddWithValue("eid", eid);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// Apply a per-category sort_order list within a single budget. Each
        /// item provides the category id, its target group, and the desired
        /// 0-based position within that group. Lets the UI both reorder
        /// within a group and move a category to a different group in one
        /// call.
        /// </summary>
        public async Task ReorderCategories(string budgetId, CategoryReorderRequest payload)
        {
            if (payload?.Categories == null || payload.Categories.Count == 0) return;

            await using var conn = await _db.GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            const string sql = "UPDATE budget_categories SET group_id=@gid, sort_order=@sort_order WHERE id=@cid AND budget_id=@bid";
            foreach (var item in payload.Categories)
            {
                if (!Guid.TryParse(item.GroupId, out var gid))
                    throw new ArgumentException($"Invalid group ID in reorder list: {item.GroupId}");
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("cid", item.Id);
                cmd.Parameters.AddWithValue("bid", budgetId);
                cmd.Parameters.AddWithValue("gid", gid);
                cmd.Parameters.AddWithValue("sort_order", item.SortOrder);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }

        private static BudgetGroup ReadGroup(NpgsqlDataReader reader) => new BudgetGroup
        {
            Id = reader.GetGuid(0).ToString(),
            EntityId = reader.GetGuid(1).ToString(),
            Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            SortOrder = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            Archived = !reader.IsDBNull(4) && reader.GetBoolean(4),
            Kind = reader.IsDBNull(5) ? "expense" : reader.GetString(5),
            Color = reader.IsDBNull(6) ? null : reader.GetString(6),
            Icon = reader.IsDBNull(7) ? null : reader.GetString(7),
            CollapsedDefault = !reader.IsDBNull(8) && reader.GetBoolean(8),
        };
    }
}
