using System.Collections.Frozen;

namespace Chemy.Core;

public static class Elements
{
    public static readonly Element Hydrogen = new(1, "H", "Hydrogen", 1.008);
    public static readonly Element Helium = new(2, "He", "Helium", 4.0026);
    public static readonly Element Lithium = new(3, "Li", "Lithium", 6.94);
    public static readonly Element Beryllium = new(4, "Be", "Beryllium", 9.0122);
    public static readonly Element Boron = new(5, "B", "Boron", 10.81);
    public static readonly Element Carbon = new(6, "C", "Carbon", 12.011);
    public static readonly Element Nitrogen = new(7, "N", "Nitrogen", 14.007);
    public static readonly Element Oxygen = new(8, "O", "Oxygen", 15.999);
    public static readonly Element Fluorine = new(9, "F", "Fluorine", 18.998);
    public static readonly Element Neon = new(10, "Ne", "Neon", 20.180);
    public static readonly Element Sodium = new(11, "Na", "Sodium", 22.990);
    public static readonly Element Magnesium = new(12, "Mg", "Magnesium", 24.305);
    public static readonly Element Aluminium = new(13, "Al", "Aluminium", 26.982);
    public static readonly Element Silicon = new(14, "Si", "Silicon", 28.085);
    public static readonly Element Phosphorus = new(15, "P", "Phosphorus", 30.974);
    public static readonly Element Sulfur = new(16, "S", "Sulfur", 32.06);
    public static readonly Element Chlorine = new(17, "Cl", "Chlorine", 35.45);
    public static readonly Element Argon = new(18, "Ar", "Argon", 39.948);
    public static readonly Element Potassium = new(19, "K", "Potassium", 39.098);
    public static readonly Element Calcium = new(20, "Ca", "Calcium", 40.078);
    public static readonly Element Scandium = new(21, "Sc", "Scandium", 44.956);
    public static readonly Element Titanium = new(22, "Ti", "Titanium", 47.867);
    public static readonly Element Vanadium = new(23, "V", "Vanadium", 50.942);
    public static readonly Element Chromium = new(24, "Cr", "Chromium", 51.996);
    public static readonly Element Manganese = new(25, "Mn", "Manganese", 54.938);
    public static readonly Element Iron = new(26, "Fe", "Iron", 55.845);
    public static readonly Element Cobalt = new(27, "Co", "Cobalt", 58.933);
    public static readonly Element Nickel = new(28, "Ni", "Nickel", 58.693);
    public static readonly Element Copper = new(29, "Cu", "Copper", 63.546);
    public static readonly Element Zinc = new(30, "Zn", "Zinc", 65.38);
    public static readonly Element Gallium = new(31, "Ga", "Gallium", 69.723);
    public static readonly Element Germanium = new(32, "Ge", "Germanium", 72.630);
    public static readonly Element Arsenic = new(33, "As", "Arsenic", 74.922);
    public static readonly Element Selenium = new(34, "Se", "Selenium", 78.971);
    public static readonly Element Bromine = new(35, "Br", "Bromine", 79.904);
    public static readonly Element Krypton = new(36, "Kr", "Krypton", 83.798);
    public static readonly Element Rubidium = new(37, "Rb", "Rubidium", 85.468);
    public static readonly Element Strontium = new(38, "Sr", "Strontium", 87.62);
    public static readonly Element Yttrium = new(39, "Y", "Yttrium", 88.906);
    public static readonly Element Zirconium = new(40, "Zr", "Zirconium", 91.224);
    public static readonly Element Niobium = new(41, "Nb", "Niobium", 92.906);
    public static readonly Element Molybdenum = new(42, "Mo", "Molybdenum", 95.95);
    public static readonly Element Technetium = new(43, "Tc", "Technetium", 98);
    public static readonly Element Ruthenium = new(44, "Ru", "Ruthenium", 101.07);
    public static readonly Element Rhodium = new(45, "Rh", "Rhodium", 102.91);
    public static readonly Element Palladium = new(46, "Pd", "Palladium", 106.42);
    public static readonly Element Silver = new(47, "Ag", "Silver", 107.87);
    public static readonly Element Cadmium = new(48, "Cd", "Cadmium", 112.41);
    public static readonly Element Indium = new(49, "In", "Indium", 114.82);
    public static readonly Element Tin = new(50, "Sn", "Tin", 118.71);
    public static readonly Element Antimony = new(51, "Sb", "Antimony", 121.76);
    public static readonly Element Tellurium = new(52, "Te", "Tellurium", 127.60);
    public static readonly Element Iodine = new(53, "I", "Iodine", 126.90);
    public static readonly Element Xenon = new(54, "Xe", "Xenon", 131.29);
    public static readonly Element Caesium = new(55, "Cs", "Caesium", 132.91);
    public static readonly Element Barium = new(56, "Ba", "Barium", 137.33);
    public static readonly Element Lanthanum = new(57, "La", "Lanthanum", 138.91);
    public static readonly Element Cerium = new(58, "Ce", "Cerium", 140.12);
    public static readonly Element Praseodymium = new(59, "Pr", "Praseodymium", 140.91);
    public static readonly Element Neodymium = new(60, "Nd", "Neodymium", 144.24);
    public static readonly Element Promethium = new(61, "Pm", "Promethium", 145);
    public static readonly Element Samarium = new(62, "Sm", "Samarium", 150.36);
    public static readonly Element Europium = new(63, "Eu", "Europium", 151.96);
    public static readonly Element Gadolinium = new(64, "Gd", "Gadolinium", 157.25);
    public static readonly Element Terbium = new(65, "Tb", "Terbium", 158.93);
    public static readonly Element Dysprosium = new(66, "Dy", "Dysprosium", 162.50);
    public static readonly Element Holmium = new(67, "Ho", "Holmium", 164.93);
    public static readonly Element Erbium = new(68, "Er", "Erbium", 167.26);
    public static readonly Element Thulium = new(69, "Tm", "Thulium", 168.93);
    public static readonly Element Ytterbium = new(70, "Yb", "Ytterbium", 173.05);
    public static readonly Element Lutetium = new(71, "Lu", "Lutetium", 174.97);
    public static readonly Element Hafnium = new(72, "Hf", "Hafnium", 178.49);
    public static readonly Element Tantalum = new(73, "Ta", "Tantalum", 180.95);
    public static readonly Element Tungsten = new(74, "W", "Tungsten", 183.84);
    public static readonly Element Rhenium = new(75, "Re", "Rhenium", 186.21);
    public static readonly Element Osmium = new(76, "Os", "Osmium", 190.23);
    public static readonly Element Iridium = new(77, "Ir", "Iridium", 192.22);
    public static readonly Element Platinum = new(78, "Pt", "Platinum", 195.08);
    public static readonly Element Gold = new(79, "Au", "Gold", 196.97);
    public static readonly Element Mercury = new(80, "Hg", "Mercury", 200.59);
    public static readonly Element Thallium = new(81, "Tl", "Thallium", 204.38);
    public static readonly Element Lead = new(82, "Pb", "Lead", 207.2);
    public static readonly Element Bismuth = new(83, "Bi", "Bismuth", 208.98);
    public static readonly Element Polonium = new(84, "Po", "Polonium", 209);
    public static readonly Element Astatine = new(85, "At", "Astatine", 210);
    public static readonly Element Radon = new(86, "Rn", "Radon", 222);
    public static readonly Element Francium = new(87, "Fr", "Francium", 223);
    public static readonly Element Radium = new(88, "Ra", "Radium", 226);
    public static readonly Element Actinium = new(89, "Ac", "Actinium", 227);
    public static readonly Element Thorium = new(90, "Th", "Thorium", 232.04);
    public static readonly Element Protactinium = new(91, "Pa", "Protactinium", 231.04);
    public static readonly Element Uranium = new(92, "U", "Uranium", 238.03);
    public static readonly Element Neptunium = new(93, "Np", "Neptunium", 237);
    public static readonly Element Plutonium = new(94, "Pu", "Plutonium", 244);
    public static readonly Element Americium = new(95, "Am", "Americium", 243);
    public static readonly Element Curium = new(96, "Cm", "Curium", 247);
    public static readonly Element Berkelium = new(97, "Bk", "Berkelium", 247);
    public static readonly Element Californium = new(98, "Cf", "Californium", 251);
    public static readonly Element Einsteinium = new(99, "Es", "Einsteinium", 252);
    public static readonly Element Fermium = new(100, "Fm", "Fermium", 257);
    public static readonly Element Mendelevium = new(101, "Md", "Mendelevium", 258);
    public static readonly Element Nobelium = new(102, "No", "Nobelium", 259);
    public static readonly Element Lawrencium = new(103, "Lr", "Lawrencium", 266);
    public static readonly Element Rutherfordium = new(104, "Rf", "Rutherfordium", 267);
    public static readonly Element Dubnium = new(105, "Db", "Dubnium", 268);
    public static readonly Element Seaborgium = new(106, "Sg", "Seaborgium", 269);
    public static readonly Element Bohrium = new(107, "Bh", "Bohrium", 270);
    public static readonly Element Hassium = new(108, "Hs", "Hassium", 270);
    public static readonly Element Meitnerium = new(109, "Mt", "Meitnerium", 278);
    public static readonly Element Darmstadtium = new(110, "Ds", "Darmstadtium", 281);
    public static readonly Element Roentgenium = new(111, "Rg", "Roentgenium", 282);
    public static readonly Element Copernicium = new(112, "Cn", "Copernicium", 285);
    public static readonly Element Nihonium = new(113, "Nh", "Nihonium", 286);
    public static readonly Element Flerovium = new(114, "Fl", "Flerovium", 289);
    public static readonly Element Moscovium = new(115, "Mc", "Moscovium", 290);
    public static readonly Element Livermorium = new(116, "Lv", "Livermorium", 293);
    public static readonly Element Tennessine = new(117, "Ts", "Tennessine", 294);
    public static readonly Element Oganesson = new(118, "Og", "Oganesson", 294);
    /// <summary>
    /// Gets the complete collection of all 118 IUPAC chemical elements.
    /// </summary>
    public static IReadOnlyList<Element> All { get; } =
    [
        Hydrogen, Helium, Lithium, Beryllium, Boron, Carbon, Nitrogen, Oxygen, Fluorine, Neon,
        Sodium, Magnesium, Aluminium, Silicon, Phosphorus, Sulfur, Chlorine, Argon, Potassium, Calcium,
        Scandium, Titanium, Vanadium, Chromium, Manganese, Iron, Cobalt, Nickel, Copper, Zinc,
        Gallium, Germanium, Arsenic, Selenium, Bromine, Krypton, Rubidium, Strontium, Yttrium, Zirconium,
        Niobium, Molybdenum, Technetium, Ruthenium, Rhodium, Palladium, Silver, Cadmium, Indium, Tin,
        Antimony, Tellurium, Iodine, Xenon, Caesium, Barium, Lanthanum, Cerium, Praseodymium, Neodymium,
        Promethium, Samarium, Europium, Gadolinium, Terbium, Dysprosium, Holmium, Erbium, Thulium, Ytterbium,
        Lutetium, Hafnium, Tantalum, Tungsten, Rhenium, Osmium, Iridium, Platinum, Gold, Mercury,
        Thallium, Lead, Bismuth, Polonium, Astatine, Radon, Francium, Radium, Actinium, Thorium,
        Protactinium, Uranium, Neptunium, Plutonium, Americium, Curium, Berkelium, Californium, Einsteinium, Fermium,
        Mendelevium, Nobelium, Lawrencium, Rutherfordium, Dubnium, Seaborgium, Bohrium, Hassium, Meitnerium, Darmstadtium,
        Roentgenium, Copernicium, Nihonium, Flerovium, Moscovium, Livermorium, Tennessine, Oganesson
    ];

