﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Persistence;
using Waher.Persistence.Serialization;
using Waher.Persistence.XmlLedger;
using Waher.Runtime.Console;
using Waher.Runtime.Temporary;
using Waher.Utility.Extract.ExportFormats;

namespace Waher.Utility.Extract
{
	class Program
	{
		private const byte TYPE_BOOLEAN = 0;
		private const byte TYPE_BYTE = 1;
		private const byte TYPE_INT16 = 2;
		private const byte TYPE_INT32 = 3;
		private const byte TYPE_INT64 = 4;
		private const byte TYPE_SBYTE = 5;
		private const byte TYPE_UINT16 = 6;
		private const byte TYPE_UINT32 = 7;
		private const byte TYPE_UINT64 = 8;
		private const byte TYPE_DECIMAL = 9;
		private const byte TYPE_DOUBLE = 10;
		private const byte TYPE_SINGLE = 11;
		private const byte TYPE_DATETIME = 12;
		private const byte TYPE_TIMESPAN = 13;
		private const byte TYPE_CHAR = 14;
		private const byte TYPE_STRING = 15;
		private const byte TYPE_ENUM = 16;
		private const byte TYPE_BYTEARRAY = 17;
		private const byte TYPE_GUID = 18;
		private const byte TYPE_DATETIMEOFFSET = 19;
		private const byte TYPE_CI_STRING = 20;
		private const byte TYPE_NULL = 29;
		private const byte TYPE_ARRAY = 30;
		private const byte TYPE_OBJECT = 31;
		private const string Preamble = "IoT Gateway Export";

