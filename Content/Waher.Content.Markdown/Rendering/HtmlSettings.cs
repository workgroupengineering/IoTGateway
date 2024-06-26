﻿namespace Waher.Content.Markdown.Rendering
{
    /// <summary>
    /// Contains settings that the HTML export uses to customize HTML output.
    /// </summary>
    public class HtmlSettings
    {
        private string hashtagClass = string.Empty;
        private string hashtagClickScript = string.Empty;
        private bool xmlEntitiesOnly = false;

        /// <summary>
        /// Contains settings that the HTML export uses to customize HTML output.
        /// </summary>
        public HtmlSettings()
        {
        }

        /// <summary>
        /// Class name used on hashtag mark elements.
        /// </summary>
        public string HashtagClass
        {
            get => this.hashtagClass;
            set => this.hashtagClass = value;
        }

		/// <summary>
		/// JavaScript to execute when hashtag mark element is clicked.
		/// </summary>
		public string HashtagClickScript
        {
            get => this.hashtagClickScript;
            set => this.hashtagClickScript = value;
        }

        /// <summary>
        /// If typographical extensions should conform to XML entities (true), or if HTML entities can be used as well (false).
        /// </summary>
        public bool XmlEntitiesOnly
        {
            get => this.xmlEntitiesOnly;
            set => this.xmlEntitiesOnly = value;
        }
    }
}
