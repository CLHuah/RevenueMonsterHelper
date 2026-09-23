using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model;

/// <summary>
///     A Revenue Monster API response carrying a single item.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class ApiResponse<T>
{
    public string code { get; set; }
    public Error error { get; set; }
    public T item { get; set; }
}

/// <summary>
///     A Revenue Monster API response carrying a list of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class ApiListResponse<T>
{
    public string code { get; set; }
    public Error error { get; set; }
    public List<T> items { get; set; }
    public Meta meta { get; set; }
}
