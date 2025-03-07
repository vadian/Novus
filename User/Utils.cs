﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions {
	public static class Utils {
		//return a string with the first letter uppercased and the rest lower case for a single word
		public static string CamelCaseWord(this string input) {
            if (input.Length > 0) {
                return input[0].ToString().ToUpper() + input.Substring(1).ToLower();
            }
            return input;
		}

		//capitalizes the first letter in every word in a string
		public static string CamelCaseString(this string input) {
			StringBuilder sb = new StringBuilder();

			foreach (string word in input.Split(' ')) {
				sb.Append(CamelCaseWord(word) + " ");
			}

			return sb.ToString().Trim();
		}

		//just first word in sentence has first letter uppercased
		public static string UppercaseFirstWordInString( this string input) {
			return input.Trim().ToLower()[0].ToString().ToUpper() + input.Substring(1);
		}

		//uppercase the first letter of the first word after a period in a sentence.
		public static string UppercaseFirstLetterAfterPeriod(string input) {
			StringBuilder sb = new StringBuilder();
			foreach (string sentence in input.Split('.')) {
				sb.Append(UppercaseFirstWordInString(sentence));
			}

			return sb.ToString();
		}

		public static string FontStyle(this string input, FontStyles style) {
			return ("\x1b[" + (int)style + "m" + input + "\x1b[" + (int)FontStyles.RESET + "m");
		}

		public static string FontColor(this string input, FontForeColor color) {
			return ("\x1b[" + (int)color + "m" + input + "\x1b[" + (int)FontForeColor.RESET + "m");
		}

		public static string FontBackground(this string input, FontBackColor color) {
			return ("\x1b[" + (int)color + "m" + input + "\x1b[" + (int)FontBackColor.RESET + "m");
		}

		public enum FontForeColor { RED = 31, GREEN, YELLOW, BLUE, PURPLE, CYAN, RESET = 0 }
		public enum FontBackColor { RED = 41, GREEN, YELLOW, BLUE, PURPLE, CYAN, RESET = 0 }
		public enum FontStyles { BOLD = 1, ITALICS = 3, UNDERLINE, RESET = 0  }
	}

    public class RandomNumber {
        private static RandomNumber _randomGen;
        private static Random _rnd;

        public static RandomNumber GetRandomNumber() {
            return _randomGen ?? (_randomGen = new RandomNumber());
        }

        private RandomNumber() {
            _rnd = new Random();
        }

        public int NextNumber(double min, double max) {
            return _rnd.Next((int)min, (int)max);
        }
    }
}
