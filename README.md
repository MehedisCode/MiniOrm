# MiniOrm

A lightweight ORM (Object-Relational Mapper) built from scratch using ADO.NET and Npgsql on top of PostgreSQL. Inspired by Entity Framework Core — but built by hand to understand how ORMs work internally.

---

## What is this?

Most .NET developers use Entity Framework without knowing what happens under the hood. This project builds a simplified version of EF Core from scratch using raw ADO.NET — no EF, no Dapper, just Npgsql and C# reflection.

---

## Projects

| Project | Description |
|---|---|
| `MiniOrm` | The ORM library + a working demo in `Program.cs` |
| `MiniOrm.Migrations` | A CLI tool for generating and applying database migrations |

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) running locally or remotely
- Only NuGet dependency: `Npgsql`

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/MehedisCode/MiniOrm.git
cd MiniOrm
```

### 2. Create a PostgreSQL database

```sql
CREATE DATABASE miniorm;
```

### 3. Set the connection string as an environment variable

**Windows (Command Prompt):**
```cmd
set MINIORM_CONN=Host=localhost;Database=miniorm;Username=postgres;Password=yourpassword
```

**Windows (PowerShell):**
```powershell
$env:MINIORM_CONN="Host=localhost;Database=miniorm;Username=postgres;Password=yourpassword"
```

**Mac / Linux:**
```bash
export MINIORM_CONN="Host=localhost;Database=miniorm;Username=postgres;Password=yourpassword"
```
---

## Running Migrations

Navigate to the migrations project first:

```bash
cd MiniOrm.Migrations
```

### Generate a migration file

Scans all models with a `[Table]` attribute and generates a `.sql` file:

```bash
dotnet run -- migrations add InitialCreate
```

This creates a timestamped file like `20260513120000_InitialCreate.sql` inside the `Migrations/` folder.

### Apply pending migrations

Runs all `.sql` files that have not been applied yet:

```bash
dotnet run -- migrations apply
```

Applied migrations are recorded in a `__migrations` table in your database so they are never run twice.

### List all migrations

Shows which migrations are applied and which are pending:

```bash
dotnet run -- migrations list
```

Example output:
```
20260513120000_InitialCreate.sql [applied]
20260513130000_AddCustomer.sql   [pending]
```

### Rollback the last migration

Reverts the most recently applied migration using its `-- down` script:

```bash
dotnet run -- migrations rollback
```

---

## Running the Demo

The demo in `MiniOrm/Program.cs` walks through all CRUD operations against a live database.

Make sure migrations are applied first, then:

```bash
cd MiniOrm
dotnet run
```
---

## Project Structure
```
MiniOrm/
├── Attributes/
│   ├── TableAttribute.cs         # [Table("table_name")]
│   ├── ColumnAttribute.cs        # [Column("column_name")]
│   └── PrimaryKeyAttribute.cs    # [PrimaryKey]
├── Models/
│   ├── Product.cs
│   └── Order.cs
├── Data/
│   ├── DbContext.cs              # base class — manages Npgsql connection
│   ├── DbSet.cs                  # generic CRUD operations
│   ├── TypeMapper.cs             # reflection — maps C# types to Postgres types
│   └── EntityMetadata.cs        # holds table name, PK, and column info
├── AppDbContext.cs               # concrete context — registers DbSets
└── Program.cs                   # demo walkthrough

MiniOrm.Migrations/
├── Commands/
│   └── MigrationRunner.cs       # add / apply / list / rollback logic
└── Program.cs                   # CLI entry point
```
---

## How Entities Are Defined

```csharp
[Table("products")]
public class Product
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }  // nullable → NULL in Postgres

    [Column("in_stock")]
    public bool InStock { get; set; }
}
```

Only properties marked with `[Column]` or `[PrimaryKey]` are mapped. Everything else is ignored.

---

## Type Mapping

| C# Type | Postgres Type | Nullability |
|---|---|---|
| `int` (PrimaryKey) | `SERIAL` | `PRIMARY KEY` |
| `int` | `INTEGER` | `NOT NULL` |
| `long` | `BIGINT` | `NOT NULL` |
| `float` | `REAL` | `NOT NULL` |
| `double` | `DOUBLE PRECISION` | `NOT NULL` |
| `decimal` | `NUMERIC` | `NOT NULL` |
| `bool` | `BOOLEAN` | `NOT NULL` |
| `DateTime` | `TIMESTAMP` | `NOT NULL` |
| `Guid` | `UUID` | `NOT NULL` |
| `string` | `TEXT` | `NOT NULL` |
| `int?` | `INTEGER` | `NULL` |
| `decimal?` | `NUMERIC` | `NULL` |
| `bool?` | `BOOLEAN` | `NULL` |
| `DateTime?` | `TIMESTAMP` | `NULL` |
| `string?` | `TEXT` | `NULL` |

Nullable value types are detected at runtime using `Nullable.GetUnderlyingType()`.

---

## How the Migration CLI Works

1. `add` — uses reflection to scan the `MiniOrm` assembly for all classes with a `[Table]` attribute. Compares them against already-migrated tables and generates SQL only for new ones. Writes a single `.sql` file with one `-- up` and one `-- down` block.

2. `apply` — reads all `.sql` files in order, skips ones already recorded in `__migrations`, executes the `-- up` SQL, then records the filename.

3. `list` — reads all `.sql` files and checks each against the `__migrations` table.

4. `rollback` — finds the last entry in `__migrations`, runs its `-- down` SQL, then removes the record.

---

## Restrictions

- Only `Npgsql` is used as a third-party NuGet package
- No Entity Framework Core
- No Dapper
- No LINQ provider or lazy loading

---

## License

MIT
