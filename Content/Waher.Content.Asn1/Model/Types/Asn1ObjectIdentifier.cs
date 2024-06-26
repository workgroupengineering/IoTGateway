﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content.Asn1.Model.Macro;
using Waher.Content.Asn1.Model.Values;

namespace Waher.Content.Asn1.Model.Types
{
	/// <summary>
	/// OBJECT IDENTIFIER, or RELATIVE-OID
	/// </summary>
	public class Asn1ObjectIdentifier : Asn1Type
	{
		private readonly bool relative;

		/// <summary>
		/// OBJECT IDENTIFIER, or RELATIVE-OID
		/// </summary>
		/// <param name="Relative">If it is a relative object ID.</param>
		public Asn1ObjectIdentifier(bool Relative)
			: base()
		{
			this.relative = Relative;
		}

		/// <summary>
		/// If it is a relative object ID.
		/// </summary>
		public bool Relative => this.relative;

		/// <summary>
		/// Parses the portion of the document at the current position, according to the type.
		/// </summary>
		/// <param name="Document">ASN.1 document being parsed.</param>
		/// <param name="Macro">Macro performing parsing.</param>
		/// <returns>Parsed ASN.1 node.</returns>
		public override Asn1Node Parse(Asn1Document Document, Asn1Macro Macro)
		{
			if (Document.ParseValue() is Values.Asn1Oid Value)
				return Value;
			else
				throw Document.SyntaxError("String value expected.");
		}

		/// <summary>
		/// Exports to C#
		/// </summary>
		/// <param name="Output">C# Output.</param>
		/// <param name="State">C# export state.</param>
		/// <param name="Indent">Indentation</param>
		/// <param name="Pass">Export pass</param>
		public override Task ExportCSharp(StringBuilder Output, CSharpExportState State, int Indent, CSharpExportPass Pass)
		{
			if (Pass == CSharpExportPass.Explicit)
				Output.Append("ObjectId");
		
			return Task.CompletedTask;
		}
	}
}
