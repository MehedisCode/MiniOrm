using MiniOrm.Attributes;
using System.Reflection;

namespace MiniOrm.Data;

public static class TypeMapper
{
    // your existing method stays exactly as is
    public static EntityMetadata GetMetadata<T>()
    {
        return GetMetadata(typeof(T));
    }

    // new method that accepts a Type at runtime
    public static EntityMetadata GetMetadata(Type type)
    {
        var tableAttr = type.GetCustomAttribute<TableAttribute>();

        var metadata = new EntityMetadata
        {
            TableName = tableAttr!.Name
        };

        foreach (var prop in type.GetProperties())
        {
            bool isPrimaryKey = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null;
            bool hasColumn = prop.GetCustomAttribute<ColumnAttribute>() != null;

            if (!isPrimaryKey && !hasColumn)
                continue;

            metadata.Columns.Add(prop);

            if (isPrimaryKey)
                metadata.PrimaryKey = prop;
        }

        return metadata;
    }
}