		/// <summary>
		/// Extracts content from a backup file generated by the IoT Gateway.
		/// 
		/// Command line switches:
		/// 
		/// -i FILENAME           Filename of input backup file.
		/// -k FILENAME           Optional key file name, if backup file is
		///                       encrypted.
		/// -o FOLDER             Folder where extracted information will be
		///                       stored.
		/// -a                    Extract from all information. Same as
		///                       -d -l -f
		/// -c COLLECTION         Extract named collection
		/// -fn FILENAME          Extract a file with a given name
		/// -d                    Extract Object Database information
		///                       If no collections have been specified,
		///                       all collections will be extracted.
		/// -l                    Extract Ledger information
		///                       If no collections have been specified,
		///                       all collections will be extracted.
		/// -f                    Extract Files
		///                       If no files have been specified,
		///                       all collections will be extracted.
		/// -?                    Help.
		/// </summary>
		static int Main(string[] args)
		{
			Extractor Extractor = null;
			FileStream InputFile = null;
			FileStream KeyFile = null;
			Dictionary<string, bool> Collections = null;
			Dictionary<string, bool> FileNames = null;
			string InputFileName = null;
			string KeyFileName = null;
			string OutputFolder = null;
			string s;
			int i = 0;
			int c = args.Length;
			bool Help = false;
			bool Database = false;
			bool Ledger = false;
			bool Files = false;

			try
			{
				while (i < c)
				{
					s = args[i++].ToLower();

					switch (s)
					{
						case "-i":
							if (i >= c)
								throw new Exception("Missing filename.");

							if (!string.IsNullOrEmpty(InputFileName))
								throw new Exception("Input file name already provided.");

							InputFileName = args[i++];
							if (!File.Exists(InputFileName))
								throw new Exception("File not found: " + InputFileName);
							break;

						case "-k":
							if (i >= c)
								throw new Exception("Missing filename.");

							if (!string.IsNullOrEmpty(KeyFileName))
								throw new Exception("Key file name already provided.");

							KeyFileName = args[i++];
							if (!File.Exists(KeyFileName))
								throw new Exception("File not found: " + KeyFileName);
							break;

						case "-o":
							if (i >= c)
								throw new Exception("Missing folder.");

							if (!string.IsNullOrEmpty(OutputFolder))
								throw new Exception("Output folder already provided.");

							OutputFolder = args[i++];
							break;

						case "-c":
							if (i >= c)
								throw new Exception("Missing collection name.");

							Collections ??= new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

							Collections[args[i++]] = true;
							break;

						case "-fn":
							if (i >= c)
								throw new Exception("Missing file name.");

							FileNames ??= new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

							FileNames[args[i++]] = true;
							break;

						case "-?":
							Help = true;
							break;

						case "-a":
							Database = true;
							Ledger = true;
							Files = true;
							break;

						case "-d":
							Database = true;
							break;

						case "-l":
							Ledger = true;
							break;

						case "-f":
							Files = true;
							break;

						default:
							throw new Exception("Unrecognized switch: " + s);
					}
				}

				if (Help || c == 0)
				{
					ConsoleOut.WriteLine("Extracts content from a backup file generated by the IoT Gateway.");
					ConsoleOut.WriteLine("");
					ConsoleOut.WriteLine("Command line switches:");
					ConsoleOut.WriteLine("");
					ConsoleOut.WriteLine("-i FILENAME           Filename of input backup file.");
					ConsoleOut.WriteLine("-k FILENAME           Optional key file name, if backup file is");
					ConsoleOut.WriteLine("                      encrypted.");
					ConsoleOut.WriteLine("-o FOLDER             Folder where extracted information will be");
					ConsoleOut.WriteLine("                      stored.");
					ConsoleOut.WriteLine("-a                    Extract from all information. Same as");
					ConsoleOut.WriteLine("                      -d -l -f");
					ConsoleOut.WriteLine("-c COLLECTION         Extract named collection");
					ConsoleOut.WriteLine("-fn FILENAME          Extract a file with a given name");
					ConsoleOut.WriteLine("-d                    Extract Object Database information");
					ConsoleOut.WriteLine("                      If no collections have been specified,");
					ConsoleOut.WriteLine("                      all collections will be extracted.");
					ConsoleOut.WriteLine("-l                    Extract Ledger information");
					ConsoleOut.WriteLine("                      If no collections have been specified,");
					ConsoleOut.WriteLine("                      all collections will be extracted.");
					ConsoleOut.WriteLine("-f                    Extract Files");
					ConsoleOut.WriteLine("                      If no files have been specified,");
					ConsoleOut.WriteLine("                      all collections will be extracted.");
					ConsoleOut.WriteLine("-?                    Help.");
					return 0;
				}

				if (string.IsNullOrEmpty(InputFileName))
					throw new Exception("No input file name provided.");

				InputFile = File.OpenRead(InputFileName);
				KeyFile = !string.IsNullOrEmpty(KeyFileName) ? File.OpenRead(KeyFileName) : null;

				Extractor = new Extractor(OutputFolder, Database, Ledger, Files, Collections, FileNames);

				DoImport(InputFile, KeyFile, Path.GetExtension(InputFileName), Extractor).Wait();

				return 0;
			}
			catch (Exception ex)
			{
				ConsoleOut.WriteLine(ex.Message);
				return -1;
			}
			finally
			{
				Extractor?.Dispose();
				InputFile?.Dispose();
				KeyFile?.Dispose();

				ConsoleOut.Flush(true);
			}
		}

		private static async Task DoImport(FileStream BackupFile, FileStream KeyFile, string Extension, Extractor Import)
		{
			BackupFile.Position = 0;

			switch (Extension.ToLower())
			{
				case ".xml":
					await RestoreXml(BackupFile, Import);
					break;

				case ".bin":
					await RestoreBinary(BackupFile, Import);
					break;

				case ".gz":
					await RestoreCompressed(BackupFile, Import);
					break;

				case ".bak":
					if (KeyFile is null)
						throw new Exception("No key file provided.");

					KeyFile.Position = 0;

					await RestoreEncrypted(BackupFile, KeyFile, Import);
					break;

				default:
					throw new Exception("Unrecognized file extension: " + Extension);
			}
		}

