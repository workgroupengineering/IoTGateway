﻿function CreateInnerHTML(ElementId, Args)
{
	var Element = document.getElementById(ElementId);
	if (Element)
		Element.innerHTML = CreateHTML(Args);
}

function CreateHTML(Args)
{
	var Segments = [
		"<h1",
		" id=\"requestForTendersTransportServiceExample\"",
		">",
		"Request for Tenders - Transport Service (Example)",
		"</h1>\r\n",
		"<p>",
		"This contract defines an auction consignment of the service of buying a transport service. The assignment of a seller to this consignment is negotiated by an online auction managed by ",
		"<em>",
		"TAG Marketplace",
		"</em>",
		"™. The document defines the rules for the automatic processing and matching of prospective sellers to this request for tenders.",
		"</p>\r\n",
		"<h2",
		" id=\"introduction\"",
		">",
		"Introduction",
		"</h2>\r\n",
		"<p>",
		"This contract defines the request to buy the service of transportation of item(s) using the ",
		"<em>",
		"TAG Marketplace",
		"</em>",
		"™ auctioning service. The ",
		"<strong>",
		"Buyer",
		"</strong>",
		" have asked the ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" to find the best match among offers from prospective ",
		"<em>",
		"Sellers",
		"</em>",
		" to this Request for Tenders. The ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" matches incoming bids from potential ",
		"<em>",
		"Sellers",
		"</em>",
		", until the best candidate is found, the first acceptable bid has been received, or the auction expires due to lack of sufficient interest.",
		"</p>\r\n",
		"<h2",
		" id=\"manifest\"",
		">",
		"Manifest",
		"</h2>\r\n",
		"<p>",
		"The ",
		"<strong>",
		"Buyer",
		"</strong>",
		" requests transport of ",
		"<strong>",
		"</strong>",
		" item(s) of ",
		"<strong>",
		"</strong>",
		" (",
		").",
		"</p>\r\n",
		"<h2",
		" id=\"pickupPoint\"",
		">",
		"Pickup Point",
		"</h2>\r\n",
		"<p>",
		"The carrier will pick up the delivery at ",
		"<strong>",
		" ",
		" ",
		"</strong>",
		", ",
		"<strong>",
		" ",
		"</strong>",
		",  ",
		"<strong>",
		"</strong>",
		".",
		"</p>\r\n",
		"<h2",
		" id=\"deliveryPoint\"",
		">",
		"Delivery Point",
		"</h2>\r\n",
		"<p>",
		"The carrier will transport the delivery to ",
		"<strong>",
		" ",
		" ",
		"</strong>",
		", ",
		"<strong>",
		" ",
		"</strong>",
		",  ",
		"<strong>",
		"</strong>",
		".",
		"</p>\r\n",
		"<h2",
		" id=\"auctionConsignment\"",
		">",
		"Auction Consignment",
		"</h2>\r\n",
		"<p>",
		"Upon the signature of this contract, the ",
		"<strong>",
		"Buyer",
		"</strong>",
		" consigns to the ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" the task of finding a service provider that sells the requested service of transporting the item(s) specified in the ",
		"<em>",
		"Manifest",
		"</em>",
		" above, with an initial ",
		"<em>",
		"asking price",
		"</em>",
		" of ",
		"<strong>",
		" ",
		"</strong>",
		". The ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" publishes information about the ",
		"<em>",
		"Request for Tenders",
		"</em>",
		" to prospective ",
		"<em>",
		"Sellers",
		"</em>",
		" who can make ",
		"<em>",
		"Offers",
		"</em>",
		" for the duration of ",
		"<strong>",
		"</strong>",
		" days from the signature of the agreement, or until the first ",
		"<em>",
		"acceptable",
		"</em>",
		" bid of ",
		"<strong>",
		" ",
		"</strong>",
		" is received. Any bid above ",
		"<strong>",
		" ",
		"</strong>",
		" will be automatically rejected. Information of the best bid will also be presented.",
		"</p>\r\n",
		"<p>",
		"The consignment concludes at the end of ",
		"<strong>",
		"</strong>",
		" days from the signature of the agreement, or when the first ",
		"<em>",
		"acceptable",
		"</em>",
		" bid of ",
		"<strong>",
		" ",
		"</strong>",
		" is received. If no bids are available when the auction expires, the consignment concludes without any payments being processed. Otherwise, two payments will be made:",
		"</p>\r\n",
		"<ul>\r\n",
		"<li",
		">",
		"An instant payment representing the agreed upon price for the best or first accepted bid plus the agreed upon commission, as a percentage of the price (",
		"<strong>",
		" %",
		"</strong>",
		"), will be made from the ",
		"<strong>",
		"Buyer",
		"</strong>",
		" to the ",
		"<em>",
		"Auctioneer",
		"</em>",
		".",
		"</li>\r\n",
		"<li",
		">",
		"An instant payment representing the agreed upon price for the best or first accepted bid, will be made from the ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" to the ",
		"<strong>",
		"Seller",
		"</strong>",
		". The ",
		"<strong>",
		"Auctioneer",
		"</strong>",
		" keeps its ",
		"<strong>",
		" %",
		"</strong>",
		" as payment for its services.",
		"</li>\r\n",
		"</ul>\r\n",
		"<p>",
		"All payments will be made using ",
		"<em>",
		"e-Daler",
		"</em>",
		". A buyer without funds in their ",
		"<em>",
		"wallet",
		"</em>",
		" will have their request for tender invalidated, and the sale assignment cancelled.",
		"</p>\r\n"];
	return Segments.join("");
}
