﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// ARTICLE element
	/// </summary>
    public class Article : HtmlElement
    {
		/// <summary>
		/// ARTICLE element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public Article(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "ARTICLE")
		{
		}
    }
}
