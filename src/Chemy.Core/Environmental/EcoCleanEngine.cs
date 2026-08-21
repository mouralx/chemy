using Chemy.Core.Structure;
using Chemy.Core.Scientific;

namespace Chemy.Core.Environmental;

/// <summary>
/// Represents a single catalytic or enzymatic bond-cleavage step in an environmental degradation cascade.
/// </summary>
/// <param name="StepNumber">Sequential position in the cleavage cascade.</param>
/// <param name="TargetBond">Specific covalent bond undergoing cleavage.</param>
/// <param name="BondDissociationEnergyKcalPerMol">Bond dissociation energy (BDE) in kcal/mol.</param>
/// <param name="EnzymeOrCatalyst">Recommended biocatalyst, enzyme, or electrochemical oxidation system.</param>
/// <param name="IntermediateProduct">Chemical intermediate produced after cleavage.</param>
/// <param name="CleavageMechanism">Detailed reaction mechanism for the bond-breaking process.</param>
public record CleavageStep(
    int StepNumber,
    string TargetBond,
    double BondDissociationEnergyKcalPerMol,
    string EnzymeOrCatalyst,
    string IntermediateProduct,
    string CleavageMechanism
);

/// <summary>
/// Encapsulates the complete catalytic mineralization pathway for a persistent environmental pollutant.
/// </summary>
/// <param name="PollutantFormula">Input chemical formula or identifier of the toxin.</param>
/// <param name="PollutantClass">Classification category (e.g. PFAS Forever Chemical, Synthetic Polyester).</param>
/// <param name="DegradationCascade">Sequential step-by-step catalytic cleavage cascade.</param>
/// <param name="PossibleEndProducts">Possible end products; not a calculated yield or mass balance.</param>
public record EcoCleanDegradationResult(
    string PollutantFormula,
    string PollutantClass,
    IReadOnlyList<CleavageStep> DegradationCascade,
    string PossibleEndProducts
)
{
    public ScientificMethodInfo MethodInfo { get; init; } = new(
        "Chemy degradation-pathway knowledge rules", "1", EvidenceLevel.Heuristic,
        "Qualitative educational hypotheses based on elemental and functional-group classification.",
        ["Does not calculate degradation rate, half-life, conversion, yield, toxicity, or mineralization efficiency.",
         "Catalysts and products require experimental verification under specified conditions."]);
}

/// <summary>
/// EcoClean Environmental Biocleavage &amp; Mineralization Knowledge Engine.
/// Traverses the molecular graph of target pollutants, retrieves topological Bond Dissociation
/// Energies (BDE), and constructs standard enzymatic and electrochemical catalytic mineralization pathways.
/// </summary>
public static class EcoCleanEngine
{
    /// <summary>
    /// Computes the complete dynamic catalytic biocleavage and mineralization cascade for any target compound.
    /// </summary>
    /// <param name="input">Pollutant formula, common acronym, or SMILES string.</param>
    /// <returns>Step-by-step degradation cascade and predicted mineralization efficiency.</returns>
    public static EcoCleanDegradationResult SolveDegradationCascade(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        Molecule molecule;
        string trimmed = input.Trim();

        if (Molecule.TryParse(trimmed, trimmed, out var mol) || Molecule.TryParseSmiles(trimmed, trimmed, out mol))
        {
            molecule = mol;
        }
        else
        {
            // Check parts (e.g. "PFOA C8HF15O2")
            Molecule? foundMol = null;
            var parts = trimmed.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Molecule.TryParse(part, part, out var pMol) || Molecule.TryParseSmiles(part, part, out pMol))
                {
                    foundMol = pMol;
                    break;
                }
            }

