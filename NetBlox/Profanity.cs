using NetBlox.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox
{
	public static class Profanity
	{
		public static List<string> Words = [
			"gtbj", 
			"chubi", 
			"qtrrx", 
			"ehbj", 
			"bnbj", 
			"`rr", 
			"w`fho`", 
			"rihu", 
			"vi`u!uid!gtbj", 
			"rtbj!lx!ehbj", 
			"qdohr", 
			"bmhu", 
			"ohff`", 
			"rugt", 
			"oducmny!hr!vnsru!f`ld!dwds"
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