		private static async Task RestoreXml(Stream BackupFile, Extractor Import)
		{
			XmlReaderSettings Settings = new()
			{
				Async = true,
				CloseInput = true,
				ConformanceLevel = ConformanceLevel.Document,
				CheckCharacters = true,
				DtdProcessing = DtdProcessing.Prohibit,
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true
			};
			XmlReader r = XmlReader.Create(BackupFile, Settings);
			KeyValuePair<string, object> P;
			bool DatabaseStarted = false;
			bool LedgerStarted = false;
			bool CollectionStarted = false;
			bool IndexStarted = false;
			bool BlockStarted = false;
			bool FilesStarted = false;

			if (!r.ReadToFollowing("Export", XmlFileLedger.Namespace))
				throw new Exception("Invalid backup XML file.");

			await Import.Start();

			while (r.Read())
			{
				if (r.IsStartElement())
				{
					switch (r.LocalName)
					{
						case "Database":
							if (r.Depth != 1)
								throw new Exception("Database element not expected.");

							await Import.StartDatabase();
							DatabaseStarted = true;
							break;

						case "Ledger":
							if (r.Depth != 1)
								throw new Exception("Ledger element not expected.");

							await Import.StartLedger();
							LedgerStarted = true;
							break;

						case "Collection":
							if (r.Depth != 2 || (!DatabaseStarted && !LedgerStarted))
								throw new Exception("Collection element not expected.");

							if (!r.MoveToAttribute("name"))
								throw new Exception("Collection name missing.");

							string CollectionName = r.Value;

							if (IndexStarted)
							{
								await Import.EndIndex();
								IndexStarted = false;
							}
							else if (BlockStarted)
							{
								await Import.EndBlock();
								BlockStarted = false;
							}

							if (CollectionStarted)
								await Import.EndCollection();

							await Import.StartCollection(CollectionName);
							CollectionStarted = true;
							break;

						case "Index":
							if (r.Depth != 3 || !CollectionStarted)
								throw new Exception("Index element not expected.");

							await Import.StartIndex();
							IndexStarted = true;
							break;

						case "Field":
							if (r.Depth != 4 || !IndexStarted)
								throw new Exception("Field element not expected.");

							if (r.MoveToFirstAttribute())
							{
								string FieldName = null;
								bool Ascending = true;

								do
								{
									switch (r.LocalName)
									{
										case "name":
											FieldName = r.Value;
											break;

										case "ascending":
											if (!CommonTypes.TryParse(r.Value, out Ascending))
												throw new Exception("Invalid boolean value.");
											break;

										case "xmlns":
											break;

										default:
											throw new Exception("Unexpected attribute: " + r.LocalName);
									}
								}
								while (r.MoveToNextAttribute());

								if (string.IsNullOrEmpty(FieldName))
									throw new Exception("Invalid field name.");

								await Import.ReportIndexField(FieldName, Ascending);
							}
							else
								throw new Exception("Field attributes expected.");

							break;

						case "Obj":
							if (r.Depth == 3 && CollectionStarted)
							{
								if (IndexStarted)
								{
									await Import.EndIndex();
									IndexStarted = false;
								}

								using (XmlReader r2 = r.ReadSubtree())
								{
									r2.Read();

									if (!r2.MoveToFirstAttribute())
										throw new Exception("Object attributes missing.");

									string ObjectId = null;
									string TypeName = string.Empty;

									do
									{
										switch (r2.LocalName)
										{
											case "id":
												ObjectId = r2.Value;
												break;

											case "type":
												TypeName = r2.Value;
												break;

											case "xmlns":
												break;

											default:
												throw new Exception("Unexpected attribute: " + r2.LocalName);
										}
									}
									while (r2.MoveToNextAttribute());

									await Import.StartObject(ObjectId, TypeName);

									while (r2.Read())
									{
										if (r2.IsStartElement())
										{
											P = ReadValue(r2);

											await Import.ReportProperty(P.Key, P.Value);
										}
									}
								}

								await Import.EndObject();
							}
							else
								throw new Exception("Obj element not expected.");

							break;

						case "Block":
							if (r.Depth != 3 || !CollectionStarted)
								throw new Exception("Block element not expected.");

							if (!r.MoveToAttribute("id"))
								throw new Exception("Block ID missing.");

							string BlockID = r.Value;

							await Import.StartBlock(BlockID);
							BlockStarted = true;
							break;

						case "MetaData":
							if (r.Depth == 4 && BlockStarted)
							{
								using XmlReader r2 = r.ReadSubtree();
								r2.Read();

								while (r2.Read())
								{
									if (r2.IsStartElement())
									{
										P = ReadValue(r2);
										await Import.BlockMetaData(P.Key, P.Value);
									}
								}
							}
							else
								throw new Exception("MetaData element not expected.");
							break;

						case "New":
						case "Update":
						case "Delete":
						case "Clear":
							if (r.Depth != 4 || !CollectionStarted || !BlockStarted)
								throw new Exception("Entry element not expected.");

							if (!Enum.TryParse(r.LocalName, out EntryType EntryType))
								throw new Exception("Unexpected element: " + r.LocalName);

							using (XmlReader r2 = r.ReadSubtree())
							{
								r2.Read();

								if (!r2.MoveToFirstAttribute())
									throw new Exception("Object attributes missing.");

								string ObjectId = null;
								string TypeName = string.Empty;
								DateTimeOffset EntryTimestamp = DateTimeOffset.MinValue;

								do
								{
									switch (r2.LocalName)
									{
										case "id":
											ObjectId = r2.Value;
											break;

										case "type":
											TypeName = r2.Value;
											break;

										case "ts":
											if (!XML.TryParse(r2.Value, out EntryTimestamp))
												throw new Exception("Invalid Entry Timestamp: " + r2.Value);
											break;

										case "xmlns":
											break;

										default:
											throw new Exception("Unexpected attribute: " + r2.LocalName);
									}
								}
								while (r2.MoveToNextAttribute());

								await Import.StartEntry(ObjectId, TypeName, EntryType, EntryTimestamp);

								while (r2.Read())
								{
									if (r2.IsStartElement())
									{
										P = ReadValue(r2);
										await Import.ReportProperty(P.Key, P.Value);
									}
								}
							}

							await Import.EndEntry();
							break;

						case "Files":
							if (r.Depth != 1)
								throw new Exception("Files element not expected.");

							if (IndexStarted)
							{
								await Import.EndIndex();
								IndexStarted = false;
							}
							else if (BlockStarted)
							{
								await Import.EndBlock();
								BlockStarted = false;
							}

							if (CollectionStarted)
							{
								await Import.EndCollection();
								CollectionStarted = false;
							}

							if (DatabaseStarted)
							{
								await Import.EndDatabase();
								DatabaseStarted = false;
							}
							else if (LedgerStarted)
							{
								await Import.EndLedger();
								LedgerStarted = false;
							}

							await Import.StartFiles();
							FilesStarted = true;
							break;

						case "File":
							if (r.Depth != 2 || !FilesStarted)
								throw new Exception("File element not expected.");

							using (XmlReader r2 = r.ReadSubtree())
							{
								r2.Read();

								if (!r2.MoveToAttribute("fileName"))
									throw new Exception("File name missing.");

								string FileName = r.Value;

								using TemporaryFile fs = new();
								
								while (r2.Read())
								{
									if (r2.IsStartElement())
									{
										while (r2.LocalName == "Chunk")
										{
											string Base64 = r2.ReadElementContentAsString();
											byte[] Data = Convert.FromBase64String(Base64);
											fs.Write(Data, 0, Data.Length);
										}
									}
								}

								fs.Position = 0;
								await Import.ExportFile(FileName, fs);
							}
							break;

						default:
							throw new Exception("Unexpected element: " + r.LocalName);
					}
				}
			}

			if (IndexStarted)
				await Import.EndIndex();
			else if (BlockStarted)
				await Import.EndBlock();

			if (CollectionStarted)
				await Import.EndCollection();

			if (DatabaseStarted)
				await Import.EndDatabase();
			else if (LedgerStarted)
				await Import.EndLedger();

			if (FilesStarted)
				await Import.EndFiles();

			await Import.End();
		}

