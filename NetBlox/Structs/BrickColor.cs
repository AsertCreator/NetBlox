using System.Numerics;

namespace NetBlox.Structs;

public record struct BrickColor
{
    public int Index;
    public string Name;
    public Color3 Color3;

    private static readonly Dictionary<int, BrickColor> AllColors = new Dictionary<int, BrickColor>()
    {
        [1] = new BrickColor(1, "White",		                new Color3(242, 243, 243)),
        [2] = new BrickColor(2, "Grey",		                    new Color3(161, 165, 162)),
        [3] = new BrickColor(3, "Light yellow",		            new Color3(249, 233, 153)),
        [5] = new BrickColor(5, "Brick yellow",		            new Color3(215, 197, 154)),
        [6] = new BrickColor(6, "Light green (Mint)",		    new Color3(194, 218, 184)),
        [9] = new BrickColor(9, "Light reddish violet",		    new Color3(232, 186, 200)),
        [11] = new BrickColor(11, "Pastel Blue",		        new Color3(128, 187, 219)),
        [12] = new BrickColor(12, "Light orange brown",		    new Color3(203, 132, 66)),
        [18] = new BrickColor(18, "Nougat",		                new Color3(204, 142, 105)),
        [21] = new BrickColor(21, "Bright red",		            new Color3(196, 40, 28)),
        [22] = new BrickColor(22, "Med. reddish violet",		new Color3(196, 112, 160)),
        [23] = new BrickColor(23, "Bright blue",		        new Color3(13, 105, 172)),
        [24] = new BrickColor(24, "Bright yellow",		        new Color3(245, 205, 48)),
        [25] = new BrickColor(25, "Earth orange",		        new Color3(98, 71, 50)),
        [26] = new BrickColor(26, "Black",		                new Color3(27, 42, 53)),
        [27] = new BrickColor(27, "Dark grey",		            new Color3(109, 110, 108)),
        [28] = new BrickColor(28, "Dark green",		            new Color3(40, 127, 71)),
        [29] = new BrickColor(29, "Medium green",		        new Color3(161, 196, 140)),
        [36] = new BrickColor(36, "Lig. Yellowich orange",		new Color3(243, 207, 155)),
        [37] = new BrickColor(37, "Bright green",		        new Color3(75, 151, 75)),
        [38] = new BrickColor(38, "Dark orange",		        new Color3(160, 95, 53)),
        [39] = new BrickColor(39, "Light bluish violet",		new Color3(193, 202, 222)),
        [40] = new BrickColor(40, "Transparent",		        new Color3(236, 236, 236)),
        [41] = new BrickColor(41, "Tr. Red",		            new Color3(205, 84, 75)),
        [42] = new BrickColor(42, "Tr. Lg blue",		        new Color3(193, 223, 240)),
        [43] = new BrickColor(43, "Tr. Blue",		            new Color3(123, 182, 232)),
        [44] = new BrickColor(44, "Tr. Yellow",		            new Color3(247, 241, 141)),
        [45] = new BrickColor(45, "Light blue",		            new Color3(180, 210, 228)),
        [47] = new BrickColor(47, "Tr. Flu. Reddish orange",	new Color3(217, 133, 108)),
        [48] = new BrickColor(48, "Tr. Green",		            new Color3(132, 182, 141)),
        [49] = new BrickColor(49, "Tr. Flu. Green",		        new Color3(248, 241, 132)),
        [50] = new BrickColor(50, "Phosph. White",		        new Color3(236, 232, 222)),
        [100] = new BrickColor(100, "Light red",		        new Color3(238, 196, 182)),
        [101] = new BrickColor(101, "Medium red",		        new Color3(218, 134, 122)),
        [102] = new BrickColor(102, "Medium blue",		        new Color3(110, 153, 202)),
        [103] = new BrickColor(103, "Light grey",		        new Color3(199, 193, 183)),
        [104] = new BrickColor(104, "Bright violet",		    new Color3(107, 50, 124)),
        [105] = new BrickColor(105, "Br. yellowish orange",		new Color3(226, 155, 64)),
        [106] = new BrickColor(106, "Bright orange",		    new Color3(218, 133, 65)),
        [107] = new BrickColor(107, "Bright bluish green",		new Color3(0, 143, 156)),
        [108] = new BrickColor(108, "Earth yellow",		        new Color3(104, 92, 67)),
        [110] = new BrickColor(110, "Bright bluish violet",		new Color3(67, 84, 147)),
        [111] = new BrickColor(111, "Tr. Brown",		        new Color3(191, 183, 177)),
        [112] = new BrickColor(112, "Medium bluish violet",		new Color3(104, 116, 172)),
        [113] = new BrickColor(113, "Tr. Medi. reddish violet",	new Color3(229, 173, 200)),
        [115] = new BrickColor(115, "Med. yellowish green",		new Color3(199, 210, 60)),
        [116] = new BrickColor(116, "Med. bluish green",		new Color3(85, 165, 175)),
        [118] = new BrickColor(118, "Light bluish green",		new Color3(183, 215, 213)),
        [119] = new BrickColor(119, "Br. yellowish green",		new Color3(164, 189, 71)),
        [120] = new BrickColor(120, "Lig. yellowish green",		new Color3(217, 228, 167)),
        [121] = new BrickColor(121, "Med. yellowish orange",	new Color3(231, 172, 88)),
        [123] = new BrickColor(123, "Br. reddish orange",		new Color3(211, 111, 76)),
        [124] = new BrickColor(124, "Bright reddish violet",	new Color3(146, 57, 120)),
        [125] = new BrickColor(125, "Light orange",		        new Color3(234, 184, 146)),
        [126] = new BrickColor(126, "Tr. Bright bluish violet",	new Color3(165, 165, 203)),
        [127] = new BrickColor(127, "Gold",		                new Color3(220, 188, 129)),
        [128] = new BrickColor(128, "Dark nougat",		        new Color3(174, 122, 89)),
        [131] = new BrickColor(131, "Silver",		            new Color3(156, 163, 168)),
        [133] = new BrickColor(133, "Neon orange",		        new Color3(213, 115, 61)),
        [134] = new BrickColor(134, "Neon green",		        new Color3(216, 221, 86)),
        [135] = new BrickColor(135, "Sand blue",		        new Color3(116, 134, 157)),
        [136] = new BrickColor(136, "Sand violet",		        new Color3(135, 124, 144)),
        [137] = new BrickColor(137, "Medium orange",	        new Color3(224, 152, 100)),
        [138] = new BrickColor(138, "Sand yellow",		        new Color3(149, 138, 115)),
        [140] = new BrickColor(140, "Earth blue",		        new Color3(32, 58, 86)),
        [141] = new BrickColor(141, "Earth green",		        new Color3(39, 70, 45)),
        [143] = new BrickColor(143, "Tr. Flu. Blue",		    new Color3(207, 226, 247)),
        [145] = new BrickColor(145, "Sand blue metallic",		new Color3(121, 136, 161)),
        [146] = new BrickColor(146, "Sand violet metallic",		new Color3(149, 142, 163)),
        [147] = new BrickColor(147, "Sand yellow metallic",		new Color3(147, 135, 103)),
        [148] = new BrickColor(148, "Dark grey metallic",		new Color3(87, 88, 87)),
        [149] = new BrickColor(149, "Black metallic",		    new Color3(22, 29, 50)),
        [150] = new BrickColor(150, "Light grey metallic",		new Color3(171, 173, 172)),
        [151] = new BrickColor(151, "Sand green",		        new Color3(120, 144, 130)),
        [153] = new BrickColor(153, "Sand red",		            new Color3(149, 121, 119)),
        [154] = new BrickColor(154, "Dark red",		            new Color3(123, 46, 47)),
        [157] = new BrickColor(157, "Tr. Flu. Yellow",		    new Color3(255, 246, 123)),
        [158] = new BrickColor(158, "Tr. Flu. Red",		        new Color3(225, 164, 194)),
        [168] = new BrickColor(168, "Gun metallic",		        new Color3(117, 108, 98)),
        [176] = new BrickColor(176, "Red flip/flop",		    new Color3(151, 105, 91)),
        [178] = new BrickColor(178, "Yellow flip/flop",		    new Color3(180, 132, 85)),
        [179] = new BrickColor(179, "Silver flip/flop",		    new Color3(137, 135, 136)),
        [180] = new BrickColor(180, "Curry",		            new Color3(215, 169, 75)),
        [190] = new BrickColor(190, "Fire Yellow",		        new Color3(249, 214, 46)),
        [191] = new BrickColor(191, "Flame yellowish orange",	new Color3(232, 171, 45)),
        [192] = new BrickColor(192, "Reddish brown",		    new Color3(105, 64, 40)),
        [193] = new BrickColor(193, "Flame reddish orange",		new Color3(207, 96, 36)),
        [194] = new BrickColor(194, "Medium stone grey",		new Color3(163, 162, 165)),
        [195] = new BrickColor(195, "Royal blue",		        new Color3(70, 103, 164)),
        [196] = new BrickColor(196, "Dark Royal blue",		    new Color3(35, 71, 139)),
        [198] = new BrickColor(198, "Bright reddish lilac",		new Color3(142, 66, 133)),
        [199] = new BrickColor(199, "Dark stone grey",		    new Color3(99, 95, 98)),
        [200] = new BrickColor(200, "Lemon metalic",		    new Color3(130, 138, 93)),
        [208] = new BrickColor(208, "Light stone grey",		    new Color3(229, 228, 223)),
        [209] = new BrickColor(209, "Dark Curry",		        new Color3(176, 142, 68)),
        [210] = new BrickColor(210, "Faded green",		        new Color3(112, 149, 120)),
        [211] = new BrickColor(211, "Turquoise",		        new Color3(121, 181, 181)),
        [212] = new BrickColor(212, "Light Royal blue",		    new Color3(159, 195, 233)),
        [213] = new BrickColor(213, "Medium Royal blue",		new Color3(108, 129, 183)),
        [216] = new BrickColor(216, "Rust",		                new Color3(144, 76, 42)),
        [217] = new BrickColor(217, "Brown",		            new Color3(124, 92, 70)),
        [218] = new BrickColor(218, "Reddish lilac",		    new Color3(150, 112, 159)),
        [219] = new BrickColor(219, "Lilac",		            new Color3(107, 98, 155)),
        [220] = new BrickColor(220, "Light lilac",		        new Color3(167, 169, 206)),
        [221] = new BrickColor(221, "Bright purple",	        new Color3(205, 98, 152)),
        [222] = new BrickColor(222, "Light purple",		        new Color3(228, 173, 200)),
        [223] = new BrickColor(223, "Light pink",		        new Color3(220, 144, 149)),
        [224] = new BrickColor(224, "Light brick yellow",		new Color3(240, 213, 160)),
        [225] = new BrickColor(225, "Warm yellowish orange",	new Color3(235, 184, 127)),
        [226] = new BrickColor(226, "Cool yellow",		        new Color3(253, 234, 141)),
        [232] = new BrickColor(232, "Dove blue",		        new Color3(125, 187, 221)),
        [268] = new BrickColor(268, "Medium lilac",		        new Color3(52, 43, 117)),
        [301] = new BrickColor(301, "Slime green",		        new Color3(80, 109, 84)),
        [302] = new BrickColor(302, "Smoky grey",		        new Color3(91, 93, 105)),
        [303] = new BrickColor(303, "Dark blue",		        new Color3(0, 16, 176)),
        [304] = new BrickColor(304, "Parsley green",	        new Color3(44, 101, 29)),
        [305] = new BrickColor(305, "Steel blue",		        new Color3(82, 124, 174)),
        [306] = new BrickColor(306, "Storm blue",		        new Color3(51, 88, 130)),
        [307] = new BrickColor(307, "Lapis",		            new Color3(16, 42, 220)),
        [308] = new BrickColor(308, "Dark indigo",	            new Color3(61, 21, 133)),
        [309] = new BrickColor(309, "Sea green",	            new Color3(52, 142, 64)),
        [310] = new BrickColor(310, "Shamrock",		            new Color3(91, 154, 76)),
        [311] = new BrickColor(311, "Fossil",		            new Color3(159, 161, 172)),
        [312] = new BrickColor(312, "Mulberry",		            new Color3(89, 34, 89)),
        [313] = new BrickColor(313, "Forest green",	            new Color3(31, 128, 29)),
        [314] = new BrickColor(314, "Cadet blue",	            new Color3(159, 173, 192)),
        [315] = new BrickColor(315, "Electric blue",		    new Color3(9, 137, 207)),
        [316] = new BrickColor(316, "Eggplant",		            new Color3(123, 0, 123)),
        [317] = new BrickColor(317, "Moss",		                new Color3(124, 156, 107)),
        [318] = new BrickColor(318, "Artichoke",		        new Color3(138, 171, 133)),
        [319] = new BrickColor(319, "Sage green",		        new Color3(185, 196, 177)),
        [320] = new BrickColor(320, "Ghost grey",		        new Color3(202, 203, 209)),
        [321] = new BrickColor(321, "Lilac",		            new Color3(167, 94, 155)),
        [322] = new BrickColor(322, "Plum",		                new Color3(123, 47, 123)),
        [323] = new BrickColor(323, "Olivine",		            new Color3(148, 190, 129)),
        [324] = new BrickColor(324, "Laurel green",		        new Color3(168, 189, 153)),
        [325] = new BrickColor(325, "Quill grey",		        new Color3(223, 223, 222)),
        [327] = new BrickColor(327, "Crimson",		            new Color3(151, 0, 0)),
        [328] = new BrickColor(328, "Mint",		                new Color3(177, 229, 166)),
        [329] = new BrickColor(329, "Baby blue",		        new Color3(152, 194, 219)),
        [330] = new BrickColor(330, "Carnation pink",	        new Color3(255, 152, 220)),
        [331] = new BrickColor(331, "Persimmon",		        new Color3(255, 89, 89)),
        [332] = new BrickColor(332, "Maroon",		            new Color3(117, 0, 0)),
        [333] = new BrickColor(333, "Gold",		                new Color3(239, 184, 56)),
        [334] = new BrickColor(334, "Daisy orange",	            new Color3(248, 217, 109)),
        [335] = new BrickColor(335, "Pearl",		            new Color3(231, 231, 236)),
        [336] = new BrickColor(336, "Fog",		                new Color3(199, 212, 228)),
        [337] = new BrickColor(337, "Salmon",		            new Color3(255, 148, 148)),
        [338] = new BrickColor(338, "Terra Cotta",	            new Color3(190, 104, 98)),
        [339] = new BrickColor(339, "Cocoa",		            new Color3(86, 36, 36)),
        [340] = new BrickColor(340, "Wheat",		            new Color3(241, 231, 199)),
        [341] = new BrickColor(341, "Buttermilk",	            new Color3(254, 243, 187)),
        [342] = new BrickColor(342, "Mauve",		            new Color3(224, 178, 208)),
        [343] = new BrickColor(343, "Sunrise",		            new Color3(212, 144, 189)),
        [344] = new BrickColor(344, "Tawny",		            new Color3(150, 85, 85)),
        [345] = new BrickColor(345, "Rust",		                new Color3(143, 76, 42)),
        [346] = new BrickColor(346, "Cashmere",		            new Color3(211, 190, 150)),
        [347] = new BrickColor(347, "Khaki",		            new Color3(226, 220, 188)),
        [348] = new BrickColor(348, "Lily white",	            new Color3(237, 234, 234)),
        [349] = new BrickColor(349, "Seashell",		            new Color3(233, 218, 218)),
        [350] = new BrickColor(350, "Burgundy",		            new Color3(136, 62, 62)),
        [351] = new BrickColor(351, "Cork",		                new Color3(188, 155, 93)),
        [352] = new BrickColor(352, "Burlap",		            new Color3(199, 172, 120)),
        [353] = new BrickColor(353, "Beige",		            new Color3(202, 191, 163)),
        [354] = new BrickColor(354, "Oyster",		            new Color3(187, 179, 178)),
        [355] = new BrickColor(355, "Pine Cone",		        new Color3(108, 88, 75)),
        [356] = new BrickColor(356, "Fawn brown",		        new Color3(160, 132, 79)),
        [357] = new BrickColor(357, "Hurricane grey",	        new Color3(149, 137, 136)),
        [358] = new BrickColor(358, "Cloudy grey",		        new Color3(171, 168, 158)),
        [359] = new BrickColor(359, "Linen",		            new Color3(175, 148, 131)),
        [360] = new BrickColor(360, "Copper",		            new Color3(150, 103, 102)),
        [361] = new BrickColor(361, "Dirt brown",		        new Color3(86, 66, 54)),
        [362] = new BrickColor(362, "Bronze",		            new Color3(126, 104, 63)),
        [363] = new BrickColor(363, "Flint",		            new Color3(105, 102, 92)),
        [364] = new BrickColor(364, "Dark taupe",		        new Color3(90, 76, 66)),
        [365] = new BrickColor(365, "Burnt Sienna",		        new Color3(106, 57, 9)),
        [1001] = new BrickColor(1001, "Institutional white",	new Color3(248, 248, 248)),
        [1002] = new BrickColor(1002, "Mid gray",		        new Color3(205, 205, 205)),
        [1003] = new BrickColor(1003, "Really black",	        new Color3(17, 17, 17)),
        [1004] = new BrickColor(1004, "Really red",		        new Color3(255, 0, 0)),
        [1005] = new BrickColor(1005, "Deep orange",		    new Color3(255, 176, 0)),
        [1006] = new BrickColor(1006, "Alder",		            new Color3(180, 128, 255)),
        [1007] = new BrickColor(1007, "Dusty Rose",		        new Color3(163, 75, 75)),
        [1008] = new BrickColor(1008, "Olive",		            new Color3(193, 190, 66)),
        [1009] = new BrickColor(1009, "New Yeller",		        new Color3(255, 255, 0)),
        [1010] = new BrickColor(1010, "Really blue",		    new Color3(0, 0, 255)),
        [1011] = new BrickColor(1011, "Navy blue",		        new Color3(0, 32, 96)),
        [1012] = new BrickColor(1012, "Deep blue",		        new Color3(33, 84, 185)),
        [1013] = new BrickColor(1013, "Cyan",		            new Color3(4, 175, 236)),
        [1014] = new BrickColor(1014, "CGA brown",		        new Color3(170, 85, 0)),
        [1015] = new BrickColor(1015, "Magenta",		        new Color3(170, 0, 170)),
        [1016] = new BrickColor(1016, "Pink",		            new Color3(255, 102, 204)),
        [1017] = new BrickColor(1017, "Deep orange",		    new Color3(255, 175, 0)),
        [1018] = new BrickColor(1018, "Teal",		            new Color3(18, 238, 212)),
        [1019] = new BrickColor(1019, "Toothpaste",		        new Color3(0, 255, 255)),
        [1020] = new BrickColor(1020, "Lime green",		        new Color3(0, 255, 0)),
        [1021] = new BrickColor(1021, "Camo",		            new Color3(58, 125, 21)),
        [1022] = new BrickColor(1022, "Grime",		            new Color3(127, 142, 100)),
        [1023] = new BrickColor(1023, "Lavender",		        new Color3(140, 91, 159)),
        [1024] = new BrickColor(1024, "Pastel light blue",		new Color3(175, 221, 255)),
        [1025] = new BrickColor(1025, "Pastel orange",		    new Color3(255, 201, 201)),
        [1026] = new BrickColor(1026, "Pastel violet",		    new Color3(177, 167, 255)),
        [1027] = new BrickColor(1027, "Pastel blue-green",		new Color3(159, 243, 233)),
        [1028] = new BrickColor(1028, "Pastel green",		    new Color3(204, 255, 204)),
        [1029] = new BrickColor(1029, "Pastel yellow",		    new Color3(255, 255, 204)),
        [1030] = new BrickColor(1030, "Pastel brown",		    new Color3(255, 204, 153)),
        [1031] = new BrickColor(1031, "Royal purple",		    new Color3(98, 37, 209)),
        [1032] = new BrickColor(1032, "Hot pink",		        new Color3(255, 0, 19))
    };
    private static readonly Dictionary<int, int> PalleteToIndex = new Dictionary<int, int>()
    {
        [0] = 141,
        [1] = 301,
        [2] = 107,
        [3] = 26,
        [4] = 1012,
        [5] = 303,
        [6] = 1011,
        [7] = 304,
        [8] = 28,
        [9] = 1018,
        [10] = 302,
        [11] = 305,
        [12] = 306,
        [13] = 307,
        [14] = 308,
        [15] = 1021,
        [16] = 309,
        [17] = 310,
        [18] = 1019,
        [19] = 135,
        [20] = 102,
        [21] = 23,
        [22] = 1010,
        [23] = 312,
        [24] = 313,
        [25] = 37,
        [26] = 1022,
        [27] = 1020,
        [28] = 1027,
        [29] = 311,
        [30] = 315,
        [31] = 1023,
        [32] = 1031,
        [33] = 316,
        [34] = 151,
        [35] = 317,
        [36] = 318,
        [37] = 319,
        [38] = 1024,
        [39] = 314,
        [40] = 1013,
        [41] = 1006,
        [42] = 321,
        [43] = 322,
        [44] = 104,
        [45] = 1008,
        [46] = 119,
        [47] = 323,
        [48] = 324,
        [49] = 325,
        [50] = 320,
        [51] = 11,
        [52] = 1026,
        [53] = 1016,
        [54] = 1032,
        [55] = 1015,
        [56] = 327,
        [57] = 1005,
        [58] = 1009,
        [59] = 29,
        [60] = 328,
        [61] = 1028,
        [62] = 208,
        [63] = 45,
        [64] = 329,
        [65] = 330,
        [66] = 331,
        [67] = 1004,
        [68] = 21,
        [69] = 332,
        [70] = 333,
        [71] = 24,
        [72] = 334,
        [73] = 226,
        [74] = 1029,
        [75] = 335,
        [76] = 336,
        [77] = 342,
        [78] = 343,
        [79] = 338,
        [80] = 1007,
        [81] = 339,
        [82] = 133,
        [83] = 106,
        [84] = 340,
        [85] = 341,
        [86] = 1001,
        [87] = 1,
        [88] = 9,
        [89] = 1025,
        [90] = 337,
        [91] = 344,
        [92] = 345,
        [93] = 1014,
        [94] = 105,
        [95] = 346,
        [96] = 347,
        [97] = 348,
        [98] = 349,
        [99] = 1030,
        [100] = 125,
        [101] = 101,
        [102] = 350,
        [103] = 192,
        [104] = 351,
        [105] = 352,
        [106] = 353,
        [107] = 354,
        [108] = 1002,
        [109] = 5,
        [110] = 18,
        [111] = 217,
        [112] = 355,
        [113] = 356,
        [114] = 153,
        [115] = 357,
        [116] = 358,
        [117] = 359,
        [118] = 360,
        [119] = 38,
        [120] = 361,
        [121] = 362,
        [122] = 199,
        [123] = 194,
        [124] = 363,
        [125] = 364,
        [126] = 365,
        [127] = 1003
    };

