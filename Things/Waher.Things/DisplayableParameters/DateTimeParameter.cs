﻿using System;
using System.Text;
using Waher.Content.Xml;

namespace Waher.Things.DisplayableParameters
{
	/// <summary>
	/// DateTime-valued parameter.
	/// </summary>
	public class DateTimeParameter : Parameter
	{
		private DateTime value;

		/// <summary>
		/// DateTime-valued parameter.
		/// </summary>
		public DateTimeParameter()
			: base()
		{
			this.value = DateTime.MinValue;
		}

		/// <summary>
		/// DateTime-valued parameter.
		/// </summary>
		/// <param name="Id">Parameter ID.</param>
		/// <param name="Name">Parameter Name.</param>
		/// <param name="Value">Parameter Value</param>
		public DateTimeParameter(string Id, string Name, DateTime Value)
			: base(Id, Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Parameter Value.
		/// </summary>
		public DateTime Value
		{
			get => this.value;
			set => this.value = value;
		}

		/// <summary>
		/// Untyped parameter value
		/// </summary>
		public override object UntypedValue => this.value;

		/// <summary>
		/// Exports the parameters to XML.
		/// </summary>
		/// <param name="Xml">XML Output.</param>
		public override void Export(StringBuilder Xml)
		{
			Xml.Append("<dateTime");
			base.Export(Xml);
			Xml.Append(" value='");
			Xml.Append(XML.Encode(this.value));
			Xml.Append("'/>");
		}
	}
}
