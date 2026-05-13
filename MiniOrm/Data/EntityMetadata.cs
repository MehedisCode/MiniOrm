using System.Reflection;

namespace MiniOrm.Data;

public class EntityMetadata
{
    public string TableName { get; set; }

    public PropertyInfo PrimaryKey { get; set; }

    public List<PropertyInfo> Columns { get; set; } = new();
}