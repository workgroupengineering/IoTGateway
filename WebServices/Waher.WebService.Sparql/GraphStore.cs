﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Multipart;
using Waher.Content.Semantic;
using Waher.IoTGateway;
using Waher.Networking.HTTP;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Script.Persistence.SPARQL.Sources;
using Waher.Security;

namespace Waher.WebService.Sparql
{
	/// <summary>
	/// Graph Store for semantic graphs.
	/// https://www.w3.org/TR/sparql12-graph-store-protocol/
	/// </summary>
	public class GraphStore : HttpSynchronousResource, IHttpGetMethod, IHttpPostMethod, IHttpPutMethod, IHttpDeleteMethod
	{
		private readonly HttpAuthenticationScheme[] authenticationSchemes;

		/// <summary>
		/// Graph Store for semantic graphs.
		/// </summary>
		/// <param name="ResourceName">Name of resource.</param>
		/// <param name="AuthenticationSchemes">Authentication schemes.</param>
		public GraphStore(string ResourceName, params HttpAuthenticationScheme[] AuthenticationSchemes)
			: base(ResourceName)
		{
			this.authenticationSchemes = AuthenticationSchemes;
		}

		/// <summary>
		/// If the resource handles sub-paths.
		/// </summary>
		public override bool HandlesSubPaths => false;

		/// <summary>
		/// If the resource uses user sessions.
		/// </summary>
		public override bool UserSessions => true;

		/// <summary>
		/// If the GET method is allowed.
		/// </summary>
		public bool AllowsGET => true;

		/// <summary>
		/// If the POST method is allowed.
		/// </summary>
		public bool AllowsPOST => true;

		/// <summary>
		/// If the PUT method is allowed.
		/// </summary>
		public bool AllowsPUT => true;

		/// <summary>
		/// If the DELETE method is allowed.
		/// </summary>
		public bool AllowsDELETE => true;

		/// <summary>
		/// Any authentication schemes used to authenticate users before access is granted to the corresponding resource.
		/// </summary>
		/// <param name="Request">Current request</param>
		public override HttpAuthenticationScheme[] GetAuthenticationSchemes(HttpRequest Request)
		{
			return this.authenticationSchemes;
		}

		/// <summary>
		/// Executes the GET method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public async Task GET(HttpRequest Request, HttpResponse Response)
		{
			if (Request.User is null || !Request.User.HasPrivilege(SparqlServiceModule.GetPrivileges))
				throw new ForbiddenException("Access denied.");

			(GraphReference Reference, Uri GraphUri) = await GetGraphReference(Request, false);

			Response.StatusCode = 200;

			if (Request.Header.Method == "HEAD")
				return;

			GraphStoreSource Source = new GraphStoreSource(Reference);
			ISemanticCube Graph = await Source.LoadGraph(GraphUri, null, false);

			await Response.Return(Graph);
		}

		private static async Task<(GraphReference, Uri)> GetGraphReference(HttpRequest Request, bool NullIfNotFound)
		{
			if (!Request.Header.TryGetQueryParameter("graph", out string GraphUri))
				throw new BadRequestException("Missing graph URI.");

			if (!Uri.TryCreate(GraphUri, UriKind.RelativeOrAbsolute, out Uri ParsedUri))
				throw new BadRequestException("Invalid graph URI.");

			if (!ParsedUri.IsAbsoluteUri)
				throw new BadRequestException("Graph URI must be an absolute URI.");

			GraphReference Reference = await Database.FindFirstIgnoreRest<GraphReference>(
				new FilterFieldEqualTo("GraphUri", GraphUri));

			if (Reference is null && !NullIfNotFound)
				throw new NotFoundException("Graph not found.");

			return (Reference, ParsedUri);
		}

		/// <summary>
		/// Executes the POST method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task PUT(HttpRequest Request, HttpResponse Response)
		{
			if (Request.User is null || !Request.User.HasPrivilege(SparqlServiceModule.AddPrivileges))
				throw new ForbiddenException("Access denied.");

			return this.Update(Request, Response, true);
		}

