﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.IoTGateway;
using Waher.IoTGateway.Setup;
using Waher.Networking;
using Waher.Networking.HTTP;
using Waher.Networking.HTTP.Authentication;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;
using Waher.Runtime.Timing;
using Waher.Security;
using Waher.Security.JWT;
using Waher.Security.Users;

namespace Waher.WebService.Tesseract
{
	/// <summary>
	/// Class providing a web API for OCR using Tesseract, installed on the server.
	/// </summary>
	[ModuleDependency("Waher.Service.IoTBroker.XmppServerModule")]
	public class TesseractApi : IConfigurableModule
	{
		private const string FolderPrefix = "Tesseract";
		private const string ExecutableName = "tesseract.exe";

		private static readonly Random rnd = new Random();
		private static Scheduler scheduler = null;
		private static ApiResource apiResource;
		private static bool disposeScheduler = false;
		private static string tesseractExe = null;
		private static string tesseractFolder = null;
		private static string tesseractDataFolder = null;
		private static string imagesFolder = null;
		private static bool hasImagesFolder = false;
		private static bool exeFound = false;

		/// <summary>
		/// Class providing a web API for OCR using Tesseract, installed on the server.
		/// </summary>
		public TesseractApi()
		{
		}

		/// <summary>
		/// Starts the module.
		/// </summary>
		public Task Start()
		{
			try
			{
				string ExeFolder = SearchForInstallationFolder();
				string ImagesFolder = null;

				if (!string.IsNullOrEmpty(Gateway.AppDataFolder))
					ImagesFolder = Path.Combine(Gateway.AppDataFolder, FolderPrefix);

				if (!(Gateway.HttpServer is null))
				{
					List<HttpAuthenticationScheme> Schemes = new List<HttpAuthenticationScheme>();
					bool RequireEncryption;
					int MinSecurityStrength;

					if (DomainConfiguration.Instance.UseEncryption && !string.IsNullOrEmpty(DomainConfiguration.Instance.Domain))
					{
						RequireEncryption = true;
						MinSecurityStrength = 128;
					}
					else
					{
						RequireEncryption = false;
						MinSecurityStrength = 0;
					}

					if (Types.TryGetModuleParameter("JWT", out object Obj) &&
						Obj is JwtFactory JwtFactory &&
						!JwtFactory.Disposed)
					{
						Schemes.Add(new JwtAuthentication(RequireEncryption, MinSecurityStrength, Gateway.Domain, null, JwtFactory));   // Any JWT token generated by the server will suffice. Does not have to point to a registered user.
					}

					if (Gateway.HttpServer.ClientCertificates != ClientCertificates.NotUsed)
						Schemes.Add(new MutualTlsAuthentication(Users.Source));

					Schemes.Add(new BasicAuthentication(RequireEncryption, MinSecurityStrength, Gateway.Domain, Users.Source));
					Schemes.Add(new DigestAuthentication(RequireEncryption, MinSecurityStrength, DigestAlgorithm.MD5, Gateway.Domain, Users.Source));
					Schemes.Add(new DigestAuthentication(RequireEncryption, MinSecurityStrength, DigestAlgorithm.SHA256, Gateway.Domain, Users.Source));
					Schemes.Add(new DigestAuthentication(RequireEncryption, MinSecurityStrength, DigestAlgorithm.SHA3_256, Gateway.Domain, Users.Source));

					apiResource = new ApiResource(this, Schemes.ToArray());

					Gateway.HttpServer.Register(apiResource);
				}

				if (scheduler is null)
				{
					if (Types.TryGetModuleParameter("Scheduler", out object Obj) && Obj is Scheduler Scheduler)
					{
						scheduler = Scheduler;
						disposeScheduler = false;
					}
					else
					{
						scheduler = new Scheduler();
						disposeScheduler = true;
					}
				}

				if (string.IsNullOrEmpty(ExeFolder))
					Log.Warning("Tesseract not found. Tesseract support will not be available via the Tesseract API.");
				else
				{
					SetInstallationPaths(ExeFolder, ImagesFolder);

					Log.Informational("Tesseract found. Tesseract API added.",
						new KeyValuePair<string, object>("Folder", ExeFolder));
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the module.
		/// </summary>
		public Task Stop()
		{
			if (disposeScheduler)
			{
				scheduler?.Dispose();
				scheduler = null;
				disposeScheduler = false;
			}

			if (!(apiResource is null))
			{
				Gateway.HttpServer.Unregister(apiResource);
				apiResource = null;
			}

			exeFound = false;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets an array of configurable pages for the module.
		/// </summary>
		/// <returns>Configurable pages</returns>
		public Task<IConfigurablePage[]> GetConfigurablePages()
		{
			return Task.FromResult(new IConfigurablePage[]
			{
				new ConfigurablePage("Tesseract OCR", "/Tesseract/Api.md")
			});
		}

		/// <summary>
		/// Path of executable file.
		/// </summary>
		public string ExecutablePath => tesseractExe;

		/// <summary>
		/// Path to folder with images.
		/// </summary>
		public string ImagesPath = imagesFolder;

		/// <summary>
		/// If the Tesseract executable application was found.
		/// </summary>
		public bool ExeFound => exeFound;

		/// <summary>
		/// Sets the installation folder of Tesseract.
		/// </summary>
		/// <param name="ExePath">Path to executable file.</param>
		/// <param name="ImagesFolder">Optional path to folder hosting images.</param>
		/// <exception cref="Exception">If trying to set the installation folder to a different folder than the one set previously.
		/// The folder can only be set once, for security reasons.</exception>
		public static void SetInstallationPaths(string ExePath, string ImagesFolder)
		{
			if (!string.IsNullOrEmpty(tesseractExe) && ExePath != tesseractExe)
				throw new Exception("Tesseract executable path has already been set.");

			tesseractExe = ExePath;
			tesseractFolder = Path.GetDirectoryName(ExePath);
			tesseractDataFolder = Path.Combine(tesseractFolder, "tessdata");
			imagesFolder = ImagesFolder;
			hasImagesFolder = !string.IsNullOrEmpty(imagesFolder);
			exeFound = true;

			if (hasImagesFolder)
			{
				if (!Directory.Exists(imagesFolder))
					Directory.CreateDirectory(imagesFolder);

				DeleteOldFiles(TimeSpan.FromDays(7));
			}
		}

		private static void DeleteOldFiles(object P)
		{
			if (P is TimeSpan MaxAge)
				DeleteOldFiles(MaxAge, true);
		}

		/// <summary>
		/// Deletes generated files older than <paramref name="MaxAge"/>.
		/// </summary>
		/// <param name="MaxAge">Age limit.</param>
		/// <param name="Reschedule">If rescheduling should be done.</param>
		public static void DeleteOldFiles(TimeSpan MaxAge, bool Reschedule)
		{
			DateTime Limit = DateTime.Now - MaxAge;
			int Count = 0;

			DirectoryInfo ImagesFolder = new DirectoryInfo(imagesFolder);
			FileInfo[] Files = ImagesFolder.GetFiles("*.*");

			foreach (FileInfo FileInfo in Files)
			{
				if (FileInfo.LastAccessTime < Limit)
				{
					try
					{
						File.Delete(FileInfo.FullName);
						Count++;
					}
					catch (Exception ex)
					{
						Log.Error("Unable to delete old file: " + ex.Message, FileInfo.FullName);
					}
				}
			}

			if (Count > 0)
				Log.Informational(Count.ToString() + " old file(s) deleted.", imagesFolder);

			if (Reschedule)
			{
				lock (rnd)
				{
					scheduler.Add(DateTime.Now.AddDays(rnd.NextDouble() * 2), DeleteOldFiles, MaxAge);
				}
			}
		}

		/// <summary>
		/// Searches for the installation folder on the local machine.
		/// </summary>
		/// <returns>Installation folder, if found, null otherwise.</returns>
		public static string SearchForInstallationFolder()
		{
			string InstallationFolder;

			InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.ProgramFilesX86);
			if (string.IsNullOrEmpty(InstallationFolder))
			{
				InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.ProgramFiles);
				if (string.IsNullOrEmpty(InstallationFolder))
				{
					InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.Programs);
					if (string.IsNullOrEmpty(InstallationFolder))
					{
						InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonProgramFilesX86);
						if (string.IsNullOrEmpty(InstallationFolder))
						{
							InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonProgramFiles);
							if (string.IsNullOrEmpty(InstallationFolder))
								InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonPrograms);
						}
					}
				}
			}

