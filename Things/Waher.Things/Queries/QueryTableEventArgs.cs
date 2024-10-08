﻿namespace Waher.Things.Queries
{
	/// <summary>
	/// Base class for all query-related table events.
	/// </summary>
	public class QueryTableEventArgs : QueryEventArgs
	{
		private readonly string tableId;

		/// <summary>
		/// Base class for all query-related table events.
		/// </summary>
		/// <param name="Query">Query.</param>
		/// <param name="TableId">Table ID.</param>
		public QueryTableEventArgs(Query Query, string TableId)
			: base(Query)
		{
			this.tableId = TableId;
		}

		/// <summary>
		/// Table ID.
		/// </summary>
		public string TableId => this.tableId;
	}
}
