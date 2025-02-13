using System.Threading.Tasks;

namespace WebAPI.integration;

public static class RestCommandExecutor
{
    public static async Task<T> Execute<T>(ICommand<T> command)
    {
        return await command.ExecuteAsync();
    }
}