		private static KeyValuePair<string, object> ReadValue(XmlReader r)
		{
			string PropertyType = r.LocalName;
			bool ReadSubtree = (PropertyType == "Bin" || PropertyType == "Array" || PropertyType == "Obj");

			if (ReadSubtree)
			{
				r = r.ReadSubtree();
				r.Read();
			}

			if (!r.MoveToFirstAttribute())
			{
				if (ReadSubtree)
					r.Dispose();

				throw new Exception("Property attributes missing.");
			}

			string ElementType = null;
			string PropertyName = null;
			object Value = null;

			do
			{
				switch (r.LocalName)
				{
					case "n":
						PropertyName = r.Value;
						break;

					case "v":
						switch (PropertyType)
						{
							case "S":
							case "En":
								Value = r.Value;
								break;

							case "S64":
								Value = Encoding.UTF8.GetString(Convert.FromBase64String(r.Value));
								break;

							case "Null":
								Value = null;
								break;

							case "Bl":
								if (CommonTypes.TryParse(r.Value, out bool bl))
									Value = bl;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid boolean value.");
								}
								break;

							case "B":
								if (byte.TryParse(r.Value, out byte b))
									Value = b;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid byte value.");
								}
								break;

							case "Ch":
								string s = r.Value;
								if (s.Length == 1)
									Value = s[0];
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid character value.");
								}
								break;

							case "CIS":
								Value = new CaseInsensitiveString(r.Value);
								break;

							case "CIS64":
								Value = new CaseInsensitiveString(Encoding.UTF8.GetString(Convert.FromBase64String(r.Value)));
								break;

							case "DT":
								if (XML.TryParse(r.Value, out DateTime DT))
									Value = DT;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid DateTime value.");
								}
								break;

