using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sh8lny.Domain.Models
{
    public class ProjectModule
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? EstimatedDuration { get; set; }
        public int OrderIndex { get; set; }
        
        /// <summary>
        /// Weight of this module as a percentage (0-100) of total project.
        /// </summary>
        public decimal Weight { get; set; }
        
        /// <summary>
        /// Module status
        /// </summary>
        public ModuleStatus Status { get; set; } = ModuleStatus.Pending;

        // Navigation
        public Project Project { get; set; } = null!;
        public ICollection<ApplicationModuleProgress> ModuleProgress { get; set; } = new HashSet<ApplicationModuleProgress>();
    }

    public enum ModuleStatus
    {
        Pending,
        InProgress,
        Completed,
        Approved,
        Rejected
    }
}
