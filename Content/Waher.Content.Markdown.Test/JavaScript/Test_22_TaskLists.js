﻿function CreateInnerHTML(ElementId, Args)
{
	var Element = document.getElementById(ElementId);
	if (Element)
		Element.innerHTML = CreateHTML(Args);
}

function CreateHTML(Args)
{
	var Segments = [
		"<p>",
		"Simple task list:",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item49\" data-position=\"49",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item49\"",
		">",
		"Unchecked",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item63\" data-position=\"63",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item63\"",
		">",
		"Checked",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item75\" data-position=\"75",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item75\"",
		">",
		"Also checked",
		"</label></li>\r\n",
		"</ul>\r\n",
		"<p>",
		"Nested task list:",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item112\" data-position=\"112",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item112\"",
		">",
		"Unchecked",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item126\" data-position=\"126",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item126\"",
		">",
		"Checked ",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item139\" data-position=\"139",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item139\"",
		">",
		"Unchecked subitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item162\" data-position=\"162",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item162\"",
		">",
		"Checked subitem ",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item184\" data-position=\"184",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item184\"",
		">",
		"Unchecked subsubitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item211\" data-position=\"211",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item211\"",
		">",
		"Checked subsubitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item236\" data-position=\"236",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item236\"",
		">",
		"Second checked subsubitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item268\" data-position=\"268",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item268\"",
		">",
		"Second unchecked subsubitem",
		"</label></li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item301\" data-position=\"301",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item301\"",
		">",
		"Second checked subitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item329\" data-position=\"329",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item329\"",
		">",
		"Second unchecked subitem",
		"</label></li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item358\" data-position=\"358",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item358\"",
		">",
		"Also checked",
		"</label></li>\r\n",
		"</ul>\r\n",
		"<p>",
		"Multi-paragraph, multi-level lists:",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item413\" data-position=\"413",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item413\"",
		">",
		"<p>",
		"Unchecked",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 1",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item458\" data-position=\"458",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item458\"",
		">",
		"<p>",
		"Checked",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 2",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item503\" data-position=\"503",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item503\"",
		">",
		"<p>",
		"Unchecked subitem",
		"</p>\r\n",
		"<pre><code class=\"",
		"nohighlight",
		"\">",
		"Some code\r\n",
		"</code></pre>\r\n",
		"<p>",
		"Second paragraph of item 2.1",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item573\" data-position=\"573",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item573\"",
		">",
		"<p>",
		"Checked subitem",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 2.2",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item628\" data-position=\"628",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item628\"",
		">",
		"Unchecked subsubitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item655\" data-position=\"655",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item655\"",
		">",
		"Checked subsubitem",
		"<pre><code class=\"",
		"nohighlight",
		"\">",
		"Some code\r\n",
		"</code></pre>\r\n",
		"<p>",
		"Second paragraph of item 2.2.2",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item731\" data-position=\"731",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item731\"",
		">",
		"Second checked subsubitem",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item763\" data-position=\"763",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item763\"",
		">",
		"Second unchecked subsubitem",
		"</label></li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item796\" data-position=\"796",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item796\"",
		">",
		"<p>",
		"Second checked subitem",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 2.3",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item857\" data-position=\"857",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item857\"",
		">",
		"<p>",
		"Second unchecked subitem",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 2.4",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item919\" data-position=\"919",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item919\"",
		">",
		"<p>",
		"Also checked",
		"</p>\r\n",
		"<p>",
		"Second paragraph of item 3",
		"</p>\r\n",
		"</label>",
		"</li>\r\n",
		"</ul>\r\n",
		"<p>",
		"Nested indentation:",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item987\" data-position=\"987",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item987\"",
		">",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item991\" data-position=\"991",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item991\"",
		">",
		"<p>",
		"Header 1",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1009\" data-position=\"1009",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1009\"",
		">",
		"Header 1.1",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1027\" data-position=\"1027",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1027\"",
		">",
		"Header 1.2",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1045\" data-position=\"1045",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1045\"",
		">",
		"Header 1.3",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1063\" data-position=\"1063",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1063\"",
		">",
		"Header 1.4",
		"</label></li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"</ul>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1083\" data-position=\"1083",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1083\"",
		">",
		"<p>",
		"Header 2",
		"</p>\r\n",
		"<ul class=\"taskList\">\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1101\" data-position=\"1101",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1101\"",
		">",
		"Header 2.1",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1119\" data-position=\"1119",
		"\" type=\"checkbox\"",
		" checked=\"checked\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1119\"",
		">",
		"Header 2.2",
		"</label></li>\r\n",
		"<li class=\"taskListItem\"><input disabled=\"disabled",
		"\" id=\"item1137\" data-position=\"1137",
		"\" type=\"checkbox\"",
		"/><span></span><label class=\"taskListItemLabel\"",
		" for=\"item1137\"",
		">",
		"Header 2.3",
		"</label></li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"</ul>\r\n",
		"</label>",
		"</li>\r\n",
		"</ul>\r\n"];
	return Segments.join("");
}
