﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// U element
	/// </summary>
    public class U : HtmlElement
    {
		/// <summary>
		/// U element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public U(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "U")
		{
		}
    }
}