							case "DTO":
								if (XML.TryParse(r.Value, out DateTimeOffset DTO))
									Value = DTO;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid DateTimeOffset value.");
								}
								break;

							case "Dc":
								if (CommonTypes.TryParse(r.Value, out decimal dc))
									Value = dc;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Decimal value.");
								}
								break;

							case "Db":
								if (CommonTypes.TryParse(r.Value, out double db))
									Value = db;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Double value.");
								}
								break;

							case "I2":
								if (short.TryParse(r.Value, out short i2))
									Value = i2;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Int16 value.");
								}
								break;

							case "I4":
								if (int.TryParse(r.Value, out int i4))
									Value = i4;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Int32 value.");
								}
								break;

							case "I8":
								if (long.TryParse(r.Value, out long i8))
									Value = i8;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Int64 value.");
								}
								break;

							case "I1":
								if (sbyte.TryParse(r.Value, out sbyte i1))
									Value = i1;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid SByte value.");
								}
								break;

							case "Fl":
								if (CommonTypes.TryParse(r.Value, out float fl))
									Value = fl;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid Single value.");
								}
								break;

							case "U2":
								if (ushort.TryParse(r.Value, out ushort u2))
									Value = u2;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid UInt16 value.");
								}
								break;

							case "U4":
								if (uint.TryParse(r.Value, out uint u4))
									Value = u4;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid UInt32 value.");
								}
								break;

							case "U8":
								if (ulong.TryParse(r.Value, out ulong u8))
									Value = u8;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid UInt64 value.");
								}
								break;

							case "TS":
								if (TimeSpan.TryParse(r.Value, out TimeSpan TS))
									Value = TS;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid TimeSpan value.");
								}
								break;

							case "Bin":
								if (ReadSubtree)
									r.Dispose();

								throw new Exception("Binary member values are reported using child elements.");

							case "ID":
								if (Guid.TryParse(r.Value, out Guid Id))
									Value = Id;
								else
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Invalid GUID value.");
								}
								break;

							case "Array":
								if (ReadSubtree)
									r.Dispose();

								throw new Exception("Arrays report values as child elements.");

							case "Obj":
								if (ReadSubtree)
									r.Dispose();

								throw new Exception("Objects report member values as child elements.");

							default:
								if (ReadSubtree)
									r.Dispose();

								throw new Exception("Unexpected property type: " + PropertyType);
						}
						break;

					case "elementType":
					case "type":
						ElementType = r.Value;
						break;

					case "xmlns":
						break;

					default:
						if (ReadSubtree)
							r.Dispose();

						throw new Exception("Unexpected attribute: " + r.LocalName);
				}
			}
			while (r.MoveToNextAttribute());

			if (ElementType is not null)
			{
				switch (PropertyType)
				{
					case "Array":
						List<object> List = new();

						while (r.Read())
						{
							if (r.IsStartElement())
							{
								KeyValuePair<string, object> P = ReadValue(r);
								if (!string.IsNullOrEmpty(P.Key))
								{
									if (ReadSubtree)
										r.Dispose();

									throw new Exception("Arrays do not contain property names.");
								}

								List.Add(P.Value);
							}
							else if (r.NodeType == XmlNodeType.EndElement)
								break;
						}

						Value = List.ToArray();
						break;

					case "Obj":
						GenericObject GenObj = new(string.Empty, ElementType, Guid.Empty);
						Value = GenObj;

						while (r.Read())
						{
							if (r.IsStartElement())
							{
								KeyValuePair<string, object> P = ReadValue(r);
								GenObj[P.Key] = P.Value;
							}
							else if (r.NodeType == XmlNodeType.EndElement)
								break;
						}
						break;

					default:
						if (ReadSubtree)
							r.Dispose();

						throw new Exception("Type only valid option for arrays and objects.");
				}
			}
			else if (PropertyType == "Bin")
			{
				MemoryStream Bin = new();

				while (r.Read())
				{
					if (r.IsStartElement())
					{
						try
						{
							while (r.LocalName == "Chunk")
							{
								string Base64 = r.ReadElementContentAsString();
								byte[] Data = Convert.FromBase64String(Base64);
								Bin.Write(Data, 0, Data.Length);
							}
						}
						catch (Exception ex)
						{
							if (ReadSubtree)
								r.Dispose();

							System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
						}
					}
					else if (r.NodeType == XmlNodeType.EndElement)
						break;
				}

				Value = Bin.ToArray();
			}

			if (ReadSubtree)
				r.Dispose();

			return new KeyValuePair<string, object>(PropertyName, Value);
		}

		private static async Task RestoreBinary(Stream BackupFile, Extractor Import)
		{
			int Version = BackupFile.ReadByte();
			if (Version != 1)
				throw new Exception("File version not supported.");

			byte Command;

			using BinaryReader r = new(BackupFile, Encoding.UTF8, true);
			string s = r.ReadString();
			if (s != Preamble)
				throw new Exception("Invalid backup file.");

			await Import.Start();

			while ((Command = r.ReadByte()) != 0)
			{
				switch (Command)
				{
					case 1:
						throw new Exception("Obsolete file.");  // 1 is obsolete (previously XMPP Credentials)

					case 2: // Database
						string CollectionName;
						string ObjectId;
						string TypeName;
						string FieldName;
						bool Ascending;

						await Import.StartDatabase();

						while (!string.IsNullOrEmpty(CollectionName = r.ReadString()))
						{
							await Import.StartCollection(CollectionName);

							byte b;

							while ((b = r.ReadByte()) != 0)
							{
								switch (b)
								{
									case 1:
										await Import.StartIndex();

										while (!string.IsNullOrEmpty(FieldName = r.ReadString()))
										{
											Ascending = r.ReadBoolean();
											await Import.ReportIndexField(FieldName, Ascending);
										}

										await Import.EndIndex();
										break;

									case 2:
										ObjectId = r.ReadString();
										TypeName = r.ReadString();

										await Import.StartObject(ObjectId, TypeName);

										byte PropertyType = r.ReadByte();
										string PropertyName = r.ReadString();
										object PropertyValue;

										while (!string.IsNullOrEmpty(PropertyName))
										{
											PropertyValue = ReadValue(r, PropertyType);

											await Import.ReportProperty(PropertyName, PropertyValue);

											PropertyType = r.ReadByte();
											PropertyName = r.ReadString();
										}

										await Import.EndObject();
										break;

									default:
										throw new Exception("Unsupported collection section: " + b.ToString());
								}
							}

							await Import.EndCollection();
						}

						await Import.EndDatabase();
						break;

					case 3: // Files
						string FileName;
						int MaxLen = 256 * 1024;
						byte[] Buffer = new byte[MaxLen];

						await Import.StartFiles();

						while (!string.IsNullOrEmpty(FileName = r.ReadString()))
						{
							long Length = r.ReadInt64();

							if (Path.IsPathRooted(FileName))
								throw new Exception("Absolute path names not allowed: " + FileName);

							using TemporaryFile File = new();

							while (Length > 0)
							{
								int Nr = r.Read(Buffer, 0, (int)Math.Min(Length, MaxLen));
								Length -= Nr;
								File.Write(Buffer, 0, Nr);
							}

							File.Position = 0;
							try
							{
								await Import.ExportFile(FileName, File);
							}
							catch (Exception ex)
							{
								ConsoleOut.WriteLine("Unable to extract " + FileName + ": " + ex.Message);
							}
						}

						await Import.EndFiles();
						break;

					case 4:
						throw new Exception("Export file contains reported errors.");

					case 5:
						throw new Exception("Export file contains reported exceptions.");

					case 6: // Ledger

						await Import.StartLedger();

						while (!string.IsNullOrEmpty(CollectionName = r.ReadString()))
						{
							await Import.StartCollection(CollectionName);

							byte b;

							while ((b = r.ReadByte()) != 0)
							{
								switch (b)
								{
									case 1:
										string BlockID = r.ReadString();
										await Import.StartBlock(BlockID);
										break;

									case 2:
										ObjectId = r.ReadString();
										TypeName = r.ReadString();
										EntryType EntryType = (EntryType)r.ReadByte();
										DateTimeKind Kind = (DateTimeKind)r.ReadByte();
										long Ticks = r.ReadInt64();
										DateTime DT = new(Ticks, Kind);
										Ticks = r.ReadInt64();
										Ticks -= Ticks % 600000000; // Offsets must be in whole minutes.
										TimeSpan TS = new(Ticks);
										DateTimeOffset EntryTimestamp = new(DT, TS);

										await Import.StartEntry(ObjectId, TypeName, EntryType, EntryTimestamp);

										byte PropertyType = r.ReadByte();
										string PropertyName = r.ReadString();
										object PropertyValue;

										while (!string.IsNullOrEmpty(PropertyName))
										{
											PropertyValue = ReadValue(r, PropertyType);
											await Import.ReportProperty(PropertyName, PropertyValue);

											PropertyType = r.ReadByte();
											PropertyName = r.ReadString();
										}

										await Import.EndObject();
										break;

									case 3:
										await Import.EndBlock();
										break;

									case 4:
										PropertyName = r.ReadString();
										PropertyType = r.ReadByte();
										PropertyValue = ReadValue(r, PropertyType);

										await Import.BlockMetaData(PropertyName, PropertyValue);
										break;

									default:
										throw new Exception("Unsupported collection section: " + b.ToString());
								}
							}

							await Import.EndCollection();
						}

						await Import.EndLedger();
						break;

					default:
						throw new Exception("Unsupported section: " + Command.ToString());
				}
			}

			await Import.End();
		}

		private static object ReadValue(BinaryReader r, byte PropertyType)
		{
			switch (PropertyType)
			{
				case TYPE_BOOLEAN: return r.ReadBoolean();
				case TYPE_BYTE: return r.ReadByte();
				case TYPE_INT16: return r.ReadInt16();
				case TYPE_INT32: return r.ReadInt32();
				case TYPE_INT64: return r.ReadInt64();
				case TYPE_SBYTE: return r.ReadSByte();
				case TYPE_UINT16: return r.ReadUInt16();
				case TYPE_UINT32: return r.ReadUInt32();
				case TYPE_UINT64: return r.ReadUInt64();
				case TYPE_DECIMAL: return r.ReadDecimal();
				case TYPE_DOUBLE: return r.ReadDouble();
				case TYPE_SINGLE: return r.ReadSingle();
				case TYPE_CHAR: return r.ReadChar();
				case TYPE_STRING: return r.ReadString();
				case TYPE_ENUM: return r.ReadString();
				case TYPE_NULL: return null;

				case TYPE_DATETIME:
					DateTimeKind Kind = (DateTimeKind)((int)r.ReadByte());
					long Ticks = r.ReadInt64();
					return new DateTime(Ticks, Kind);

				case TYPE_TIMESPAN:
					Ticks = r.ReadInt64();
					return new TimeSpan(Ticks);

				case TYPE_BYTEARRAY:
					int Count = r.ReadInt32();
					return r.ReadBytes(Count);

				case TYPE_GUID:
					byte[] Bin = r.ReadBytes(16);
					return new Guid(Bin);

				case TYPE_DATETIMEOFFSET:
					Kind = (DateTimeKind)((int)r.ReadByte());
					Ticks = r.ReadInt64();
					DateTime DT = new(Ticks, Kind);
					Ticks = r.ReadInt64();
					Ticks -= Ticks % 600000000; // Offsets must be in whole minutes.
					TimeSpan TS = new(Ticks);
					return new DateTimeOffset(DT, TS);

				case TYPE_CI_STRING:
					return new CaseInsensitiveString(r.ReadString());

				case TYPE_ARRAY:
					r.ReadString(); // Type name
					long NrElements = r.ReadInt64();

					List<object> List = new();

					while (NrElements > 0)
					{
						NrElements--;
						PropertyType = r.ReadByte();
						List.Add(ReadValue(r, PropertyType));
					}

					return List.ToArray();

				case TYPE_OBJECT:
					string TypeName = r.ReadString();
					GenericObject Object = new(string.Empty, TypeName, Guid.Empty);

					PropertyType = r.ReadByte();
					string PropertyName = r.ReadString();

					while (!string.IsNullOrEmpty(PropertyName))
					{
						Object[PropertyName] = ReadValue(r, PropertyType);

						PropertyType = r.ReadByte();
						PropertyName = r.ReadString();
					}

					return Object;

				default:
					throw new Exception("Unsupported property type: " + PropertyType.ToString());
			}
		}

		private static async Task RestoreCompressed(Stream BackupFile, Extractor Import)
		{
			using GZipStream gz = new(BackupFile, CompressionMode.Decompress, true);
			
			await RestoreBinary(gz, Import);
		}

		private static async Task<(ICryptoTransform, CryptoStream)> RestoreEncrypted(Stream BackupFile, Stream KeyFile, Extractor Import)
		{
			XmlDocument Doc = new()
			{
				PreserveWhitespace = true
			};

			try
			{
				Doc.Load(KeyFile);
			}
			catch (Exception)
			{
				throw new Exception("Invalid key file.");
			}

			XmlElement KeyAes256 = Doc.DocumentElement;
			if (KeyAes256.LocalName != "KeyAes256" ||
				KeyAes256.NamespaceURI != XmlFileLedger.Namespace ||
				!KeyAes256.HasAttribute("key") ||
				!KeyAes256.HasAttribute("iv"))
			{
				throw new Exception("Invalid key file.");
			}

			byte[] Key = Convert.FromBase64String(KeyAes256.Attributes["key"].Value);
			byte[] IV = Convert.FromBase64String(KeyAes256.Attributes["iv"].Value);

			ICryptoTransform AesTransform = Import.CreateDecryptor(Key, IV);
			CryptoStream cs = new(BackupFile, AesTransform, CryptoStreamMode.Read);

			await RestoreCompressed(cs, Import);

			return (AesTransform, cs);
		}

	}
}
