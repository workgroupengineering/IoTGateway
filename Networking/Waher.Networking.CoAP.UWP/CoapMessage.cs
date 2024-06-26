﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Waher.Networking.CoAP.Options;

namespace Waher.Networking.CoAP
{
	/// <summary>
	/// Contains information about a CoAP message.
	/// </summary>
	public class CoapMessage : IHostReference
	{
		private Dictionary<string, string> uriQuery = null;
		private Dictionary<string, string> locationQuery = null;
		private readonly CoapOption[] options;
		private readonly CoapMessageType type;
		private readonly CoapCode code;
		private readonly IPEndPoint from;
		private Uri baseUri = null;
		private byte[] payload;
		private readonly ushort messageId;
		private readonly ulong token;
		private string host = null;
		private string path = null;
		private string subPath = null;
		private string locationPath = null;
		private ushort? contentFormat = null;
		private ulong? accept = null;
		private ushort? port = null;
		private uint maxAge = 60;
		private uint? size1 = null;
		private uint? size2 = null;
		private CoapOptionBlock1 block1 = null;
		private CoapOptionBlock2 block2 = null;
		private uint? observe = null;

		internal CoapMessage(CoapMessageType Type, CoapCode Code, ushort MessageId, ulong Token, CoapOption[] Options, byte[] Payload,
			IPEndPoint From)
		{
			this.type = Type;
			this.code = Code;
			this.messageId = MessageId;
			this.token = Token;
			this.options = Options;
			this.payload = Payload;
			this.from = From;
		}

		/// <summary>
		/// Type of message.
		/// </summary>
		public CoapMessageType Type => this.type;

		/// <summary>
		/// Message code.
		/// </summary>
		public CoapCode Code => this.code;

		/// <summary>
		/// Message ID.
		/// </summary>
		public ushort MessageId => this.messageId;

		/// <summary>
		/// Token
		/// </summary>
		public ulong Token => this.token;

		/// <summary>
		/// Available options.
		/// </summary>
		public CoapOption[] Options => this.options;

		/// <summary>
		/// Payload, if available, or null otherwise.
		/// </summary>
		public byte[] Payload
		{
			get => this.payload;
			internal set => this.payload = value;
		}

		/// <summary>
		/// Base URI, if available, or null otherwise.
		/// </summary>
		public Uri BaseUri
		{
			get => this.baseUri;
			internal set => this.baseUri = value;
		}

		/// <summary>
		/// From where the message came.
		/// </summary>
		public IPEndPoint From => this.from;

		/// <summary>
		/// Optional accept option.
		/// </summary>
		public ulong? Accept
		{
			get => this.accept;
			internal set => this.accept = value;
		}

		/// <summary>
		/// Optional content format option.
		/// </summary>
		public ushort? ContentFormat
		{
			get => this.contentFormat;
			internal set => this.contentFormat = value;
		}

		/// <summary>
		/// Optional URI query parameters.
		/// </summary>
		public Dictionary<string, string> UriQuery
		{
			get => this.uriQuery;
			internal set => this.uriQuery = value;
		}

		/// <summary>
		/// Tries to get a URI query parameter value.
		/// </summary>
		/// <param name="Name">Parameter name.</param>
		/// <param name="Value">Parameter value.</param>
		/// <returns>If a parameter with the gíven name was found.</returns>
		public bool TryGetUriQueryParameter(string Name, out string Value)
		{
			if (this.uriQuery is null)
			{
				Value = null;
				return false;
			}
			else
				return this.uriQuery.TryGetValue(Name, out Value);
		}

		/// <summary>
		/// Optional URI Port number option.
		/// </summary>
		public ushort? Port
		{
			get => this.port;
			internal set => this.port = value;
		}

		/// <summary>
		/// Optional URI Host option.
		/// </summary>
		public string Host
		{
			get => this.host;
			internal set => this.host = value;
		}