    public static BrickColor[] AllBrickColors => AllColors.Values.ToArray();

    public static readonly BrickColor White = GetBrickColorByIndex(1);
    public static readonly BrickColor MediumStoneGrey = GetBrickColorByIndex(194);
    public static readonly BrickColor DarkStoneGrey = GetBrickColorByIndex(199);
    public static readonly BrickColor Black = GetBrickColorByIndex(26);
    public static readonly BrickColor Red = GetBrickColorByIndex(21);
    public static readonly BrickColor Yellow = GetBrickColorByIndex(24);
    public static readonly BrickColor Green = GetBrickColorByIndex(28);
    public static readonly BrickColor Blue = GetBrickColorByIndex(23);

    private static Random rng = new();

    public BrickColor(int index, string name, Color3 corresponding)
    {
        Index = index;
        Name = name;
        Color3 = corresponding;
    }

    public static BrickColor Random()
    {
        return AllBrickColors[rng.Next() % AllColors.Count];
    }
    public static BrickColor GetBrickColorByPalleteIndex(int id)
    {
        if (!PalleteToIndex.TryGetValue(id, out int regularid))
            return MediumStoneGrey;
        return GetBrickColorByIndex(regularid);
    }
    public static BrickColor GetBrickColorByIndex(int index)
    {
        if (!AllColors.ContainsKey(index))
            return MediumStoneGrey;
        return AllColors[index];
    }
    public static BrickColor GetBrickColorByName(string name)
    {
        for (int i = 0; i < AllColors.Count; i++)
        {
            KeyValuePair<int, BrickColor> brickColor = AllColors.ElementAt(i);
            if (brickColor.Value.Name.ToLowerInvariant().Trim() == name.ToLowerInvariant().Trim())
                return brickColor.Value;
        }
        return MediumStoneGrey;
    }
    // is this way too overengineered
    public static BrickColor GetBrickColorByClosestColor3(Color3 myColor)
    {
        BrickColor current = MediumStoneGrey;
        float closeness = -29329882824;
        for (int i = 0; i < AllColors.Count; i++)
        {
            KeyValuePair<int, BrickColor> brickColor = AllColors.ElementAt(i);
            Color3 sample = brickColor.Value.Color3;
            Vector3 tr_sample = new Vector3(sample.R - 128, sample.G - 128, sample.B - 128);
            Vector3 tr_target = new Vector3(myColor.R - 128, myColor.G - 128, myColor.B - 128);
            float dotproduct = Vector3.Dot(tr_sample, tr_target);

            if (dotproduct > closeness)
            {
                current = brickColor.Value;
                closeness = dotproduct;
            }
        }
        return current;
    }
}