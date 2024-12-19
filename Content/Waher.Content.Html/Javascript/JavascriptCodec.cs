﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace Waher.Content.Html.JavaScript
{
	/// <summary>
	/// JavaScript encoder/decoder.
	/// </summary>
	public class JavaScriptCodec : IContentDecoder, IContentEncoder
	{
		/// <summary>
		/// JavaScript encoder/decoder.
		/// </summary>
		public JavaScriptCodec()
		{
		}

		/// <summary>
		/// application/javascript
		/// </summary>
		public const string DefaultContentType = "application/javascript";

		/// <summary>
		/// JavaScript content types.
		/// </summary>
		public static readonly string[] JavaScriptContentTypes = new string[]
		{
			DefaultContentType
		};

		/// <summary>
		/// Plain text file extensions.
		/// </summary>
		public static readonly string[] JavaScriptFileExtensions = new string[]
		{
			"js"
		};

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes => JavaScriptContentTypes;

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => JavaScriptFileExtensions;

		/// <summary>
		/// If the decoder decodes an object with a given content type.
		/// </summary>
		/// <param name="ContentType">Content type to decode.</param>
		/// <param name="Grade">How well the decoder decodes the object.</param>
		/// <returns>If the decoder can decode an object with the given type.</returns>
		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (Array.IndexOf(JavaScriptContentTypes, ContentType) >= 0)
			{
				Grade = Grade.Barely;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Decodes an object.
		/// </summary>
		/// <param name="ContentType">Internet Content Type.</param>
		/// <param name="Data">Encoded object.</param>
		/// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
		/// <param name="Fields">Any content-type related fields and their corresponding values.</param>
		///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
		/// <returns>Decoded object.</returns>
		public Task<ContentResponse> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding, KeyValuePair<string, string>[] Fields, Uri BaseUri)
		{
			string JavaScript = CommonTypes.GetString(Data, Encoding);
			return Task.FromResult(new ContentResponse(ContentType, new JavaScriptDocument(JavaScript), Data));
		}

		/// <summary>
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			switch (FileExtension.ToLower())
			{
				case "js":
					ContentType = DefaultContentType;
					return true;

				default:
					ContentType = string.Empty;
					return false;
			}
		}

		/// <summary>
		/// Tries to get the file extension of an item, given its Content-Type.
		/// </summary>
		/// <param name="ContentType">Content type.</param>
		/// <param name="FileExtension">File extension.</param>
		/// <returns>If the Content-Type was recognized.</returns>
		public bool TryGetFileExtension(string ContentType, out string FileExtension)
		{
			switch (ContentType.ToLower())
			{
				case DefaultContentType:
					FileExtension = JavaScriptFileExtensions[0];
					return true;

				default:
					FileExtension = string.Empty;
					return false;
			}
		}

		/// <summary>
		/// If the encoder encodes a given object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Grade">How well the encoder encodes the object.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>If the encoder can encode the given object.</returns>
		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if (Object is JavaScriptDocument)
			{
				if (InternetContent.IsAccepted(JavaScriptContentTypes, AcceptedContentTypes))
				{
					Grade = Grade.Barely;
					return true;
				}
			}

			Grade = Grade.NotAtAll;
			return false;
		}

		/// <summary>
		/// Encodes an object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Encoding">Desired encoding of text. Can be null if no desired encoding is speified.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>Encoded object, as well as Content Type of encoding. Includes information about any text encodings used.</returns>
		public Task<ContentResponse> EncodeAsync(object Object, Encoding Encoding, params string[] AcceptedContentTypes)
		{
			if (!InternetContent.IsAccepted(JavaScriptContentTypes, out string ContentType, AcceptedContentTypes))
				throw new ArgumentException("Unable to encode object, or content type not accepted.", nameof(Object));

			string s;

			if (Object is JavaScriptDocument JavaScriptDoc)
				s = JavaScriptDoc.JavaScript;
			else
				s = Object.ToString();

			if (Encoding is null)
			{
				ContentType += "; charset=utf-8";
				Encoding = Encoding.UTF8;
			}
			else
				ContentType += "; charset=" + Encoding.WebName;

			return Task.FromResult(new ContentResponse(ContentType, Object, Encoding.GetBytes(s)));
		}
	}
}
