using System;
using System.Collections.Generic;

namespace Automatica.Core.WebApi.Controllers
{
    /// <summary>
    /// A device found in learn mode: added as new node below <see cref="Parent"/>, or taking over an existing node
    /// (<see cref="Replace"/>, e.g. a swapped device) so its links, history and visualization stay.
    /// </summary>
    public class LearnedNodeRequest
    {
        /// <summary>
        /// The node learn mode ran on (e.g. EnOcean "Learned").
        /// </summary>
        public Guid Parent { get; set; }

        /// <summary>
        /// The template of the new node, ignored when replacing.
        /// </summary>
        public Guid NodeTemplate { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Guid? Area { get; set; }

        public Guid? Category { get; set; }

        /// <summary>
        /// Property values by template key (e.g. enocean-serialnumber), set on the node and its default children.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The property that identifies the device (e.g. enocean-serialnumber): no other node below
        /// <see cref="Parent"/> may have the same value.
        /// </summary>
        public string UniqueProperty { get; set; }

        /// <summary>
        /// An existing node below <see cref="Parent"/> that gets the properties instead of a new node.
        /// </summary>
        public Guid? Replace { get; set; }
    }

    /// <summary>
    /// The node that already has the value of <see cref="LearnedNodeRequest.UniqueProperty"/>.
    /// </summary>
    public class LearnedNodeConflict
    {
        public Guid NodeId { get; set; }

        public string Name { get; set; }
    }
}
