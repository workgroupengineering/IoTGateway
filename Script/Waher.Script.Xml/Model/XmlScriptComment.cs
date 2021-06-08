﻿using System;
using System.Xml;

namespace Waher.Script.Xml.Model
{
	/// <summary>
	/// XML Script Comment node.
	/// </summary>
	public class XmlScriptComment : XmlScriptLeafNode
	{
		private readonly string text;

		/// <summary>
		/// XML Script Comment node.
		/// </summary>
		/// <param name="Text">Text content.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public XmlScriptComment(string Text, int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.text = Text;
		}

		/// <summary>
		/// Builds an XML Document objcet
		/// </summary>
		/// <param name="Document">Document being built.</param>
		/// <param name="Parent">Parent element.</param>
		/// <param name="Variables">Current set of variables.</param>
		internal override void Build(XmlDocument Document, XmlElement Parent, Variables Variables)
		{
			Parent.AppendChild(Document.CreateComment(this.text));
		}
	}
}
