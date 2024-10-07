using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Types;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace lets_break_razor.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnGetLocalTodoComments()
    {
        string cwd = Directory.GetCurrentDirectory();
        string personal_dir = cwd.AsDirectory().GoUpToDirectory("personal").FullName;
        string todo_comment_pattern = @"//\s*todo:?(?<content>.*)$";
        string readme_todo_pattern = @"^(?<spacing>\s*)(?<checkbox>-\s*\[\s?\])\s*(?<content>.*)$"; // https://regex101.com/r/ez0van/2

        var comment_todos_search = new Grepper
        {
            RootPath = personal_dir,
            Recursive = true,
            FileSearchMask = "*.cs*",
            FileSearchLinePattern = todo_comment_pattern,
        }
            .GetMatchingFiles()
            .ToList();

        var todos_from_source_files = comment_todos_search
            .Select(t => t.Line.Extract<TodoComment>(todo_comment_pattern))
            .Flatten()
            .ToList();

        // todos_from_source_files.Take(10).Dump();
        // Console.WriteLine($"total files with todo comments: {todos_from_source_files.Count}");

        Console.WriteLine($"total comment todos {todos_from_source_files.Count}");

        var readme_todos_search = new Grepper
        {
            RootPath = personal_dir,
            Recursive = true,
            FileSearchMask = "README*.md",
            FileSearchLinePattern = readme_todo_pattern,
        }
            .GetMatchingFiles()
            .ToList();

        var todos_from_readme_files = readme_todos_search
            .Select(t => t.Line.Extract<ReadMeTodo>(readme_todo_pattern))
            .Flatten()
            .ToList();

        Console.WriteLine($"todos from readme {todos_from_readme_files.Count}");

        var results = new LocalTodos
        {
            CommentsWithTodos = todos_from_source_files.ToArray(),
            ReadmeTodos = todos_from_readme_files.ToArray(),
        };

        // build quick and dirty sql for storing todos:
        // var sb = new StringBuilder(@"insert into")
        //     .Append(" todos (content, description) values ");

        // foreach (var comment in todos_from_source_files
        //              .Where(c => c?.content?.Length > 0
        //                          && c?.content?.Length <= 250))
        // {
        //     sb.AppendLine($"('{comment?.content ?? string.Empty}'),");
        // }

        // sb.RemoveFromEnd(2);

        // foreach (var readme in todos_from_readme_files.Where(r => r.content.Length > 0
        //                                                           && r.content.Length <= 250))
        // {
        //     sb.AppendLine($"('{readme.content}')");
        // }

        // string query = sb.ToString();
        // Console.WriteLine("query len:>> " + query.Length);
        //
        // new SaveFile(query).To(cwd, "sql").As("sync_local_todos.sql");

        // Console.WriteLine("insert query: >> \n" + query);

        return Content(results.ToString());
    }

    public async Task<IActionResult> OnGetSchemaInsights()
    {
        var table_info = await GetCurrentSchema();
        var result = new SchemaInfo() { table_info = table_info, total_execution_time_ms = 0 };

        return Partial("_SchemaCards", result);
    }

    public async Task<IActionResult> OnGetAllTodos()
    {
        Console.WriteLine(nameof(OnGetAllTodos));
        var watch = Stopwatch.StartNew();
        string query =
            @"
                        select id, content, status, priority #, is_sample_data
                        from AvailableTodos
                        # where
                         #   content like 'test%'
                           # or todos.is_sample_data = 1
                        ";

        using var connection = SQLConnections.CreateConnection();
               
               var todos =
                (await connection.QueryAsync<Part>(    query)).ToArray();


        // var todos = (await connection.QueryAsync("get_all_todos", CommandType.StoredProcedure)).ToArray();

        watch.Stop();
        var ms = watch.ElapsedMilliseconds;
        Console.WriteLine();
        return Content($"done.  found {todos.Length} todos, taking {ms} milliseconds");

        // return Partial("_PortalCards", todos);
    }

    public async Task<IActionResult> OnGetSwap()
    {
        int num = Enumerable.Range(1, 10000).TakeFirstRandom();

        return Content($"{num}");
    }

    private async Task<MySqlTableInfo[]> GetCurrentSchema()
    {
        string db_name =
            Environment.GetEnvironmentVariable("MYSQLDATABASE")
            ?? throw new NullReferenceException(
                "mysqldatabase environment variable cannot be null or empty!"
            );
        string query =
            $@"
use {db_name};
show full tables;";

        List<MySqlTableInfo> table_info = new List<MySqlTableInfo>();
        await using var connection = new MySqlConnection(SQLConnections.GetMySQLConnectionString());
        await connection.OpenAsync();
        using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            table_info.Add(
                new MySqlTableInfo()
                {
                    table_name = reader.GetString(0),
                    table_type = reader.GetString(1),
                }
            );
        }

        return table_info.ToArray();
    }
}

