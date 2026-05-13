using MiniOrm.Attributes;
using MiniOrm.Data;
using MiniOrm.Models;
using Npgsql;
using System.Reflection;

namespace MiniOrm.Migrations.Commands;

public class MigrationRunner
{
    private readonly string _connStr;

    private readonly string _migrationFolder =
        Path.Combine(Directory.GetCurrentDirectory(), "Migrations");

    public MigrationRunner()
    {
        _connStr =
            Environment.GetEnvironmentVariable("MINIORM_CONN")
            ?? throw new Exception("MINIORM_CONN not found");

        if (!Directory.Exists(_migrationFolder))
            Directory.CreateDirectory(_migrationFolder);
    }

    // =========================================
    // ADD MIGRATION
    // =========================================

    public void AddMigration(string name)
    {
        EnsureMigrationTable();

        var allEntityTypes = Assembly.Load("MiniOrm")
            .GetTypes()
            .Where(t => t.GetCustomAttribute<TableAttribute>() != null)
            .ToList();

        var alreadyMigratedTables = GetAlreadyMigratedTableNames();

        var newEntityTypes = allEntityTypes
            .Where(t =>
            {
                var tableAttr = t.GetCustomAttribute<TableAttribute>();
                return !alreadyMigratedTables.Contains(tableAttr!.Name);
            })
            .ToList();

        if (newEntityTypes.Count == 0)
        {
            Console.WriteLine("No new entities found. Nothing to migrate.");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string fileName = $"{timestamp}_{name}.sql";
        string path = Path.Combine(_migrationFolder, fileName);

        // build one single -- up block and one single -- down block
        string sql = GenerateCombinedSql(newEntityTypes);

        File.WriteAllText(path, sql);
        Console.WriteLine($"Migration created: {fileName}");
    }

    // =========================================
    // APPLY MIGRATIONS
    // =========================================

    public void ApplyMigrations()
    {
        EnsureMigrationTable();

        var files = Directory.GetFiles(_migrationFolder, "*.sql")
            .OrderBy(f => f)
            .ToList();

        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);

            if (IsMigrationApplied(conn, fileName))
                continue;

            Console.WriteLine($"Applying {fileName}...");

            string content = File.ReadAllText(file);
            string upSql = ExtractAllUpBlocks(content);

            using var cmd = new NpgsqlCommand(upSql, conn);
            cmd.ExecuteNonQuery();

            using var insertCmd = new NpgsqlCommand(
                "INSERT INTO __migrations(filename) VALUES(@name)", conn);
            insertCmd.Parameters.AddWithValue("name", fileName);
            insertCmd.ExecuteNonQuery();

            Console.WriteLine($"Applied {fileName}");
        }
    }

    // =========================================
    // LIST MIGRATIONS
    // =========================================

    public void ListMigrations()
    {
        EnsureMigrationTable();

        var files = Directory.GetFiles(_migrationFolder, "*.sql")
            .OrderBy(f => f)
            .ToList();

        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            bool applied = IsMigrationApplied(conn, fileName);
            Console.WriteLine($"{fileName} [{(applied ? "applied" : "pending")}]");
        }
    }

    // =========================================
    // ROLLBACK
    // =========================================

    public void RollbackLastMigration()
    {
        EnsureMigrationTable();

        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();

        string getLastSql = @"
            SELECT filename
            FROM __migrations
            ORDER BY id DESC
            LIMIT 1";

        using var getCmd = new NpgsqlCommand(getLastSql, conn);
        var result = getCmd.ExecuteScalar();

        if (result == null)
        {
            Console.WriteLine("No migrations applied.");
            return;
        }

        string fileName = result.ToString()!;
        string filePath = Path.Combine(_migrationFolder, fileName);
        string content = File.ReadAllText(filePath);

        string downSql = ExtractAllDownBlocks(content);

        Console.WriteLine($"Rolling back {fileName}...");

        using var rollbackCmd = new NpgsqlCommand(downSql, conn);
        rollbackCmd.ExecuteNonQuery();

        using var deleteCmd = new NpgsqlCommand(
            "DELETE FROM __migrations WHERE filename=@name", conn);
        deleteCmd.Parameters.AddWithValue("name", fileName);
        deleteCmd.ExecuteNonQuery();

        Console.WriteLine("Rollback complete.");
    }

    // =========================================
    // HELPERS
    // =========================================

    private void EnsureMigrationTable()
    {
        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();

        string sql = @"
            CREATE TABLE IF NOT EXISTS __migrations
            (
                id SERIAL PRIMARY KEY,
                filename TEXT NOT NULL
            )";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private bool IsMigrationApplied(NpgsqlConnection conn, string fileName)
    {
        string sql = "SELECT COUNT(*) FROM __migrations WHERE filename=@name";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", fileName);
        long count = (long)cmd.ExecuteScalar()!;
        return count > 0;
    }

    private List<string> GetAlreadyMigratedTableNames()
    {
        var tableNames = new List<string>();

        var files = Directory.GetFiles(_migrationFolder, "*.sql")
            .OrderBy(f => f)
            .ToList();

        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            string upSection = ExtractAllUpBlocks(content);

            foreach (var line in upSection.Split('\n'))
            {
                var trimmed = line.Trim().ToLower();

                if (trimmed.StartsWith("create table"))
                {
                    var parts = trimmed.Split(' ',
                        StringSplitOptions.RemoveEmptyEntries);

                    // "create table if not exists tablename"  → index 5
                    // "create table tablename"                → index 2
                    string tableName = parts.Contains("exists")
                        ? parts[5].TrimEnd('(', ' ')
                        : parts[2].TrimEnd('(', ' ');

                    tableNames.Add(tableName);
                }
            }
        }

        return tableNames;
    }

    private string ExtractAllUpBlocks(string content)
    {
        var upStatements = new List<string>();

        // split by -- up to get each up section
        var sections = content.Split("-- up",
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var section in sections)
        {
            // take only the part before -- down
            string upPart = section.Split("-- down")[0].Trim();

            if (!string.IsNullOrWhiteSpace(upPart))
                upStatements.Add(upPart);
        }

        return string.Join("\n\n", upStatements);
    }

    private string ExtractAllDownBlocks(string content)
    {
        var parts = content.Split("-- down", StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            Console.WriteLine("No -- down section found in migration file.");
            return string.Empty;
        }
        string downSql = parts[1].Trim();

        return downSql;
    }

    // =========================================
    // SQL GENERATOR
    // =========================================

    private string GenerateCombinedSql(List<Type> entityTypes)
    {
        var upBlocks = new List<string>();
        var downBlocks = new List<string>();

        foreach (var type in entityTypes)
        {
            var metadata = TypeMapper.GetMetadata(type);
            var columns = new List<string>();

            foreach (var prop in metadata.Columns)
            {
                bool isPrimaryKey = prop == metadata.PrimaryKey;

                if (isPrimaryKey)
                {
                    columns.Add($"{GetColumnName(prop)} SERIAL PRIMARY KEY");
                }
                else
                {
                    string pgType = GetPostgresType(prop.PropertyType);
                    bool nullable = IsNullable(prop.PropertyType);
                    columns.Add($"{GetColumnName(prop)} {pgType} {(nullable ? "NULL" : "NOT NULL")}");
                }
            }

            upBlocks.Add($@"CREATE TABLE IF NOT EXISTS {metadata.TableName}
(
    {string.Join(",\n    ", columns)}
);");

            downBlocks.Add($"DROP TABLE IF EXISTS {metadata.TableName};");
        }

        // reverse down blocks so tables drop in reverse order
        downBlocks.Reverse();

        return $"-- up\n\n{string.Join("\n\n", upBlocks)}\n\n-- down\n\n{string.Join("\n\n", downBlocks)}";
    }

    private string GetColumnName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<MiniOrm.Attributes.ColumnAttribute>();
        return attr?.Name ?? prop.Name.ToLower();
    }

    private bool IsNullable(Type type)
    {
        if (!type.IsValueType)
            return true;

        return Nullable.GetUnderlyingType(type) != null;
    }

    private string GetPostgresType(Type type)
    {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(int)) return "INTEGER";
        if (actualType == typeof(long)) return "BIGINT";
        if (actualType == typeof(float)) return "REAL";
        if (actualType == typeof(double)) return "DOUBLE PRECISION";
        if (actualType == typeof(decimal)) return "NUMERIC";
        if (actualType == typeof(bool)) return "BOOLEAN";
        if (actualType == typeof(DateTime)) return "TIMESTAMP";
        if (actualType == typeof(Guid)) return "UUID";
        if (actualType == typeof(string)) return "TEXT";

        throw new Exception($"Unsupported type: {actualType.Name}");
    }
}