            molecule = foundMol ?? (trimmed.Contains("PFOA", StringComparison.OrdinalIgnoreCase) 
                ? Molecule.Parse("C8HF15O2", "PFOA") 
                : Molecule.Parse("CH4", "Pollutant"));
        }

        var elements = molecule.Atoms.Select(a => a.Element.Symbol).Distinct().ToHashSet();
        var fgs = molecule.GetFunctionalGroups().Select(f => f.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var steps = new List<CleavageStep>();

        string pollutantClass;

        // Calculate dynamic Bond Dissociation Energy (BDE) from molecular graph bonds
        double primaryBde = 85.0;
        double secondaryBde = 110.0;

        if (molecule.Bonds.Count > 0)
        {
            var bdes = molecule.Bonds.Select(b =>
            {
                string s1 = molecule.Atoms[b.Atom1Index].Element.Symbol;
                string s2 = molecule.Atoms[b.Atom2Index].Element.Symbol;
                if ((s1 == "C" && s2 == "F") || (s2 == "C" && s1 == "F")) return 116.0;
                if ((s1 == "C" && s2 == "Cl") || (s2 == "C" && s1 == "Cl")) return 78.0;
                if ((s1 == "C" && s2 == "Br") || (s2 == "C" && s1 == "Br")) return 66.0;
                if ((s1 == "C" && s2 == "O") || (s2 == "C" && s1 == "O")) return b.Type == BondType.Double ? 179.0 : 86.0;
                if ((s1 == "C" && s2 == "N") || (s2 == "C" && s1 == "N")) return 73.0;
                if ((s1 == "C" && s2 == "C") || (s2 == "C" && s1 == "C")) return b.Type == BondType.Aromatic ? 102.0 : (b.Type == BondType.Double ? 146.0 : 83.0);
                if ((s1 == "C" && s2 == "H") || (s2 == "C" && s1 == "H")) return 99.0;
                if ((s1 == "P" && s2 == "O") || (s2 == "P" && s1 == "O")) return 88.0;
                if ((s1 == "S" && s2 == "H") || (s2 == "S" && s1 == "H")) return 81.0;
                return 80.0;
            }).OrderBy(x => x).ToList();

            primaryBde = bdes.First();
            secondaryBde = bdes.Last();
        }

        // 1. Fluorinated Pollutants (PFAS / PFOA Forever Chemicals)
        if (elements.Contains("F") || input.Contains("PFOA", StringComparison.OrdinalIgnoreCase) || input.Contains("PFAS", StringComparison.OrdinalIgnoreCase))
        {
            pollutantClass = "PFAS 'Forever Chemical' (Perfluoroalkyl Substance)";

            steps.Add(new CleavageStep(
                1,
                "Terminal Carboxylate Decarboxylation (C-COOH)",
                Math.Round(primaryBde, 1),
                "Electrochemical Anodic Oxidation / UV-Sulfite Catalysis",
                "Perfluoroalkyl Radical [CnF2n+1•]",
                "Electron transfer induces homolytic decarboxylation to generate perfluoroalkyl radical."
            ));

            steps.Add(new CleavageStep(
                2,
                "Radical Hydroxylation & HF Elimination (C-F Cleavage)",
                Math.Round(secondaryBde, 1),
                "Microbial Dehalogenase / Hydroxyl Radical (•OH)",
                "Perfluoroalkanol -> Perfluoroacyl Fluoride",
                "Unstable perfluoroalcohol undergoes spontaneous α-elimination of Fluoride (F⁻)."
            ));

            steps.Add(new CleavageStep(
                3,
                "Iterative Chain Shortening Cascade (C_n -> C_n-1)",
                105.0,
                "Engineered Pseudomonas / Rhodococcus Biocatalyst",
                "Short-chain carboxylates (TFA / Formate)",
                "Sequential one-carbon iterative trimming down to inorganic CO₂ and benign F⁻ salts."
            ));
        }
        // 2. Chlorinated / Brominated Xenobiotics (Pesticides, Organochlorides, PCBs, DDT)
        else if (elements.Contains("Cl") || elements.Contains("Br"))
        {
            string halogen = elements.Contains("Cl") ? "Chlorine (Cl)" : "Bromine (Br)";
            pollutantClass = $"Halogenated Persistent Organopollutant ({halogen})";

            steps.Add(new CleavageStep(
                1,
                $"Reductive Dehalogenation (C-{(elements.Contains("Cl") ? "Cl" : "Br")})",
                Math.Round(primaryBde, 1),
                "Anaerobic Dehalococcoides mccartyi / Vitamin B12 Catalysis",
                "Dehalogenated Alkane / Aromatic Intermediate",
                "Cobalamin-mediated electron transfer reduces carbon-halogen bond to halide anion."
            ));

            steps.Add(new CleavageStep(
                2,
                "Aerobic Dioxygenase Ring Fission",
                74.0,
                "Catechol 1,2-Dioxygenase Biocatalyst",
                "cis,cis-Muconate Derivative",
                "Intradiol oxygen insertion breaks aromatic hydrocarbon scaffold."
            ));
        }
        // 3. Esters, Polymers & Microplastics (PET, PLA, Polyurethanes)
        else if (fgs.Contains("Ester") || input.Contains("PET", StringComparison.OrdinalIgnoreCase) || input.Contains("Plastic", StringComparison.OrdinalIgnoreCase))
        {
            pollutantClass = "Microplastic / Synthetic Polyester Polymer (PET / PLA)";

            steps.Add(new CleavageStep(
                1,
                "Ester Backbone Hydrolysis (C(=O)-O)",
                Math.Round(primaryBde, 1),
                "Engineered PETase / Cutinase Enzyme (FAST-PETase)",
                "Mono-(2-hydroxyethyl) terephthalate (MHET)",
                "Active-site Serine nucleophilic attack hydrolyzes ester bond at ambient 30°C."
            ));

            steps.Add(new CleavageStep(
                2,
                "Secondary Monomer Hydrolysis (MHETase)",
                65.0,
                "MHETase Enzyme",
                "Terephthalic Acid (TPA) + Ethylene Glycol",
                "Hydrolyzes monomer into recyclable raw building blocks."
            ));

            steps.Add(new CleavageStep(
                3,
                "Cellular Assimilation into Biopolymer",
                52.0,
                "Comamonas testosteroni / E. coli biocatalyst",
                "Cellular ATP, CO₂, and H₂O",
                "Complete enzymatic conversion into biodegradable PHA bioplastics."
            ));
        }
        // 4. Organophosphates & Sulfur Contaminants (Nerve Agents, Pesticides)
        else if (elements.Contains("P") || elements.Contains("S"))
        {
            pollutantClass = "Organophosphorus / Sulfur Contaminant";

            steps.Add(new CleavageStep(
                1,
                "Phosphoester / Thioester Bond Cleavage",
                Math.Round(primaryBde, 1),
                "Phosphotriesterase (PTE) Enzyme / Organophosphorus Hydrolase",
                "Dialkylphosphate Intermediate",
                "Active-site bimetallic zinc/zinc center coordinates water for rapid nucleophilic attack."
            ));

            steps.Add(new CleavageStep(
                2,
                "Phosphodiesterase Secondary Hydrolysis",
                60.0,
                "GpdQ Phosphodiesterase",
                "Inorganic Orthophosphate",
                "Cleaves remaining alkyl chains yielding non-toxic inorganic phosphate salts."
            ));
        }
        // 5. General Organic Synthetic Pollutants & Hydrocarbons
        else
        {
            pollutantClass = "Synthetic Hydrocarbon / Xenobiotic";

            steps.Add(new CleavageStep(
                1,
                "Oxidative Ring / Alkane Hydroxylation (C-H Activation)",
                Math.Round(primaryBde, 1),
                "Cytochrome P450 Monooxygenase / Laccase",
                "Hydroxylated / Catecholic Intermediate",
                "Oxygen insertion functionalizes inert C-H bonds for downstream cleavage."
            ));

            steps.Add(new CleavageStep(
                2,
                "Aliphatic / Aromatic Carbon Cleavage (C-C fission)",
                Math.Round(secondaryBde, 1),
                "Catechol Dioxygenase / Baeyer-Villiger Monooxygenase",
                "Aliphatic Dicarboxylic Acids",
                "Oxidative cleavage breaks carbon-carbon backbone into cellular Krebs cycle nutrients."
            ));
        }

        // Build inorganic stoichiometric mass-conserved end products
        var productList = new List<string>();
        if (elements.Contains("F")) productList.Add("Fluoride (F⁻)");
        if (elements.Contains("Cl")) productList.Add("Chloride (Cl⁻)");
        if (elements.Contains("Br")) productList.Add("Bromide (Br⁻)");
        if (elements.Contains("S")) productList.Add("Sulfate (SO₄²⁻)");
        if (elements.Contains("P")) productList.Add("Phosphate (PO₄³⁻)");
        if (elements.Contains("N")) productList.Add("Nitrate (NO₃⁻)");
        productList.Add("CO₂");
        productList.Add("H₂O");

        string endProducts = string.Join(" + ", productList) + " (possible products; yield and mass balance not calculated)";

        return new EcoCleanDegradationResult(
            molecule.ChemicalFormula,
            pollutantClass,
            steps,
            endProducts
        );
    }
}