/// <summary>
/// this class holds all the todotasks on your local drive
/// </summary>
public class LocalTodos
{
    public TodoComment[] CommentsWithTodos { get; set; } = Array.Empty<TodoComment>();
    public ReadMeTodo[] ReadmeTodos { get; set; } = Array.Empty<ReadMeTodo>();

    public override string ToString()
    {
        return $"total comment todos: {CommentsWithTodos.Length}\n total readme todos: {ReadmeTodos.Length}";
    }
}

public class TodoComment
{
    public string content { get; set; } = string.Empty;
}

public class ReadMeTodo
{
    public string content { get; set; } = string.Empty;
    public string checkbox { get; set; } = string.Empty;
    public bool is_checked => checkbox.Contains("x", StringComparison.OrdinalIgnoreCase);
}

public class SchemaInfo
{
    public int total_execution_time_ms { get; set; }
    public MySqlTableInfo[] table_info { get; set; } = Array.Empty<MySqlTableInfo>();
}

public class Part
{
    public int id { get; set; } = -1;
    public string uri { get; set; } = string.Empty; // link to an individual record.
    public string name { get; set; } = string.Empty;
    public string description { set; get; }
    public string cost { get; set; } = string.Empty;
}

public class RegexEnumBase : Enumeration
{
    protected RegexEnumBase(int id, string name, string pattern, string uri = "")
        : base(id, name)
    {
        Pattern = pattern;
        CompiledRegex = new Regex(
            pattern,
            RegexOptions.IgnorePatternWhitespace
                | RegexOptions.Compiled
                | RegexOptions.IgnoreCase
                | RegexOptions.Multiline
        );
        this.uri = uri;
    }

    public string uri { get; set; } = string.Empty;

    public Regex CompiledRegex { get; set; }
    public string Pattern { get; set; }
}

public abstract class Enumeration : IComparable
{
    public string Name { get; private set; } = string.Empty;

    public int Id { get; private set; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>()
        where T : Enumeration =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object obj)
    {
        if (obj is not Enumeration otherValue)
        {
            return false;
        }

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

    // Other utility methods ...


    // Mine
    public static implicit operator Enumeration(string name)
    {
        var enumeration = GetAll<Enumeration>()
            .SingleOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return enumeration;
    }

    // From Jimmy B.  / Reuben Bond
    // Credit: https://github.com/dotnet-architecture/eShopOnContainers/blob/dev/src/Services/Ordering/Ordering.Domain/SeedWork/Enumeration.cs

    public override int GetHashCode() => Id.GetHashCode();

    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
    {
        var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
        return absoluteDifference;
    }

    public static T FromValue<T>(int value)
        where T : Enumeration
    {
        var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
        return matchingItem;
    }

    public static T FromDisplayName<T>(string displayName)
        where T : Enumeration
    {
        var matchingItem = Parse<T, string>(
            displayName,
            "display name",
            item => item.Name == displayName
        );
        return matchingItem;
    }

    private static T Parse<T, K>(K value, string description, Func<T, bool> predicate)
        where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
            throw new InvalidOperationException(
                $"'{value}' is not a valid {description} in {typeof(T)}"
            );

        return matchingItem;
    }
}

public class MySqlTableInfo
{
    public string table_name { get; set; } = string.Empty;
    public string table_type { get; set; } = string.Empty;
}

public static class SQLConnections
{
    public static MySqlConnection CreateConnection() => GetMySQLConnectionString().AsConnection();

    public static MySqlConnection AsConnection(this string connectionString) =>
        new MySqlConnection(connectionString);

    public static string GetMySQLConnectionString()
    {
        var connectionString = new MySqlConnectionStringBuilder()
        {
            Database = Environment.GetEnvironmentVariable("MYSQLDATABASE"),
            Server = Environment.GetEnvironmentVariable("MYSQLHOST"),
            Password = Environment.GetEnvironmentVariable("MYSQLPASSWORD"),
            UserID = Environment.GetEnvironmentVariable("MYSQLUSER"),
            Port = (uint)Environment.GetEnvironmentVariable("MYSQLPORT").ToInt(),
        }.ToString();

        if (connectionString.IsEmpty())
            throw new ArgumentNullException(nameof(connectionString));
        return connectionString;
    }
}
