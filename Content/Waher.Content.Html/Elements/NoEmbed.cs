﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// NOEMBED element
	/// </summary>
    public class NoEmbed : HtmlElement
    {
		/// <summary>
		/// NOEMBED element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public NoEmbed(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "NOEMBED")
		{
		}
    }
}
