using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetAllTodos()
    {
        var watch = Stopwatch.StartNew();
        string query = @"select id, content, status, priority from todos where 
                                                    # length(content) > 15 or
                                                     content like 'test%'
                                                    #id  = 30";

        using var connection = SQLConnections.CreateConnection();
        var todos = (await connection.QueryAsync(query)).ToArray();

        // var todos = (await connection.QueryAsync("get_all_todos", CommandType.StoredProcedure)).ToArray();

        watch.Stop();
        var ms = watch.ElapsedMilliseconds;

        return Content($"done.  found {todos.Length} todos, taking {ms} milliseconds");
    }


    public async Task<IActionResult> OnGetSwap()
    {
        int num = Enumerable.Range(1, 10000).TakeFirstRandom();

        return Content($"{num}");
    }
}

public class RegexEnumBase : Enumeration
{
    protected RegexEnumBase(int id, string name, string pattern, string uri = "") : base(id, name)
    {
        Pattern = pattern;
        CompiledRegex = new Regex(pattern,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.IgnoreCase |
            RegexOptions.Multiline);
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

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
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

    public static T FromValue<T>(int value) where T : Enumeration
    {
        var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
        return matchingItem;
    }

    public static T FromDisplayName<T>(string displayName) where T : Enumeration
    {
        var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
        return matchingItem;
    }

    private static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

        return matchingItem;
    }
}

public static class SQLConnections
{
    public static MySqlConnection CreateConnection() => GetMySQLConnectionString().AsConnection();

    public static MySqlConnection AsConnection(this string connectionString) => new MySqlConnection(connectionString);

    public static string GetMySQLConnectionString()
    {
        var connectionString = new MySqlConnectionStringBuilder()
        {
            Database = Environment.GetEnvironmentVariable("MYSQLDATABASE"),
            Server = Environment.GetEnvironmentVariable("MYSQLHOST"),
            Password = Environment.GetEnvironmentVariable("MYSQLPASSWORD"),
            UserID = Environment.GetEnvironmentVariable("MYSQLUSER"),
            Port = (uint)Environment.GetEnvironmentVariable("MYSQLPORT").ToInt()
        }.ToString();

        if (connectionString.IsEmpty()) throw new ArgumentNullException(nameof(connectionString));
        return connectionString;
    }
}