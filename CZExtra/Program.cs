using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CZExtraScript
{
	class Program
	{
		static void Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var inputFile = args[0]; //@"E:\\Visual Novel Pack\\Clock Zero\\CZ Switch\\3.2 - Script\\extra-script\\703.txt"; //
			if (!inputFile.Contains(".txt"))
			{
				export(inputFile);
			}
			else
			{
				import(inputFile);
			}
		}

		static void export(string inputFile)
		{
			Quiz quizAnswer = null;
			using (BinaryReader b = new BinaryReader(File.Open(inputFile, FileMode.Open), Encoding.GetEncoding("shift_jis")))
			{

				quizAnswer = new Quiz();
				quizAnswer.read(b);

				var outputFile = inputFile + ".txt";
				using (StreamWriter w = new StreamWriter(File.Open(outputFile, FileMode.Create), Encoding.GetEncoding("utf-8")))
				{
					var count = 1;
					foreach (var entry in quizAnswer.entries)
					{
						w.Write($"{count},{entry._answerOffset},answer,{entry.answer}\n");
						w.Write($"{count},{entry._hintOffset},hint,{entry.hint}\n");

						var characters = "";
						foreach (var character in entry.characters)
						{
							characters += character;
						}
						w.Write($"{count},{entry._charactersOffset},chars,{characters}\n");
						count++;
					}
				}
			}
		}

		static void import(string inputFile)
		{
			var fileName = Path.GetFileName(inputFile);
			var folder = inputFile.Replace(fileName, string.Empty);
			var tableFile = folder + "table.txt";

			var inputText = File.ReadAllText(inputFile);

			var tableLines = File.ReadAllLines(tableFile);
			foreach (var line in tableLines)
			{
				var parts = line.Split('=');
				inputText = inputText.Replace(parts[0], parts[1]);
			}

			var alphabet = "qwertyuiopasdfghjklzxcvbnmQEWRTYUIOPASDFGHJKLZXCVBNM ";
			var outputFile = folder + "\\out\\" + fileName.Replace(".txt", string.Empty);
			File.Copy(inputFile.Replace(".txt", string.Empty), outputFile, true);
			FileInfo fileInfo = new FileInfo(outputFile);
			fileInfo.IsReadOnly = false;

			using (BinaryWriter b = new BinaryWriter(File.Open(outputFile, FileMode.Open), Encoding.GetEncoding("shift_jis")))
			{
				var inputLines = inputText.Split('\n');
				foreach (var line in inputLines)
				{
					if (line.Length > 0)
					{
						var parts = line.Split(',');
						var offset = Convert.ToInt64(parts[1]);
						var characters = parts[3].Trim().ToCharArray();
						b.BaseStream.Seek(offset, SeekOrigin.Begin);

						var charString = "";
						foreach (var character in characters)
						{
							charString += character;
							if (alphabet.Contains(character.ToString()))
							{
								charString += "|";
							}
						}

						byte[] utf8Bytes = Encoding.GetEncoding("utf-8").GetBytes(charString);
						var shiftjsBytes = Encoding.Convert(Encoding.GetEncoding("utf-8"), Encoding.GetEncoding("shift_jis"), utf8Bytes);

						var name = parts[2];

						switch (name)
						{
							case "answer":
							case "hint":
								b.Write(shiftjsBytes);
								b.Write((byte)0x00);
								break;
							case "chars":
								for (var i = 0; i < shiftjsBytes.Length; i++)
								{
									b.Write(shiftjsBytes[i]);
									if (i % 2 == 1)
									{
										b.Write((byte)0x00);
										b.Write((byte)0xFF);
									}
								}
								break;
						}
					}
				}
			}
		}
	}
}



public class Quiz
{
	public class QuizEntry
	{
		public long _offset;
		public long _answerOffset;
		public long _hintOffset;
		public long _charactersOffset;

		public UInt16 id;
		public string answer;
		public string hint;
		public List<string> characters;

		public void read(BinaryReader b)
		{
			_offset = b.BaseStream.Position;

			id = b.ReadUInt16();

			// Answer
			_answerOffset = b.BaseStream.Position;
			var letters = new List<byte>();
			var letter = b.ReadByte();
			while (letter != 0x00 && letter != 0xFF)
			{
				letters.Add(letter);
				letter = b.ReadByte();
			}
			answer = Encoding.GetEncoding("shift_jis").GetString(letters.ToArray()).Trim();

			// Hint
			b.BaseStream.Seek(_answerOffset + 90, SeekOrigin.Begin);

			_hintOffset = b.BaseStream.Position;
			letters = new List<byte>();
			letter = b.ReadByte();
			while (letter != 0x00 && letter != 0xFF)
			{
				letters.Add(letter);
				letter = b.ReadByte();
			}
			hint = Encoding.GetEncoding("shift_jis").GetString(letters.ToArray()).Trim();

			// Character
			b.BaseStream.Seek(_hintOffset + 30, SeekOrigin.Begin);
			_charactersOffset = b.BaseStream.Position;
			characters = new List<string>();
			for (var i = 0; i < 10; i++)
			{
				var character = Encoding.GetEncoding("shift_jis").GetString(b.ReadBytes(2)).Trim();
				characters.Add(character);
				b.ReadBytes(2);
			}
		}
	}

	public List<QuizEntry> entries;

	public void read(BinaryReader b)
	{
		b.BaseStream.Seek(8, SeekOrigin.Begin);

		entries = new List<QuizEntry>();
		while (b.BaseStream.Position < b.BaseStream.Length)
		{
			var entry = new QuizEntry();
			entry.read(b);
			entries.Add(entry);

			b.BaseStream.Seek(entry._offset, SeekOrigin.Begin);
			b.BaseStream.Seek(198, SeekOrigin.Current);
		}
	}
}