using System.Text.Json;
using System.Text.Json.Serialization;
using Chemy.Core.Reactions.Explanations;
using Chemy.Core.Thermodynamics;

namespace Chemy.Core.Serialization;

public class MoleculeJsonConverter : JsonConverter<Molecule>
{
    public override Molecule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? formula = reader.GetString();
        if (string.IsNullOrEmpty(formula)) throw new JsonException("Molecule JSON string is null or empty.");
        return Molecule.Parse(formula);
    }

    public override void Write(Utf8JsonWriter writer, Molecule value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("formula", value.ChemicalFormula);
        writer.WriteNumber("molecularWeight", value.MolecularWeight);
        writer.WriteNumber("netCharge", value.NetCharge);
        writer.WriteEndObject();
    }
}

public class ReactionJsonConverter : JsonConverter<Reaction>
{
    public override Reaction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? equation = reader.GetString();
        if (string.IsNullOrEmpty(equation)) throw new JsonException("Reaction JSON string is null or empty.");
        return Reaction.Parse(equation);
    }

    public override void Write(Utf8JsonWriter writer, Reaction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("equation", value.ToString());
        writer.WriteBoolean("isBalanced", value.IsBalanced);
        writer.WriteEndObject();
    }
}
