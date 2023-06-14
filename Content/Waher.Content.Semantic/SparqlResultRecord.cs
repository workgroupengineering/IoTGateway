﻿using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Waher.Content.Semantic
{
	/// <summary>
	/// Contains a record from the results of a SPARQL query.
	/// </summary>
	public class SparqlResultRecord : IEnumerable<SparqlResultItem>
	{
		private readonly Dictionary<string, SparqlResultItem> items;

		/// <summary>
		/// Contains a record from the results of a SPARQL query.
		/// </summary>
		/// <param name="Items">Items in record.</param>
		/// <param name="VariableOrder">Variable order.</param>
		public SparqlResultRecord(Dictionary<string, SparqlResultItem> Items)
		{
			this.items = Items;
		}

		/// <summary>
		/// Gets the value of a variable in the record. If the variable is not found,
		/// null is returned.
		/// </summary>
		/// <param name="VariableName">Name of variable.</param>
		/// <returns>Result, if found, null if not found.</returns>
		public ISemanticElement this[string VariableName]
		{
			get
			{
				if (this.items.TryGetValue(VariableName, out SparqlResultItem Result))
					return Result.Value;
				else
					return null;
			}
		}

		/// <summary>
		/// Gets enumerator over items in record.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		public IEnumerator<SparqlResultItem> GetEnumerator()
		{
			return this.items.Values.GetEnumerator();
		}

		/// <summary>
		/// Gets enumerator over items in record.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.items.Values.GetEnumerator();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool First = true;

			foreach (SparqlResultItem Item in this.items.Values)
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Item.ToString());
			}

			return sb.ToString();
		}
	}
}