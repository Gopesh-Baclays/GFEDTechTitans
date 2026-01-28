using System.Collections.Generic;

namespace RECAP.Models
{
    public class ProspectDetailsViewModel
    {
        // The prospect selected by id (can be null)
        public ProspectViewModel? SelectedProspect { get; set; }

        // 1-based index of the selected prospect (null when not provided)
        public int? SelectedIndex { get; set; }

        // Full list to render below
        public List<ProspectViewModel>? Prospects { get; set; } = new List<ProspectViewModel>();
    }
}