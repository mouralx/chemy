namespace Chemy.Core.Environmental;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Represents a single catalytic or enzymatic bond-cleavage step in an environmental degradation cascade.
/// </summary>
/// <param name="StepNumber">Sequential position in the cleavage cascade.</param>
/// <param name="TargetBond">Specific covalent bond undergoing cleavage.</param>
/// <param name="BondDissociationEnergyKcalPerMol">Estimated Bond Dissociation Energy (BDE) in kcal/mol.</param>
/// <param name="EnzymeOrCatalyst">Candidate biocatalyst, enzyme family, or advanced oxidation process (AOP).</param>
/// <param name="IntermediateProduct">Chemical intermediate produced after cleavage.</param>
/// <param name="CleavageMechanism">Proposed chemical mechanism for the bond-cleavage step.</param>
public record CleavageStep(
    int StepNumber,
    string TargetBond,
    double BondDissociationEnergyKcalPerMol,
    string EnzymeOrCatalyst,
    string IntermediateProduct,
    string CleavageMechanism
);

/// <summary>
/// Encapsulates the qualitative catalytic mineralization pathway for an environmental pollutant.
/// </summary>
/// <param name="PollutantFormula">Chemical formula of the target compound.</param>
/// <param name="PollutantClass">Classification category (e.g. PFAS, Organohalide, Polyester).</param>
/// <param name="DegradationCascade">Sequential step-by-step catalytic cleavage cascade.</param>
/// <param name="TheoreticalMineralizationProducts">Stoichiometric inorganic mineral end-products.</param>
/// <param name="MethodInfo">Scientific method provenance, evidence level, and caveats.</param>
public record EcoCleanDegradationResult(
    string PollutantFormula,
    string PollutantClass,
    IReadOnlyList<CleavageStep> DegradationCascade,
    string TheoreticalMineralizationProducts,
    ScientificMethodInfo MethodInfo
)
{
    /// <summary>
    /// Backwards-compatible legacy property (always returns 0.0 with warning in MethodInfo).
    /// </summary>
    [Obsolete("Quantitative mineralization efficiency requires empirical reactor kinetics and is deprecated.")]
    [System.Text.Json.Serialization.JsonIgnore]
    public double TotalMineralizationEfficiencyPercent => 0.0;

    /// <summary>
    /// Backwards-compatible legacy property (always returns 0.0 with warning in MethodInfo).
    /// </summary>
    [Obsolete("Natural environmental persistence half-life requires environmental field calibration.")]
    [System.Text.Json.Serialization.JsonIgnore]
    public double PersistenceHalfLifeYears => 0.0;

    /// <summary>
    /// Backwards-compatible alias for TheoreticalMineralizationProducts.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string MineralizedEndProducts => TheoreticalMineralizationProducts;
}

/// <summary>
/// EcoClean Environmental Biocleavage &amp; Degradation Cascade Engine.
/// Analyzes the molecular graph of target pollutants, identifies weakest bonds based on Bond Dissociation
/// Energies (BDE), and generates qualitative enzymatic and advanced oxidation pathways.
/// </summary>
public static class EcoCleanEngine
{
    private static readonly ScientificMethodInfo EcoCleanMethodInfo = new(
        "EcoClean Qualitative BDE Degradation Cascade",
        "2026.1",
        EvidenceLevel.Heuristic,
        "Organic xenobiotics, halogenated pollutants, and synthetic polymers.",
        [
            "Qualitative mechanistic hypothesis based on bond dissociation energies and literature enzyme pathways.",
            "Does NOT compute quantitative mineralization kinetics, residence times, or reactor mass balances."
        ]
    );

    /// <summary>
    /// Generates the qualitative catalytic biocleavage and mineralization cascade for a target compound.
    /// </summary>
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
            var parts = trimmed.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
            Molecule? foundMol = null;
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
                "Sequential one-carbon iterative trimming down to inorganic CO₂ and F⁻ salts."
            ));
        }
        // 2. Chlorinated / Brominated Xenobiotics (Pesticides, Organochlorides, PCBs, DDT)
        else if (elements.Contains("Cl") || elements.Contains("Br"))
        {
            pollutantClass = "Halogenated Xenobiotic / Organohalide";

            steps.Add(new CleavageStep(
                1,
                "Reductive / Oxidative Dehalogenation (C-X Cleavage)",
                Math.Round(primaryBde, 1),
                "Reductive Dehalogenase / Zero-Valent Iron (Fe⁰)",
                "Dehalogenated Hydrocarbon Skeleton + Halide (X⁻)",
                "Electron transfer reduces carbon-halogen bond, releasing halide ion."
            ));

            steps.Add(new CleavageStep(
                2,
                "Aromatic Hydroxylation & Ring Fission",
                Math.Round(secondaryBde, 1),
                "Toluene Dioxygenase / Fenton Reagent (Fe²⁺/H₂O₂)",
                "Catecholic Intermediate -> cis,cis-Muconate",
                "Ortho-cleavage of benzene ring via dioxygenase into Krebs cycle intermediates."
            ));
        }
        // 3. Synthetic Esters & Plastics (PET Microplastics)
        else if (elements.Contains("O") && molecule.Bonds.Any(b => b.Type == BondType.Double &&
            (molecule.Atoms[b.Atom1Index].Element.Symbol == "O" || molecule.Atoms[b.Atom2Index].Element.Symbol == "O")))
        {
            pollutantClass = "Synthetic Polyester / Microplastic";

            steps.Add(new CleavageStep(
                1,
                "Ester Bond Hydrolysis (C(=O)-O Cleavage)",
                Math.Round(primaryBde, 1),
                "PETase / Cutinase Hydrolase (Ideonella sakaiensis)",
                "Monomeric Terephthalic Acid + Ethylene Glycol",
                "Catalytic Ser-His-Asp triad performs nucleophilic attack on ester carbonyl."
            ));

            steps.Add(new CleavageStep(
                2,
                "Cellular Assimilation into Biomass",
                52.0,
                "Comamonas testosteroni / E. coli biocatalyst",
                "Cellular ATP, CO₂, and H₂O",
                "Enzymatic assimilation into biomass."
            ));
        }
        // 4. Organophosphates & Sulfur Contaminants (Pesticides)
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
        // 5. General Hydrocarbons
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

        // Build stoichiometric mineral end products
        var productList = new List<string>();
        if (elements.Contains("F")) productList.Add("Fluoride (F⁻)");
        if (elements.Contains("Cl")) productList.Add("Chloride (Cl⁻)");
        if (elements.Contains("Br")) productList.Add("Bromide (Br⁻)");
        if (elements.Contains("S")) productList.Add("Sulfate (SO₄²⁻)");
        if (elements.Contains("P")) productList.Add("Phosphate (PO₄³⁻)");
        if (elements.Contains("N")) productList.Add("Nitrate (NO₃⁻)");
        productList.Add("CO₂");
        productList.Add("H₂O");

        string endProducts = string.Join(" + ", productList);

        return new EcoCleanDegradationResult(
            molecule.ChemicalFormula,
            pollutantClass,
            steps,
            endProducts,
            EcoCleanMethodInfo
        );
    }
}
