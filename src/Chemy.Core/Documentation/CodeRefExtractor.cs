using System.Reflection;
using System.Xml.Linq;

namespace Chemy.Core.Documentation;

public record CodeMemberDoc(
    string Name,
    string Summary,
    string? Returns = null,
    Dictionary<string, string>? Parameters = null,
    string MemberType = "Method",
    string Signature = ""
);

public record CodeClassDoc(
    string Name,
    string FullName,
    string Namespace,
    string Summary,
    string Category,
    List<CodeMemberDoc> Properties,
    List<CodeMemberDoc> Methods
);

/// <summary>
/// Extracts C# XML documentation comments and reflection metadata directly from assemblies.
/// Enables dynamic code reference generation from source code docstrings.
/// </summary>
public static class CodeRefExtractor
{
    public static List<CodeClassDoc> Extract(Assembly assembly)
    {
        var classDocs = new List<CodeClassDoc>();

        // Attempt to find companion .xml doc file
        string xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
        var xmlDocs = new Dictionary<string, XElement>();

        if (File.Exists(xmlPath))
        {
            try
            {
                var doc = XDocument.Load(xmlPath);
                foreach (var member in doc.Descendants("member"))
                {
                    var nameAttr = member.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(nameAttr))
                    {
                        xmlDocs[nameAttr] = member;
                    }
                }
            }
            catch
            {
                // Fallback to type reflection if XML file loading fails
            }
        }

        var types = assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested && !t.Name.StartsWith("<"))
            .OrderBy(t => t.Name);

        foreach (var type in types)
        {
            string typeDocKey = $"T:{type.FullName}";
            xmlDocs.TryGetValue(typeDocKey, out var typeXml);
            string classSummary = GetSummary(typeXml) ?? $"Domain component {type.Name} within namespace {type.Namespace}.";

            string category = type.Namespace?.Replace("Chemy.Core.", "").Replace("Chemy.Core", "Core") ?? "General";

            var properties = new List<CodeMemberDoc>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).OrderBy(p => p.Name))
            {
                string propDocKey = $"P:{type.FullName}.{prop.Name}";
                xmlDocs.TryGetValue(propDocKey, out var propXml);
                string propSummary = GetSummary(propXml) ?? $"Gets property {prop.Name} of type {prop.PropertyType.Name}.";

                properties.Add(new CodeMemberDoc(
                    Name: prop.Name,
                    Summary: propSummary,
                    MemberType: "Property",
                    Signature: $"{prop.PropertyType.Name} {prop.Name}"
                ));
            }

            var methods = new List<CodeMemberDoc>();
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .OrderBy(m => m.Name))
            {
                var paramTypes = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));
                string methodDocKey = string.IsNullOrEmpty(paramTypes)
                    ? $"M:{type.FullName}.{method.Name}"
                    : $"M:{type.FullName}.{method.Name}({paramTypes})";

                xmlDocs.TryGetValue(methodDocKey, out var methodXml);
                if (methodXml == null)
                {
                    // Fallback try without parameters key prefix
                    var firstMatchKey = xmlDocs.Keys.FirstOrDefault(k => k.StartsWith($"M:{type.FullName}.{method.Name}"));
                    if (firstMatchKey != null) methodXml = xmlDocs[firstMatchKey];
                }

                string methodSummary = GetSummary(methodXml) ?? $"Executes method {method.Name}.";
                string? returns = GetElementText(methodXml, "returns");

                var paramDocs = new Dictionary<string, string>();
                if (methodXml != null)
                {
                    foreach (var paramElem in methodXml.Elements("param"))
                    {
                        var name = paramElem.Attribute("name")?.Value;
                        if (!string.IsNullOrEmpty(name))
                        {
                            paramDocs[name] = paramElem.Value.Trim();
                        }
                    }
                }

                string methodSig = $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})";

                methods.Add(new CodeMemberDoc(
                    Name: method.Name,
                    Summary: methodSummary,
                    Returns: returns,
                    Parameters: paramDocs,
                    MemberType: "Method",
                    Signature: methodSig
                ));
            }

            classDocs.Add(new CodeClassDoc(
                Name: type.Name,
                FullName: type.FullName ?? type.Name,
                Namespace: type.Namespace ?? "Chemy.Core",
                Summary: classSummary,
                Category: category,
                Properties: properties,
                Methods: methods
            ));
        }

        return classDocs;
    }

    private static string? GetSummary(XElement? xml)
    {
        if (xml == null) return null;
        var summaryElem = xml.Element("summary");
        return summaryElem != null ? CleanDocText(summaryElem.Value) : null;
    }

    private static string? GetElementText(XElement? xml, string elementName)
    {
        if (xml == null) return null;
        var elem = xml.Element(elementName);
        return elem != null ? CleanDocText(elem.Value) : null;
    }

    private static string CleanDocText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var lines = raw.Split('\n').Select(l => l.Trim());
        return string.Join(" ", lines).Trim();
    }
}
