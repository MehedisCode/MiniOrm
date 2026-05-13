using MiniOrm.Migrations.Commands;

var runner = new MigrationRunner();

if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("dotnet run -- migrations add <Name>");
    Console.WriteLine("dotnet run -- migrations apply");
    Console.WriteLine("dotnet run -- migrations list");
    Console.WriteLine("dotnet run -- migrations rollback");
    return;
}

string command = args[0];
string action = args[1];

if (command != "migrations")
{
    Console.WriteLine("Unknown command");
    return;
}

switch (action)
{
    case "add":

        if (args.Length < 3)
        {
            Console.WriteLine("Migration name required");
            return;
        }

        runner.AddMigration(args[2]);
        break;

    case "apply":
        runner.ApplyMigrations();
        break;

    case "list":
        runner.ListMigrations();
        break;

    case "rollback":
        runner.RollbackLastMigration();
        break;

    default:
        Console.WriteLine("Unknown action");
        break;
}