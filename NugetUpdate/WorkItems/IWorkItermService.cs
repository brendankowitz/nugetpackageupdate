using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPackageUpdates.WorkItems
{
    public interface IWorkItemService
    {
        Task<dynamic> CreateUserStoryAsync(List<object> requestBody, string workItemType = "User Story");
    }
}


