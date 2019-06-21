using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Git
{
    public interface IMetricsService
    {
        /// <summary>
        /// Posts the provided usage model.
        /// </summary>
        void PostUsage(List<Usage> model);
    }
}
