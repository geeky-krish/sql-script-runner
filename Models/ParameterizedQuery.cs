using Microsoft.Data.SqlClient;

namespace SQLScripRunner.Models;

internal sealed class ParameterizedQuery
{
    public string SqlScript { get; set; } = string.Empty;
    public List<SqlParameter> Parameters { get; set; } = new List<SqlParameter>();
}
internal sealed class QueryResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public T? Data { get; set; }
}