    private static readonly FrozenDictionary<string, Element> BySymbolMap = All.ToFrozenDictionary(e => e.Symbol, StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenDictionary<int, Element> ByAtomicNumberMap = All.ToFrozenDictionary(e => e.AtomicNumber);

    /// <summary>
    /// Finds an element by its chemical symbol with case-insensitive O(1) lookup.
    /// </summary>
    /// <param name="symbol">Chemical symbol (e.g. "Fe", "H", "cu").</param>
    /// <returns>Matching Element record.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if symbol is not found.</exception>
    public static Element GetBySymbol(string symbol) =>
        BySymbolMap.TryGetValue(symbol, out var element)
            ? element
            : throw new KeyNotFoundException($"No element found with symbol '{symbol}'.");

    /// <summary>
    /// Finds an element by its atomic number (1 to 118) with O(1) lookup.
    /// </summary>
    /// <param name="atomicNumber">Atomic number (Z).</param>
    /// <returns>Matching Element record.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if atomic number is invalid.</exception>
    public static Element GetByAtomicNumber(int atomicNumber) =>
        ByAtomicNumberMap.TryGetValue(atomicNumber, out var element)
            ? element
            : throw new ArgumentOutOfRangeException(nameof(atomicNumber), $"Atomic number must be between 1 and 118. Received: {atomicNumber}");

    /// <summary>
    /// Safely attempts to lookup an element by its chemical symbol without throwing an exception.
    /// </summary>
    public static bool TryGetBySymbol(string symbol, out Element element) =>
        BySymbolMap.TryGetValue(symbol, out element);

    /// <summary>
    /// Safely attempts to lookup an element by its atomic number without throwing an exception.
    /// </summary>
    public static bool TryGetByAtomicNumber(int atomicNumber, out Element element) =>
        ByAtomicNumberMap.TryGetValue(atomicNumber, out element);
}

