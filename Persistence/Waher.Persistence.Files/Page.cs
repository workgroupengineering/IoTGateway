﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Waher.Persistence.Filters;
using Waher.Persistence.Serialization;

namespace Waher.Persistence.Files
{
	/// <summary>
	/// Contains a page of items.
	/// </summary>
	/// <typeparam name="T">Type of objects on the page.</typeparam>
	public class Page<T> : IPage<T>
		where T : class
	{
		private readonly int pageSize;
		private readonly string collection;
		private readonly Filter filter;
		private readonly string[] sortOrder;
		private readonly string objectIdName;
		private readonly IEnumerable<T> items;
		private readonly ObjectSerializer serializer;
		private readonly FilesProvider provider;
		private readonly T lastItem;
		private readonly bool hasLastItem;

		/// <summary>
		/// Contains a page of items.
		/// </summary>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="Collection">Optional collection name.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <param name="Items">Items on page.</param>
		/// <param name="Serializer">Object serializer.</param>
		/// <param name="Provider">Current database provider.</param>
		public Page(int PageSize, string Collection, Filter Filter, string[] SortOrder,
			IEnumerable<T> Items, ObjectSerializer Serializer, FilesProvider Provider)
		{
			this.pageSize = PageSize;
			this.collection = Collection;
			this.filter = Filter;
			this.sortOrder = SortOrder;
			this.items = Items;
			this.serializer = Serializer;
			this.provider = Provider;
			this.objectIdName = this.serializer.ObjectIdMemberName;

			if (string.IsNullOrEmpty(this.objectIdName))
				throw new IOException("Paginated objects must implement Object IDs.");

			this.hasLastItem = false;

			foreach (T Item in Items)
			{
				this.lastItem = Item;
				this.hasLastItem = false;
			}
		}

		/// <summary>
		/// Items available in the page. The enumeration may be empty.
		/// </summary>
		public IEnumerable<T> Items => this.items;

		/// <summary>
		/// If there may be more pages following this page.
		/// </summary>
		public bool More => this.hasLastItem;

		/// <summary>
		/// Finds the next page of objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <returns>Next page, directly following the current page.</returns>
		public async Task<IPage<T>> FindNext()
		{
			if (!this.hasLastItem)
				return new EmptyPage<T>();

			Guid LastObjectId = await this.serializer.GetObjectId(this.lastItem, false, null);
			if (LastObjectId == Guid.Empty)
				return new EmptyPage<T>();

			Filter NextPageFilter;

			if (this.filter is FilterAnd FilterAnd)
			{
				int c = FilterAnd.ChildFilters.Length;
				Filter[] NewFilters = new Filter[c + 1];

				Array.Copy(FilterAnd.ChildFilters, 0, NewFilters, 0, c);
				NewFilters[c] = new FilterFieldGreaterThan(this.objectIdName, LastObjectId);

				NextPageFilter = new FilterAnd(NewFilters);
			}
			else if (this.filter is null)
				NextPageFilter = new FilterFieldGreaterThan(this.objectIdName, LastObjectId);
			else
			{
				NextPageFilter = new FilterAnd(
					this.filter,
					new FilterFieldGreaterThan(this.objectIdName, LastObjectId));
			}

			IEnumerable<T> NewItems;

			if (string.IsNullOrEmpty(this.collection))
				NewItems = await this.provider.Find<T>(0, this.pageSize, NextPageFilter, this.sortOrder);
			else
				NewItems = await this.provider.Find<T>(this.collection, 0, this.pageSize, NextPageFilter, this.sortOrder);

			return new Page<T>(this.pageSize, this.collection, this.filter, this.sortOrder,
				NewItems, this.serializer, this.provider);
		}
	}
}