		/// <summary>
		/// Optional URI Path options, appended into a path string.
		/// </summary>
		public string Path
		{
			get => this.path;
			set => this.path = value;
		}

		/// <summary>
		/// Part of the <see cref="Path"/> not matched by the corresponding resource
		/// processing the message, in case the resource supports subpaths.
		/// </summary>
		public string SubPath
		{
			get => this.subPath;
			set => this.subPath = value;
		}

		/// <summary>
		/// Max Age option (number of seconds).
		/// </summary>
		public uint MaxAge
		{
			get => this.maxAge;
			internal set => this.maxAge = value;
		}

		/// <summary>
		/// Optional Location Path options, appended into a path string.
		/// </summary>
		public string LocationPath
		{
			get => this.locationPath;
			internal set => this.locationPath = value;
		}

		/// <summary>
		/// Optional Location Query parameters.
		/// </summary>
		public Dictionary<string, string> LocationQuery
		{
			get => this.locationQuery;
			internal set => this.locationQuery = value;
		}

		/// <summary>
		/// Tries to get a Location query parameter value.
		/// </summary>
		/// <param name="Name">Parameter  name.</param>
		/// <param name="Value">Parameter value.</param>
		/// <returns>If a location parameter was found with the given name.</returns>
		public bool TryGetLocationQueryParameter(string Name, out string Value)
		{
			if (this.locationQuery is null)
			{
				Value = null;
				return false;
			}
			else
				return this.locationQuery.TryGetValue(Name, out Value);
		}

		/// <summary>
		/// Optional Size1 option.
		/// </summary>
		public uint? Size1
		{
			get => this.size1;
			internal set => this.size1 = value;
		}

		/// <summary>
		/// Optional Size2 option.
		/// </summary>
		public uint? Size2
		{
			get => this.size2;
			internal set => this.size2 = value;
		}

		/// <summary>
		/// Optional Block1 option (request payload).
		/// </summary>
		public CoapOptionBlock1 Block1
		{
			get => this.block1;
			internal set => this.block1 = value;
		}

		/// <summary>
		/// Optional Block2 option (response payload).
		/// </summary>
		public CoapOptionBlock2 Block2
		{
			get => this.block2;
			internal set => this.block2 = value;
		}

		/// <summary>
		/// Optional Observe option.
		/// </summary>
		public uint? Observe
		{
			get => this.observe;
			internal set => this.observe = value;
		}

		/// <summary>
		/// Generates an URI for the message.
		/// </summary>
		/// <returns>URI string.</returns>
		public string GetUri()
		{
			return CoapEndpoint.GetUri(this.host, this.port, this.path, this.uriQuery);
		}

		/// <summary>
		/// Decodes the payload of the message.
		/// </summary>
		/// <returns>Decoded payload.</returns>
		[Obsolete("Use DecodeAsync instead, for better asynchronous performance.")]
		public object Decode()
		{
			return this.DecodeAsync().Result;
		}

		/// <summary>
		/// Decodes the payload of the message.
		/// </summary>
		/// <returns>Decoded payload.</returns>
		public async Task<object> DecodeAsync()
		{
			if (this.payload is null)
				return null;
			else if (!this.contentFormat.HasValue)
				return this.payload;
			else
				return await CoapEndpoint.DecodeAsync((int)this.contentFormat.Value, this.payload, this.baseUri);
		}

		/// <summary>
		/// Checks if a given content format is acceptable to the client.
		/// </summary>
		/// <param name="ContentFormat">Content Format to check.</param>
		/// <returns>If the content format is acceptable or not.</returns>
		public bool IsAcceptable(int ContentFormat)
		{
			if (this.accept is null)
				return true;

			if (ContentFormat < 0)
				return false;

			foreach (CoapOption Option in this.options)
			{
				if (Option is CoapOptionAccept Accept && Accept.Value == (uint)ContentFormat)
					return true;
			}

			return false;
		}

	}
}