			return InstallationFolder;
		}

		private static string SearchForInstallationFolder(Environment.SpecialFolder SpecialFolder)
		{
			string Folder;

			try
			{
				Folder = Environment.GetFolderPath(SpecialFolder);
			}
			catch (Exception)
			{
				return null; // Folder not defined for the operating system.
			}

			string Result = SearchForInstallationFolder(Folder);

			if (string.IsNullOrEmpty(Result) && !string.IsNullOrEmpty(Gateway.RuntimeFolder))
				Result = SearchForInstallationFolder(Path.Combine(Gateway.RuntimeFolder, SpecialFolder.ToString()));

			return Result;
		}

		private static string SearchForInstallationFolder(string Folder)
		{
			if (string.IsNullOrEmpty(Folder))
				return null;

			if (!Directory.Exists(Folder))
				return null;

			string FolderName;
			string[] SubFolders;

			try
			{
				SubFolders = Directory.GetDirectories(Folder);
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return null;
			}

			foreach (string SubFolder in SubFolders)
			{
				try
				{
					FolderName = Path.GetFileName(SubFolder);
					if (!FolderName.StartsWith(FolderPrefix, StringComparison.CurrentCultureIgnoreCase))
						continue;

					string ExePath = Path.Combine(SubFolder, ExecutableName);
					if (File.Exists(ExePath))
						return ExePath;
				}
				catch (Exception)
				{
					continue;	// Ignore. Some folders might be protected.
				}
			}

			return null;
		}

		/// <summary>
		/// Performs OCR on an image.
		/// </summary>
		/// <param name="Image">Binary representation of image.</param>
		/// <param name="ContentType">Content-Type of image representation.</param>
		/// <param name="PageSegmentationMode">Optional Page segmentation mode.</param>
		/// <param name="Language">Optional language.</param>
		/// <returns>Decoded text.</returns>
		public Task<string> PerformOcr(byte[] Image, string ContentType, string PageSegmentationMode, string Language)
		{
			if (Enum.TryParse(PageSegmentationMode, out PageSegmentationMode ParsedPageSegmentationMode))
				return this.PerformOcr(Image, ContentType, ParsedPageSegmentationMode, Language);
			else
				return this.PerformOcr(Image, ContentType, (PageSegmentationMode?)null, Language);
		}

		/// <summary>
		/// Performs OCR on an image.
		/// </summary>
		/// <param name="Image">Binary representation of image.</param>
		/// <param name="ContentType">Content-Type of image representation.</param>
		/// <param name="PageSegmentationMode">Optional Page segmentation mode.</param>
		/// <param name="Language">Optional language.</param>
		/// <returns>Decoded text.</returns>
		public async Task<string> PerformOcr(byte[] Image, string ContentType, PageSegmentationMode? PageSegmentationMode,
			string Language)
		{
			string Extension = InternetContent.GetFileExtension(ContentType);
			TemporaryFile TempFile = null;
			string ResultFileName = null;
			string FileName;

			try
			{
				if (hasImagesFolder)
				{
					string Hash = Hashes.ComputeSHA256HashString(Image);
					FileName = Path.Combine(imagesFolder, Path.ChangeExtension(Hash, Extension));
				}
				else
				{
					TempFile = new TemporaryFile(Path.ChangeExtension(Path.GetTempFileName(), Extension));
					await TempFile.WriteAsync(Image, 0, Image.Length);

					FileName = TempFile.FileName;
				}

				ResultFileName = FileName + ".txt";

				if (!(imagesFolder is null) && File.Exists(ResultFileName))
					return await Resources.ReadAllTextAsync(ResultFileName);
				else
				{
					if (!string.IsNullOrEmpty(imagesFolder))
						await Resources.WriteAllBytesAsync(FileName, Image);

					StringBuilder Arguments = new StringBuilder();

					Arguments.Append('"');
					Arguments.Append(FileName);
					Arguments.Append("\" \"");
					Arguments.Append(FileName);
					Arguments.Append('"');

					if (!string.IsNullOrEmpty(Language))
					{
						Arguments.Append(" -l ");
						Arguments.Append(Language);
					}

					if (PageSegmentationMode.HasValue)
					{
						Arguments.Append(" --psm ");
						Arguments.Append(((int)PageSegmentationMode.Value).ToString());
					}

					if (!string.IsNullOrEmpty(tesseractDataFolder))
					{
						Arguments.Append(" --tessdata-dir \"");
						Arguments.Append(tesseractDataFolder);
						Arguments.Append('"');
					}

					ProcessStartInfo ProcessInformation = new ProcessStartInfo()
					{
						FileName = tesseractExe,
						Arguments = Arguments.ToString(),
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = true,
						WorkingDirectory = tesseractFolder,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					Process P = new Process();
					TaskCompletionSource<string> ResultSource = new TaskCompletionSource<string>();

					P.ErrorDataReceived += (Sender, e) =>
					{
						Log.Error("Unable to perform OCR: " + e.Data);
						ResultSource.TrySetResult(null);
					};

					P.Exited += async (Sender, e) =>
					{
						try
						{
							if (P.ExitCode != 0)
							{
								string ErrorText = await P.StandardError.ReadToEndAsync();
								Log.Error("Unable to perform OCR. Exit code: " + P.ExitCode.ToString() + "\r\n\r\n" + ErrorText);
								ResultSource.TrySetResult(null);
							}
							else
							{
								string Result = await Resources.ReadAllTextAsync(ResultFileName);
								ResultSource.TrySetResult(Result);
							}
						}
						catch (Exception ex)
						{
							Log.Exception(ex);
						}
					};

					Task _ = Task.Delay(10000).ContinueWith(Prev => ResultSource.TrySetException(new TimeoutException("Tesseract process did not terminate properly.")));

					P.StartInfo = ProcessInformation;
					P.EnableRaisingEvents = true;
					P.Start();

					return await ResultSource.Task;
				}
			}
			finally
			{
				if (!(TempFile is null))
				{
					TempFile.Dispose();

					if (!string.IsNullOrEmpty(ResultFileName))
						File.Delete(ResultFileName);
				}
			}
		}
	}
}
