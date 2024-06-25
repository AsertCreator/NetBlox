using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox
{
	public static class Profanity
	{
		public static List<string> Words = [
			"fuck",
			"bitch",
			"pussy",
			"dick",
			"cock",
			"ass",
			"vagina",
			"shit",
			"what the fuck",
			"suck my dick",
			"penis",
			"clit",
			"nigga",
			"stfu",
			"netblox is worst game ever"
		]; // i know so much things lol
		// i hope github wont kill me for this lol
		public static string Filter(string msg)
		{
			string cleany = msg;
			for (int i = 0; i < Words.Count; i++)
				cleany = cleany.Replace(Words[i], new string('#', Words[i].Length));
			return cleany;
		}
	}
}
