using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace OpenCGSS.StarlightDirector.Models.Beatmap {
    public sealed class CompiledScore {

        public CompiledScore([NotNull, ItemNotNull] IEnumerable<CompiledNote> notes) {
            Notes = notes.ToArray();
        }

        [NotNull, ItemNotNull]
        public IReadOnlyList<CompiledNote> Notes { get; }

    }
}
