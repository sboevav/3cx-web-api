using System.Threading.Tasks;

namespace WebAPI.integration;

public interface ICommand<T>
{
    Task<T> ExecuteAsync();
}