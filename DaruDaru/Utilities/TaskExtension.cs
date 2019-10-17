using System.Threading.Tasks;

namespace DaruDaru.Utilities
{
    internal static class TaskExtension
    {
        public static T Exec<T>(this Task<T> task)
        {
            task.Wait();
            if (task.Exception != null)
                throw task.Exception;

            return task.Result;
        }
    }
}