		private async Task Update(HttpRequest Request, HttpResponse Response, bool DeleteOld) 
		{ 
			if (!Request.HasData)
				throw new BadRequestException("No data in request.");

			object Decoded = await Request.DecodeDataAsync();
			List<KeyValuePair<string, string>> Files = new List<KeyValuePair<string, string>>();

			if (Decoded is TurtleDocument TurtleDoc)
				Files.Add(new KeyValuePair<string, string>(TurtleDoc.Text, TurtleCodec.DefaultExtension));
			else if (Decoded is RdfDocument RdfDoc)
				Files.Add(new KeyValuePair<string, string>(RdfDoc.Text, RdfCodec.DefaultExtension));
			else if (Decoded is Dictionary<string, object> Form)
			{
				foreach (KeyValuePair<string, object> P in Form)
				{
					if (P.Value is TurtleDocument TurtleDoc2)
						Files.Add(new KeyValuePair<string, string>(TurtleDoc2.Text, TurtleCodec.DefaultExtension));
					else if (P.Value is RdfDocument RdfDoc2)
						Files.Add(new KeyValuePair<string, string>(RdfDoc2.Text, RdfCodec.DefaultExtension));
					else
						throw new UnsupportedMediaTypeException("Content in form must be semantic triples documents.");
				}
			}
			else if (Decoded is Content.Multipart.MultipartContent Form2)
			{
				foreach (EmbeddedContent P in Form2.Content)
				{
					if (P.Decoded is TurtleDocument TurtleDoc2)
						Files.Add(new KeyValuePair<string, string>(TurtleDoc2.Text, TurtleCodec.DefaultExtension));
					else if (P.Decoded is RdfDocument RdfDoc2)
						Files.Add(new KeyValuePair<string, string>(RdfDoc2.Text, RdfCodec.DefaultExtension));
					else
						throw new UnsupportedMediaTypeException("Content in form must be semantic triples documents.");
				}
			}
			else
				throw new UnsupportedMediaTypeException("Content must be a semantic triples document, or a collection of semantic triples document in a multipart form.");

			(GraphReference Reference, Uri GraphUri) = await GetGraphReference(Request, true);
			DateTime TP = DateTime.UtcNow;
			string H = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(GraphUri.AbsoluteUri));
			string FileName;
			int i = 0;

			if (Reference is null)
			{
				Reference = new GraphReference()
				{
					Created = TP,
					Updated = TP,
					GraphUri = GraphUri.AbsoluteUri,
					NrFiles = Files.Count,
					GraphDigest = H,
					Folder = Path.Combine(Gateway.AppDataFolder, "GraphStore", H)
				};

				if (!Directory.Exists(Reference.Folder))
					Directory.CreateDirectory(Reference.Folder);

				foreach (KeyValuePair<string, string> P in Files)
				{
					FileName = Path.Combine(Reference.Folder, (++i).ToString());
					FileName = Path.ChangeExtension(FileName, P.Value);

					await Resources.WriteAllTextAsync(FileName, P.Key);
				}

				await Database.Insert(Reference);

				Response.StatusCode = 201;
				Response.StatusMessage = "Created";
			}
			else
			{
				Reference.Updated = TP;

				if (DeleteOld)
				{
					foreach (string FileName2 in Directory.GetFiles(Reference.Folder, "*.*", SearchOption.TopDirectoryOnly))
						File.Delete(FileName2);
				
					Reference.NrFiles = Files.Count;
				}
				else
				{
					i = Reference.NrFiles;
					Reference.NrFiles += Files.Count;
				}

				foreach (KeyValuePair<string, string> P in Files)
				{
					FileName = Path.Combine(Reference.Folder, (++i).ToString());
					FileName = Path.ChangeExtension(FileName, P.Value);

					await Resources.WriteAllTextAsync(FileName, P.Key);
				}

				await Database.Update(Reference);

				Response.StatusCode = 200;  // OK
			}
		}

		/// <summary>
		/// Executes the POST method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public async Task DELETE(HttpRequest Request, HttpResponse Response)
		{
			if (Request.User is null || !Request.User.HasPrivilege(SparqlServiceModule.DeletePrivileges))
				throw new ForbiddenException("Access denied.");

			(GraphReference Reference, Uri _) = await GetGraphReference(Request, false);
			bool FilesDeleted = false;

			foreach (string FileName2 in Directory.GetFiles(Reference.Folder, "*.*", SearchOption.TopDirectoryOnly))
			{
				File.Delete(FileName2);
				FilesDeleted = true;
			}

			await Database.Delete(Reference);

			if (FilesDeleted)
				Response.StatusCode = 200;
			else
			{
				Response.StatusCode = 204;
				Response.StatusMessage = "No Content";
			}
		}

		/// <summary>
		/// Executes the POST method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task POST(HttpRequest Request, HttpResponse Response)
		{
			if (Request.User is null || !Request.User.HasPrivilege(SparqlServiceModule.UpdatePrivileges))
				throw new ForbiddenException("Access denied.");

			return this.Update(Request, Response, false);
		}
	}
}