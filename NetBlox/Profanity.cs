using NetBlox.Common;

namespace NetBlox
{
	public static class Profanity
	{
		public static List<string> Words = [
			"`rr", "`rrg`f", "`rrw`f", "`rrw`fho`", "`rrehbj", "`rrbnbj", "`rrinmd", "`rrbtbj", "`rrchubi", "`rrbtou",
			"`rrlnuidsgtbj", "`rrlnuidsgtbjds", "`rrlg", "`rrgtbj", "`rrgtbjhof", "`rrchubihof", "`rrbnbjhof", "`rrbtouhof",
			"`rrbnnjhof", "`rrg`ffnu", "g`f", "g`f`rr", "g`fw`f", "g`fw`fho`", "g`fehbj", "g`fbnbj", "g`finmd", "g`fbtbj",
			"g`fchubi", "g`fbtou", "g`flnuidsgtbj", "g`flnuidsgtbjds", "g`flg", "g`fgtbj", "g`fgtbjhof", "g`fchubihof",
			"g`fbnbjhof", "g`fbtouhof", "g`fbnnjhof", "g`ffnu", "g`ffnu`rr", "g`ffnuw`f", "g`ffnuw`fho`", "g`ffnuehbj",
			"g`ffnubnbj", "g`ffnuinmd", "g`ffnubtbj", "g`ffnuchubi", "g`ffnubtou", "g`ffnulnuidsgtbj", "g`ffnulnuidsgtbjds",
			"g`ffnulg", "g`ffnugtbj", "g`ffnugtbjhof", "g`ffnuchubihof", "g`ffnubnbjhof", "g`ffnubtouhof", "g`ffnubnnjhof",
			"w`f", "w`fg`f", "w`f`rr", "w`fehbj", "w`fbnbj", "w`finmd", "w`fbtbj", "w`fchubi", "w`fbtou", "w`flnuidsgtbj",
			"w`flnuidsgtbjds", "w`flg", "w`fgtbj", "w`fgtbjhof", "w`fchubihof", "w`fbnbjhof", "w`fbtouhof", "w`fbnnjhof",
			"w`fg`ffnu", "w`fho`", "w`fho`g`f", "w`fho``rr", "w`fho`ehbj", "w`fho`bnbj", "w`fho`inmd", "w`fho`btbj",
			"w`fho`chubi", "w`fho`btou", "w`fho`lnuidsgtbj", "w`fho`lnuidsgtbjds", "w`fho`lg", "w`fho`gtbj", "w`fho`gtbjhof",
			"w`fho`chubihof", "w`fho`bnbjhof", "w`fho`btouhof", "w`fho`bnnjhof", "w`fho`g`ffnu", "ehbj", "ehbjg`f", "ehbjw`f",
			"ehbjw`fho`", "ehbj`rr", "ehbjid`e", "ehbjbnbj", "ehbjinmd", "ehbjbtbj", "ehbjchubi", "ehbjbtou", "ehbjlnuidsgtbj",
			"ehbjlnuidsgtbjds", "ehbjlg", "ehbjgtbj", "ehbjgtbjhof", "ehbjchubihof", "ehbjbnbjhof", "ehbjbtouhof", "ehbjbnnjhof",
			"ehbjg`ffnu", "bnbj", "bnbjg`f", "bnbjw`f", "bnbjw`fho`", "bnbjehbj", "bnbj`rr", "bnbjinmd", "bnbjbtbj", "bnbjchubi",
			"bnbjbtou", "bnbjlnuidsgtbj", "bnbjlnuidsgtbjds", "bnbjlg", "bnbjgtbj", "bnbjgtbjhof", "bnbjchubihof", "bnbjbnbjhof",
			"bnbjbtouhof", "bnbjbnnjhof", "bnbjg`ffnu", "inmd", "inmdg`f", "inmdw`f", "inmdw`fho`", "inmdehbj", "inmd`rr",
			"inmdbnbj", "inmdbtbj", "inmdchubi", "inmdbtou", "inmdlnuidsgtbj", "inmdlnuidsgtbjds", "inmdlg", "inmdgtbj",
			"inmdgtbjhof", "inmdchubihof", "inmdbnbjhof", "inmdbtouhof", "inmdbnnjhof", "inmdg`ffnu", "btbj", "btbjg`f",
			"btbjw`f", "btbjw`fho`", "btbjehbj", "btbj`rr", "btbjbnbj", "btbjinmd", "btbjchubi", "btbjbtou", "btbjlnuidsgtbj",
			"btbjlnuidsgtbjds", "btbjlg", "btbjgtbj", "btbjgtbjhof", "btbjchubihof", "btbjbnbjhof", "btbjbtouhof", "btbjbnnjhof",
			"btbjg`ffnu", "btbjnme", "btbjnmeg`f", "btbjnmew`f", "btbjnmew`fho`", "btbjnmeehbj", "btbjnme`rr", "btbjnmebnbj",
			"btbjnmeinmd", "btbjnmechubi", "btbjnmebtou", "btbjnmelnuidsgtbj", "btbjnmelnuidsgtbjds", "btbjnmelg", "btbjnmegtbj",
			"btbjnmegtbjhof", "btbjnmechubihof", "btbjnmebnbjhof", "btbjnmebtouhof", "btbjnmebnnjhof", "btbjnmeg`ffnu",
			"chubi", "chubig`f", "chubiw`f", "chubiw`fho`", "chubiehbj", "chubi`rr", "chubibnbj", "chubiinmd", "chubibtbj",
			"chubibtou", "chubilnuidsgtbj", "chubilnuidsgtbjds", "chubilg", "chubigtbj", "chubigtbjhof", "chubichubihof",
			"chubibnbjhof", "chubibtouhof", "chubibnnjhof", "chubig`ffnu", "btou", "btoug`f", "btouw`f", "btouw`fho`", "btouehbj",
			"btou`rr", "btoubnbj", "btouinmd", "btoubtbj", "btouchubi", "btoulnuidsgtbj", "btoulnuidsgtbjds", "btoulg", "btougtbj",
			"btougtbjhof", "btouchubihof", "btoubnbjhof", "btoubtouhof", "btoubnnjhof", "btoug`ffnu", "lnuidsgtbj", "lnuidsgtbjg`f",
			"lnuidsgtbjw`f", "lnuidsgtbjw`fho`", "lnuidsgtbjehbj", "lnuidsgtbj`rr", "lnuidsgtbjbnbj", "lnuidsgtbjinmd",
			"lnuidsgtbjbtbj", "lnuidsgtbjchubi", "lnuidsgtbjbtou", "lnuidsgtbjlnuidsgtbjds", "lnuidsgtbjlg", "lnuidsgtbjgtbj",
			"lnuidsgtbjgtbjhof", "lnuidsgtbjchubihof", "lnuidsgtbjbnbjhof", "lnuidsgtbjbtouhof", "lnuidsgtbjbnnjhof",
			"lnuidsgtbjg`ffnu", "lnuidsgtbjds", "lnuidsgtbjdsg`f", "lnuidsgtbjdsw`f", "lnuidsgtbjdsw`fho`", "lnuidsgtbjdsehbj",
			"lnuidsgtbjds`rr", "lnuidsgtbjdsbnbj", "lnuidsgtbjdsinmd", "lnuidsgtbjdsbtbj", "lnuidsgtbjdschubi", "lnuidsgtbjdsbtou",
			"lnuidsgtbjdslg", "lnuidsgtbjdsgtbj", "lnuidsgtbjdsgtbjhof", "lnuidsgtbjdschubihof", "lnuidsgtbjdsbnbjhof",
			"lnuidsgtbjdsbtouhof", "lnuidsgtbjdsbnnjhof", "lnuidsgtbjdsg`ffnu", "lg", "lgg`f", "lgw`f", "lgw`fho`", "lgehbj", "lg`rr",
			"lgbnbj", "lginmd", "lgbtbj", "lgchubi", "lgbtou", "lglnuidsgtbj", "lglnuidsgtbjhof", "lglg", "lggtbj", "lggtbjhof",
			"lgchubihof", "lgbnbjhof", "lgbtouhof", "lgbnnjhof", "lgg`ffnu", "gtbj", "gtbjr", "gtbjhof", "gtbjds", "gnnjds", "gttjds",
			"gtbjg`f", "gtbjw`f", "gtbjw`fho`", "gtbjehbj", "gtbj`rr", "gtbjbnbj", "gtbjinmd", "gtbjbtbj", "gtbjchubi", "gtbjbtou",
			"gtbjlnuidsgtbj", "gtbjlnuidsgtbjhof", "gtbjlg", "gtbjgtbj", "gtbjgtbjhof", "gtbjchubihof", "gtbjbnbjhof", "gtbjbtouhof",
			"gtbjbnnjhof", "gtbjg`ffnu", "chubihof", "chubihofg`f", "chubihofw`f", "chubihofw`fho`", "chubihofehbj", "chubihof`rr",
			"chubihofbnbj", "chubihofinmd", "chubihofbtbj", "chubihofchubi", "chubihofbtou", "chubihoflg", "chubihofgtbj",
			"chubihofgtbjhof", "chubihofgtbj", "chubihofbnbjhof", "chubihofbtouhof", "chubihofbnnjhof", "chubihofg`ffnu", "bnbjhof",
			"bnbjhofg`f", "bnbjhofw`f", "bnbjhofw`fho`", "bnbjhofehbj", "bnbjhof`rr", "bnbjhofbnbj", "bnbjhofinmd", "bnbjhofbtbj",
			"bnbjhofchubi", "bnbjhofbtou", "bnbjhoflg", "bnbjhofgtbj", "bnbjhofgtbjhof", "bnbjhofgtbj", "bnbjhofchubihof",
			"bnbjhofbtouhof", "bnbjhofbnnjhof", "bnbjhofg`ffnu", "btouhof", "btouhofg`f", "btouhofw`f", "btouhofw`fho`", "btouhofehbj",
			"btouhof`rr", "btouhofbnbj", "btouhofinmd", "btouhofbtbj", "btouhofchubi", "btouhofbtou", "btouhoflg", "btouhofgtbj",
			"btouhofgtbjhof", "btouhofgtbj", "btouhofchubihof", "btouhofbnbjhof", "btouhofbnnjhof", "btouhofg`ffnu", "ohff`",
			"ohffds", "ohff`r", "ohffdsr", "odff`", "odffds", "odff`", "odff`r", "s`qd", "jhmm!xntsrdmg", "jxr", "0599", "cm`bjohff`",
			"cm`bjohff`r", "cm`bjohffds", "cm`bjohffdsr", "ohf`", "ohf``", "oducmny!hr!uid!vnsru!f`ld!dwds", "wnud!sdqtcmhb`o",
			"wnud!edlnbs`u", "wnud!mhcdsu`sh`o", "wnud!fsddo", "wnud!sdgnsl", "wnud!m`cnts", "wnud!bnordsw`uhwd", "wnud!mhcedl",
			"wnud!fsddor", "rihu", "rihuuhof", "rihu`rr", "rihugtbj", "rihug`f", "rihug`ffnu", "`rrronuude", "en!78", "en!87", "dsnuhb",
			"dsnuhb`mmx", "qnso", "qnson", "qsno", "qsnon", "qnsonfsq`ix", "qsnonfsq`ix", "iuuqr;..", "iuuq;..", "guq;..", "u/ld",
			"udmdfs`l", "vi`ur`qq", "*0", "*6", "qinod!otlcds", "cg", "cnxgshdoe", "cnxgshdoer", "fg", "fhsmgshdoe", "fhsmgshdoer",
			"nens", "kdv", "kdvhri", ")", "(", "\"",  "\"\"",  "\"\"\"",  "\"\"\"\"",  "\"\"\"\"\"",  "\"\"\"\"\"\"",  "\"\"\"\"\"\"\"", 
			"\"\"\"\"\"\"\"\"",
		];
		private static bool setup = false;
		// profanity is encrypted now
		public static string Filter(string msg)
		{
			if (!setup)
			{
				for (int i = 0; i < Words.Count; i++) 
					Words[i] = MathE.Roll(Words[i], 1);
				setup = true;
			}

			string cleany = msg;
			for (int i = 0; i < Words.Count; i++)
				cleany = cleany.Replace(Words[i], new string('#', Words[i].Length));
			return cleany;
		}
	}
}
