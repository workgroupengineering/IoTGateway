﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// NEXTID element
	/// </summary>
    public class NextId : HtmlElement
    {
		/// <summary>
		/// NEXTID element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public NextId(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "NEXTID")
		{
		}
    }
}
