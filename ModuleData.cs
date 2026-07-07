using System.IO;
using System.Linq;

namespace Ghost.Gw2EventTracker {

    internal static class ModuleData {

        internal static string ReadEmbedded(string resourceSuffix) {
            var assembly = typeof(ModuleData).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceSuffix, System.StringComparison.OrdinalIgnoreCase));

            if (resourceName == null) {
                throw new FileNotFoundException($"Embedded resource ending with '{resourceSuffix}' was not found.");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName)!)
            using (var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }

}