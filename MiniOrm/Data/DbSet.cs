using MiniOrm.Attributes;
using Npgsql;
using System.Reflection;

namespace MiniOrm.Data;

public class DbSet<T> where T : new()
{
    private readonly DbContext _context;
    private readonly EntityMetadata _metadata;

    public DbSet(DbContext context)
    {
        _context = context;
        _metadata = TypeMapper.GetMetadata<T>();
    }

    // =====================================
    // INSERT
    // =====================================

    public int Insert(T entity)
    {
        var columns = _metadata.Columns
            .Where(c => c != _metadata.PrimaryKey)
            .ToList();

        var columnNames = columns
            .Select(GetColumnName);

        var parameterNames = columns
            .Select(c => $"@{c.Name}");

        string sql = $@"
            INSERT INTO {_metadata.TableName}
            ({string.Join(",", columnNames)})
            VALUES ({string.Join(",", parameterNames)})
            RETURNING id";

        using var conn = _context.CreateConnection();

        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);

        foreach (var col in columns)
        {
            object value =
                col.GetValue(entity) ?? DBNull.Value;

            cmd.Parameters.AddWithValue(col.Name, value);
        }

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // =====================================
    // FIND BY ID
    // =====================================

    public T? FindById(int id)
    {
        string sql = $@"
            SELECT *
            FROM {_metadata.TableName}
            WHERE id=@id";

        using var conn = _context.CreateConnection();

        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return default;

        T entity = new T();

        foreach (var prop in _metadata.Columns)
        {
            string columnName = GetColumnName(prop);

            object value = reader[columnName];

            if (value == DBNull.Value)
            {
                prop.SetValue(entity, null);
            }
            else
            {
                prop.SetValue(entity, value);
            }
        }

        return entity;
    }

    // =====================================
    // GET ALL
    // =====================================

    public List<T> GetAll()
    {
        string sql =
            $"SELECT * FROM {_metadata.TableName}";

        using var conn = _context.CreateConnection();

        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);

        using var reader = cmd.ExecuteReader();

        List<T> list = new();

        while (reader.Read())
        {
            T entity = new T();

            foreach (var prop in _metadata.Columns)
            {
                string columnName = GetColumnName(prop);

                object value = reader[columnName];

                if (value == DBNull.Value)
                {
                    prop.SetValue(entity, null);
                }
                else
                {
                    prop.SetValue(entity, value);
                }
            }

            list.Add(entity);
        }

        return list;
    }

    // =====================================
    // UPDATE
    // =====================================

    public void Update(T entity)
    {
        var columns = _metadata.Columns
            .Where(c => c != _metadata.PrimaryKey)
            .ToList();

        string setClause = string.Join(",",
            columns.Select(c =>
                $"{GetColumnName(c)}=@{c.Name}"));

        string sql = $@"
            UPDATE {_metadata.TableName}
            SET {setClause}
            WHERE id=@id";

        using var conn = _context.CreateConnection();

        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);

        foreach (var col in columns)
        {
            object value =
                col.GetValue(entity) ?? DBNull.Value;

            cmd.Parameters.AddWithValue(col.Name, value);
        }

        object id = _metadata.PrimaryKey.GetValue(entity);

        cmd.Parameters.AddWithValue("id", id);

        cmd.ExecuteNonQuery();
    }

    // =====================================
    // DELETE
    // =====================================

    public void Delete(int id)
    {
        string sql = $@"
            DELETE FROM {_metadata.TableName}
            WHERE id=@id";

        using var conn = _context.CreateConnection();

        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);

        cmd.ExecuteNonQuery();
    }

    // =====================================
    // HELPER
    // =====================================

    private string GetColumnName(PropertyInfo prop)
    {
        var attr =
            prop.GetCustomAttribute<ColumnAttribute>();

        return attr?.Name ?? prop.Name.ToLower();
    }
}