﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Runtime.ServiceRegistration
{
	/// <summary>
	/// Registry annotation.
	/// </summary>
	public class Annotation
	{
		private string tag;
		private string value;

		/// <summary>
		/// Registry annotation.
		/// </summary>
		public Annotation()
		{
			this.tag = string.Empty;
			this.value = string.Empty;
		}

		/// <summary>
		/// Registry annotation.
		/// </summary>
		/// <param name="Tag">Tag name</param>
		/// <param name="Value">Value</param>
		public Annotation(string Tag, string Value)
		{
			this.tag = Tag;
			this.value = Value;
		}

		/// <summary>
		/// Tag name
		/// </summary>
		public string Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		/// <summary>
		/// Value
		/// </summary>
		public string Value
		{
			get => this.value;
			set => this.value = value;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.tag + "=" + this.value;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return (obj is Annotation A &&
				this.tag == A.tag &&
				this.value == A.value);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = 0;

			if (!(this.tag is null))
				Result = this.tag.GetHashCode();

			if (!(this.value is null))
				Result ^= Result << 5 ^ this.value.GetHashCode();

			return Result;
		}

		/// <summary>
		/// Merges two arrays of annotations.
		/// </summary>
		/// <param name="Annotations1">First array of annotations.</param>
		/// <param name="Annotations2">Second array of annotations.</param>
		/// <returns>Merged array of annotations.</returns>
		public static Annotation[] Merge(Annotation[] Annotations1, params Annotation[] Annotations2)
		{
			int c, d;

			if (Annotations1 is null || (c = Annotations1.Length) == 0)
				return Annotations2;

			if (Annotations2 is null || (d = Annotations2.Length) == 0)
				return Annotations1;

			Annotation[] Result = new Annotation[c + d];
			Array.Copy(Annotations1, 0, Result, 0, c);
			Array.Copy(Annotations2, 0, Result, c, d);
			
			return Result;
		}

	}
}
