﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
#else
using System.Security.Authentication;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
#endif
using Waher.Content;
using Waher.Events;
using Waher.Events.Statistics;
using Waher.Networking.HTTP.HeaderFields;
using Waher.Networking.Sniffers;
using Waher.Networking.HTTP.TransferEncodings;
using Waher.Networking.HTTP.Vanity;
using Waher.Runtime.Cache;
using Waher.Script;
using Waher.Security;

namespace Waher.Networking.HTTP
{
	/// <summary>
	/// Implements an HTTP server.
	/// </summary>
	public class HttpServer : Sniffable, IDisposable, IResourceMap
	{
		/// <summary>
		/// Default HTTP Port (80).
		/// </summary>
		public const int DefaultHttpPort = 80;

		/// <summary>
		/// Default HTTPS port (443).
		/// </summary>
		public const int DefaultHttpsPort = 443;

		/// <summary>
		/// Default Connection backlog (10).
		/// </summary>
		public const int DefaultConnectionBacklog = 10;

		/// <summary>
		/// Default buffer size (16384).
		/// </summary>
		public const int DefaultBufferSize = 16384;

		internal static readonly Variables globalVariables = new Variables();

		/// <summary>
		/// Reference to global collection of variables.
		/// </summary>
		public static Variables GlobalVariables => globalVariables;

#if WINDOWS_UWP
		private LinkedList<KeyValuePair<StreamSocketListener, Guid>> listeners = new LinkedList<KeyValuePair<StreamSocketListener, Guid>>();
#else
		private LinkedList<KeyValuePair<TcpListener, bool>> listeners = new LinkedList<KeyValuePair<TcpListener, bool>>();
		private X509Certificate serverCertificate;
		private Dictionary<int, KeyValuePair<ClientCertificates, bool>> portSpecificMTlsSettings;
		private ClientCertificates clientCertificates = ClientCertificates.NotUsed;
		private bool trustClientCertificates = false;
		private bool clientCertificateSettingsLocked = false;
#endif
		private readonly Dictionary<string, HttpResource> resources = new Dictionary<string, HttpResource>(StringComparer.CurrentCultureIgnoreCase);
		private TimeSpan sessionTimeout = TimeSpan.FromMinutes(20);
		private TimeSpan requestTimeout = TimeSpan.FromMinutes(2);
		private Cache<HttpRequest, RequestInfo> currentRequests;
		private Cache<string, Variables> sessions;
		private string resourceOverride = null;
		private Regex resourceOverrideFilter = null;
		private readonly object statSynch = new object();
		private Dictionary<string, Statistic> callsPerMethod = new Dictionary<string, Statistic>();
		private Dictionary<string, Statistic> callsPerUserAgent = new Dictionary<string, Statistic>();
		private Dictionary<string, Statistic> callsPerFrom = new Dictionary<string, Statistic>();
		private Dictionary<string, Statistic> callsPerResource = new Dictionary<string, Statistic>();
		private readonly Dictionary<Guid, HttpClientConnection> connections = new Dictionary<Guid, HttpClientConnection>();
		private readonly Dictionary<int, bool> failedPorts = new Dictionary<int, bool>();
		private readonly VanityResources vanityResources = new VanityResources();
		private ILoginAuditor loginAuditor = null;
		private DateTime lastStat = DateTime.MinValue;
		private string eTagSalt = string.Empty;
		private string name = typeof(HttpServer).Namespace;
		private int[] httpPorts;
		private long nrBytesRx = 0;
		private long nrBytesTx = 0;
		private long nrCalls = 0;
#if !WINDOWS_UWP
		private int[] httpsPorts;
		private int? upgradePort = null;
		private bool closed = false;
#endif
		private bool adaptToNetworkChanges;

		#region Constructors

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(params ISniffer[] Sniffers)
#if WINDOWS_UWP
			: this(new int[] { DefaultHttpPort }, false, Sniffers)
#else
			: this(new int[] { DefaultHttpPort }, null, null, false, Sniffers)
#endif
		{
		}

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPort">HTTP Port</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int HttpPort, params ISniffer[] Sniffers)
#if WINDOWS_UWP
			: this(new int[] { HttpPort }, false, Sniffers)
#else
			: this(new int[] { HttpPort }, null, null, false, Sniffers)
#endif
		{
		}

#if !WINDOWS_UWP
		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="ServerCertificate">Server certificate identifying the domain of the server.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(X509Certificate ServerCertificate, params ISniffer[] Sniffers)
			: this(new int[] { DefaultHttpPort }, new int[] { DefaultHttpsPort }, ServerCertificate, false, Sniffers)
		{
		}

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPort">HTTP Port</param>
		/// <param name="HttpsPort">HTTPS Port</param>
		/// <param name="ServerCertificate">Server certificate identifying the domain of the server.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int HttpPort, int HttpsPort, X509Certificate ServerCertificate, params ISniffer[] Sniffers)
			: this(new int[] { HttpPort }, new int[] { HttpsPort }, ServerCertificate, false, Sniffers)
		{
		}
#endif

#if WINDOWS_UWP
		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPorts">HTTP Ports</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int[] HttpPorts, params ISniffer[] Sniffers)
			: this(HttpPorts, false, Sniffers)
		{
		}

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPorts">HTTP Ports</param>
		/// <param name="AdaptToNetworkChanges">If the server is to adapt to network changes automatically.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int[] HttpPorts, bool AdaptToNetworkChanges, params ISniffer[] Sniffers)
#else
		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPorts">HTTP Ports</param>
		/// <param name="Sniffers">Sniffers.</param>
		/// <param name="HttpsPorts">HTTPS Ports</param>
		/// <param name="ServerCertificate">Server certificate identifying the domain of the server.</param>
		public HttpServer(int[] HttpPorts, int[] HttpsPorts, X509Certificate ServerCertificate, params ISniffer[] Sniffers)
			: this(HttpPorts, HttpsPorts, ServerCertificate, false, Sniffers)
		{
		}

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPorts">HTTP Ports</param>
		/// <param name="HttpsPorts">HTTPS Ports</param>
		/// <param name="ServerCertificate">Server certificate identifying the domain of the server.</param>
		/// <param name="AdaptToNetworkChanges">If the server is to adapt to network changes automatically.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int[] HttpPorts, int[] HttpsPorts, X509Certificate ServerCertificate, bool AdaptToNetworkChanges,
			params ISniffer[] Sniffers)
			: this(HttpPorts, HttpsPorts, ServerCertificate, AdaptToNetworkChanges, ClientCertificates.NotUsed, false, null, false, Sniffers)
		{
		}

		/// <summary>
		/// Implements an HTTPS server.
		/// </summary>
		/// <param name="HttpPorts">HTTP Ports</param>
		/// <param name="HttpsPorts">HTTPS Ports</param>
		/// <param name="ServerCertificate">Server certificate identifying the domain of the server.</param>
		/// <param name="AdaptToNetworkChanges">If the server is to adapt to network changes automatically.</param>
		/// <param name="ClientCertificates">If client certificates are not used, optional or required.</param>
		/// <param name="TrustClientCertificates">If client certificates should be trusted, even if they do not validate.</param>
		/// <param name="PortSpecificSettings">Port-specific mTLS settings.</param>
		/// <param name="LockSettings">If client certificate settings should be locked.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public HttpServer(int[] HttpPorts, int[] HttpsPorts, X509Certificate ServerCertificate, bool AdaptToNetworkChanges,
			ClientCertificates ClientCertificates, bool TrustClientCertificates,
			Dictionary<int, KeyValuePair<ClientCertificates, bool>> PortSpecificSettings, bool LockSettings,
			params ISniffer[] Sniffers)
#endif
			: base(Sniffers)
		{
#if !WINDOWS_UWP
			this.serverCertificate = ServerCertificate;
			this.clientCertificates = ClientCertificates;
			this.trustClientCertificates = TrustClientCertificates;
			this.portSpecificMTlsSettings = PortSpecificSettings;
			this.clientCertificateSettingsLocked = LockSettings;
#endif
			this.sessions = new Cache<string, Variables>(int.MaxValue, TimeSpan.MaxValue, this.sessionTimeout, true);
			this.sessions.Removed += this.Sessions_Removed;
			this.currentRequests = new Cache<HttpRequest, RequestInfo>(int.MaxValue, TimeSpan.MaxValue, this.requestTimeout, true);
			this.currentRequests.Removed += this.CurrentRequests_Removed;
			this.lastStat = DateTime.Now;
			this.adaptToNetworkChanges = AdaptToNetworkChanges;

			this.httpPorts = new int[0];

#if WINDOWS_UWP
			Task _ = this.AddHttpPorts(HttpPorts);

			if (this.adaptToNetworkChanges)
				NetworkInformation.NetworkStatusChanged += this.NetworkChange_NetworkAddressChanged;
#else
			this.AddHttpPorts(HttpPorts);
			this.httpsPorts = new int[0];
			this.AddHttpsPorts(HttpsPorts);

			if (this.adaptToNetworkChanges)
				NetworkChange.NetworkAddressChanged += this.NetworkChange_NetworkAddressChanged;
#endif
		}

#if WINDOWS_UWP
		private void NetworkChange_NetworkAddressChanged(object sender)
		{
			Task _ = this.NetworkChanged();
		}

		/// <summary>
		/// Adapts the server to changes in the network. This method can be called automatically by calling the constructor accordingly.
		/// </summary>
		public async Task NetworkChanged()
#else
		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			this.NetworkChanged();
		}

		/// <summary>
		/// Adapts the server to changes in the network. This method can be called automatically by calling the constructor accordingly.
		/// </summary>
		public async void NetworkChanged()
#endif
		{
			try
			{
				int[] HttpPorts = this.httpPorts;
				this.httpPorts = new int[0];

#if WINDOWS_UWP
				LinkedList<KeyValuePair<StreamSocketListener, Guid>> Listeners = this.listeners;
				this.listeners = new LinkedList<KeyValuePair<StreamSocketListener, Guid>>();

				foreach (KeyValuePair<StreamSocketListener, Guid> P in Listeners)
				{
					try
					{
						P.Key.Dispose();
					}
					catch (Exception ex)
					{
						Log.Exception(ex);
					}
				}

				await this.AddHttpPorts(HttpPorts, Listeners);
#else
				int[] HttpsPorts = this.httpsPorts;
				this.httpsPorts = new int[0];
				LinkedList<KeyValuePair<TcpListener, bool>> Listeners = this.listeners;
				this.listeners = new LinkedList<KeyValuePair<TcpListener, bool>>();

				foreach (KeyValuePair<TcpListener, bool> P in Listeners)
				{
					try
					{
						P.Key.Stop();
					}
					catch (Exception ex)
					{
						Log.Exception(ex);
					}
				}

				this.AddHttpPorts(HttpPorts, Listeners);
				this.AddHttpsPorts(HttpsPorts, Listeners);
#endif
				await this.OnNetworkChanged.Raise(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Event raised when the network has been changed.
		/// </summary>
		public event EventHandlerAsync OnNetworkChanged = null;

		/// <summary>
		/// If the server is to adapt to network changes automatically.
		/// </summary>
		public bool AdaptToNetworkChanges
		{
			get => this.adaptToNetworkChanges;

			set
			{
				if (value != this.adaptToNetworkChanges)
				{
					this.adaptToNetworkChanges = value;

					if (value)
					{
#if WINDOWS_UWP
						NetworkInformation.NetworkStatusChanged += this.NetworkChange_NetworkAddressChanged;
#else
						NetworkChange.NetworkAddressChanged += this.NetworkChange_NetworkAddressChanged;
#endif
					}
					else
					{
#if WINDOWS_UWP
						NetworkInformation.NetworkStatusChanged -= this.NetworkChange_NetworkAddressChanged;
#else
						NetworkChange.NetworkAddressChanged -= this.NetworkChange_NetworkAddressChanged;
#endif
					}
				}
			}
		}

#if WINDOWS_UWP
		/// <summary>
		/// Opens additional HTTP ports, if not already open.
		/// </summary>
		/// <param name="HttpPorts">HTTP ports</param>
		public async Task AddHttpPorts(params int[] HttpPorts)
		{
			await this.AddHttpPorts(HttpPorts, null);
		}
#else
		/// <summary>
		/// Opens additional HTTP ports, if not already open.
		/// </summary>
		/// <param name="HttpPorts">HTTP ports</param>33
		public void AddHttpPorts(params int[] HttpPorts)
		{
			this.AddHttpPorts(HttpPorts, null);
		}
#endif

#if WINDOWS_UWP
		private async Task AddHttpPorts(int[] HttpPorts, LinkedList<KeyValuePair<StreamSocketListener, Guid>> Listeners)
#else
		private void AddHttpPorts(int[] HttpPorts, LinkedList<KeyValuePair<TcpListener, bool>> Listeners)
#endif
		{
			if (HttpPorts is null)
				return;

			try
			{
#if WINDOWS_UWP
				StreamSocketListener Listener;

				foreach (ConnectionProfile Profile in NetworkInformation.GetConnectionProfiles())
				{
					if (Profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.None)
						continue;

					foreach (int HttpPort in HttpPorts)
					{
						if (Array.IndexOf(this.httpPorts, HttpPort) >= 0)
							continue;

						Listener = null;

						LinkedListNode<KeyValuePair<StreamSocketListener, Guid>> Node;

						Node = Listeners?.First;
						while (!(Node is null))
						{
							StreamSocketListener L = Node.Value.Key;
							Guid AdapterId = Node.Value.Value;

							if (AdapterId == Profile.NetworkAdapter.NetworkAdapterId)
							{
								Listener = L;
								Listeners.Remove(Node);
								break;
							}

							Node = Node.Next;
						}

						if (Listener is null)
						{
							try
							{
								Listener = new StreamSocketListener();
								await Listener.BindServiceNameAsync(HttpPort.ToString(), SocketProtectionLevel.PlainSocket, Profile.NetworkAdapter);
								Listener.ConnectionReceived += this.Listener_ConnectionReceived;

								this.listeners.AddLast(new KeyValuePair<StreamSocketListener, Guid>(Listener, Profile.NetworkAdapter.NetworkAdapterId));
							}
							catch (Exception ex)
							{
								this.failedPorts[HttpPort] = true;
								Log.Exception(ex, Profile.ProfileName);
							}
						}
						else
							this.listeners.AddLast(new KeyValuePair<StreamSocketListener, Guid>(Listener, Profile.NetworkAdapter.NetworkAdapterId));
					}
				}
#else
				TcpListener Listener;

				foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (Interface.OperationalStatus != OperationalStatus.Up)
						continue;

					IPInterfaceProperties Properties = Interface.GetIPProperties();

					foreach (UnicastIPAddressInformation UnicastAddress in Properties.UnicastAddresses)
					{
						if ((UnicastAddress.Address.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4) ||
							(UnicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6))
						{
							foreach (int HttpPort in HttpPorts)
							{
								if (Array.IndexOf(this.httpPorts, HttpPort) >= 0)
									continue;

								Listener = null;

								LinkedListNode<KeyValuePair<TcpListener, bool>> Node;
								IPEndPoint DesiredEndpoint = new IPEndPoint(UnicastAddress.Address, HttpPort);

								Node = Listeners?.First;
								while (!(Node is null))
								{
									TcpListener L = Node.Value.Key;
									bool Tls = Node.Value.Value;

									if ((!Tls) && L.LocalEndpoint == DesiredEndpoint)
									{
										Listener = L;
										Listeners.Remove(Node);
										break;
									}

									Node = Node.Next;
								}

								if (Listener is null)
								{
									try
									{
										Listener = new TcpListener(UnicastAddress.Address, HttpPort);
										Listener.Start(DefaultConnectionBacklog);
										Task T = this.ListenForIncomingConnections(Listener, false, ClientCertificates.NotUsed, false);

										this.listeners.AddLast(new KeyValuePair<TcpListener, bool>(Listener, false));
									}
									catch (SocketException)
									{
										this.failedPorts[HttpPort] = true;
										Log.Error("Unable to open HTTP port for listening.",
											new KeyValuePair<string, object>("Address", UnicastAddress.Address.ToString()),
											new KeyValuePair<string, object>("Port", HttpPort));
									}
									catch (Exception ex)
									{
										this.failedPorts[HttpPort] = true;
										Log.Exception(ex, UnicastAddress.Address.ToString() + ":" + HttpPort);
									}
								}
								else
									this.listeners.AddLast(new KeyValuePair<TcpListener, bool>(Listener, false));
							}
						}
					}
				}
#endif
				foreach (int HttpPort in HttpPorts)
				{
					if (Array.IndexOf(this.httpPorts, HttpPort) < 0)
					{
						int c = this.httpPorts.Length;
						Array.Resize(ref this.httpPorts, c + 1);
						this.httpPorts[c] = HttpPort;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

#if !WINDOWS_UWP
		/// <summary>
		/// Opens additional HTTPS ports, if not already open.
		/// </summary>
		/// <param name="HttpsPorts">HTTP ports</param>
		public void AddHttpsPorts(params int[] HttpsPorts)
		{
			this.AddHttpsPorts(HttpsPorts, null);
		}

		private void AddHttpsPorts(int[] HttpsPorts, LinkedList<KeyValuePair<TcpListener, bool>> Listeners)
		{
			if (HttpsPorts is null)
				return;

			try
			{
				TcpListener Listener;

				foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (Interface.OperationalStatus != OperationalStatus.Up)
						continue;

					IPInterfaceProperties Properties = Interface.GetIPProperties();

					foreach (UnicastIPAddressInformation UnicastAddress in Properties.UnicastAddresses)
					{
						if ((UnicastAddress.Address.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4) ||
							(UnicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6))
						{
							foreach (int HttpsPort in HttpsPorts)
							{
								if (Array.IndexOf(this.httpsPorts, HttpsPort) >= 0)
									continue;

								Listener = null;

								LinkedListNode<KeyValuePair<TcpListener, bool>> Node;
								IPEndPoint DesiredEndpoint = new IPEndPoint(UnicastAddress.Address, HttpsPort);

								Node = Listeners?.First;
								while (!(Node is null))
								{
									TcpListener L = Node.Value.Key;
									bool Tls = Node.Value.Value;

									if (Tls && L.LocalEndpoint == DesiredEndpoint)
									{
										Listener = L;
										Listeners.Remove(Node);
										break;
									}

									Node = Node.Next;
								}

								if (Listener is null)
								{
									try
									{
										this.GetMTlsSettings(HttpsPort, out ClientCertificates ClientCertificates, out bool TrustCertificates);

										Listener = new TcpListener(DesiredEndpoint);
										Listener.Start(DefaultConnectionBacklog);
										Task T = this.ListenForIncomingConnections(Listener, true, ClientCertificates, TrustCertificates);

										this.listeners.AddLast(new KeyValuePair<TcpListener, bool>(Listener, true));
									}
									catch (SocketException)
									{
										this.failedPorts[HttpsPort] = true;
										Log.Error("Unable to open HTTPS port for listening.",
											new KeyValuePair<string, object>("Address", UnicastAddress.Address.ToString()),
											new KeyValuePair<string, object>("Port", HttpsPort));
									}
									catch (Exception ex)
									{
										this.failedPorts[HttpsPort] = true;
										Log.Exception(ex, UnicastAddress.Address.ToString() + ":" + HttpsPort);
									}
								}
								else
									this.listeners.AddLast(new KeyValuePair<TcpListener, bool>(Listener, true));
							}
						}
					}
				}

				foreach (int HttpsPort in HttpsPorts)
				{
					if (Array.IndexOf(this.httpsPorts, HttpsPort) < 0)
					{
						int c = this.httpsPorts.Length;
						Array.Resize(ref this.httpsPorts, c + 1);
						this.httpsPorts[c] = HttpsPort;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}

			this.upgradePort = null;
		}
#endif

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
#if WINDOWS_UWP
			NetworkInformation.NetworkStatusChanged -= this.NetworkChange_NetworkAddressChanged;
#else
			this.closed = true;
			NetworkChange.NetworkAddressChanged -= this.NetworkChange_NetworkAddressChanged;
#endif

			if (!(this.listeners is null))
			{
#if WINDOWS_UWP
				LinkedList<KeyValuePair<StreamSocketListener, Guid>> Listeners = this.listeners;
				this.listeners = null;

				foreach (KeyValuePair<StreamSocketListener, Guid> Listener in Listeners)
					Listener.Key.Dispose();
#else
				LinkedList<KeyValuePair<TcpListener, bool>> Listeners = this.listeners;
				this.listeners = null;

				foreach (KeyValuePair<TcpListener, bool> Listener in Listeners)
					Listener.Key.Stop();
#endif
			}

			this.sessions?.Dispose();
			this.sessions = null;

			this.currentRequests?.Dispose();
			this.currentRequests = null;
		}

		/// <summary>
		/// Ports successfully opened.
		/// </summary>
		public int[] OpenPorts
		{
			get
			{
				return this.GetPorts(true, true);
			}
		}

		/// <summary>
		/// HTTP Ports successfully opened.
		/// </summary>
		public int[] OpenHttpPorts
		{
			get
			{
				return this.GetPorts(true, false);
			}
		}

		/// <summary>
		/// HTTPS Ports successfully opened.
		/// </summary>
		public int[] OpenHttpsPorts
		{
			get
			{
				return this.GetPorts(false, true);
			}
		}

		/// <summary>
		/// IP Addresses receiving requests on.
		/// </summary>
		public IPAddress[] LocalIpAddresses
		{
			get
			{
				Dictionary<IPAddress, bool> Addresses = new Dictionary<IPAddress, bool>();

#if WINDOWS_UWP
				foreach (HostName HostName in NetworkInformation.GetHostNames())
				{
					if ((HostName.Type == HostNameType.Ipv4 || HostName.Type == HostNameType.Ipv6) &&
						!(HostName.IPInformation?.NetworkAdapter is null) &&
						IPAddress.TryParse(HostName.CanonicalName, out IPAddress Addr))
					{
						Addresses[Addr] = true;
					}
				}
#else
				foreach (KeyValuePair<TcpListener, bool> P in this.listeners)
				{
					if (P.Key.LocalEndpoint is IPEndPoint Endpoint)
						Addresses[Endpoint.Address] = true;
				}
#endif
				IPAddress[] Result = new IPAddress[Addresses.Count];
				Addresses.Keys.CopyTo(Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Salt value used when calculating ETag values.
		/// </summary>
		public string ETagSalt => this.eTagSalt;

		/// <summary>
		/// Sets a new salt value used when calculating ETag values.
		/// </summary>
		public async Task SetETagSalt(string NewSalt)
		{
			if (this.eTagSalt != NewSalt)
			{
				this.eTagSalt = NewSalt;

				await this.ETagSaltChanged.Raise(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Server name. This string will be shown on the Server header field if nothing else is provided. If it is blank,
		/// the server header field will be omitted.
		/// </summary>
		public string Name
		{
			get => this.name;
			set => this.name = value;
		}

		/// <summary>
		/// Event raised when the <see cref="ETagSalt"/> value has changed.
		/// </summary>
		public event EventHandlerAsync ETagSaltChanged = null;

		private int[] GetPorts(bool Http, bool Https)
		{
			SortedDictionary<int, bool> Open = new SortedDictionary<int, bool>();

			if (!(this.listeners is null))
			{
#if WINDOWS_UWP
				foreach (KeyValuePair<StreamSocketListener, Guid> Listener in this.listeners)
				{
					if (Http)
					{
						if (int.TryParse(Listener.Key.Information.LocalPort, out int i) && !this.failedPorts.ContainsKey(i))
							Open[i] = true;
					}
				}
#else
				IPEndPoint IPEndPoint;

				foreach (KeyValuePair<TcpListener, bool> Listener in this.listeners)
				{
					if ((Listener.Value && Https) || ((!Listener.Value) && Http))
					{
						IPEndPoint = Listener.Key.LocalEndpoint as IPEndPoint;
						if (!(IPEndPoint is null) && !this.failedPorts.ContainsKey(IPEndPoint.Port))
							Open[IPEndPoint.Port] = true;
					}
				}
#endif
			}

			int[] Result = new int[Open.Count];
			Open.Keys.CopyTo(Result, 0);

			return Result;
		}

#if !WINDOWS_UWP
		internal int? UpgradePort
		{
			get
			{
				if (this.upgradePort.HasValue)
					return this.upgradePort;

				if (this.serverCertificate is null)
					return null;

				int? Result = null;
				int Port;

				if (!(this.listeners is null))
				{
					IPEndPoint IPEndPoint;

					foreach (KeyValuePair<TcpListener, bool> Listener in this.listeners)
					{
						if (Listener.Value)
						{
							IPEndPoint = Listener.Key.LocalEndpoint as IPEndPoint;
							if (!(IPEndPoint is null) && !this.failedPorts.ContainsKey(Port = IPEndPoint.Port))
							{
								if (Port == DefaultHttpsPort || !Result.HasValue)
									Result = Port;
							}
						}
					}
				}

				this.upgradePort = Result;

				return Result;
			}
		}

		/// <summary>
		/// Updates the server certificate
		/// </summary>
		/// <param name="ServerCertificate">Server Certificate.</param>
		public void UpdateCertificate(X509Certificate ServerCertificate)
		{
			this.serverCertificate = ServerCertificate;
			this.upgradePort = null;
		}

		/// <summary>
		/// Configures Mutual-TLS capabilities of the server. Affects all connections, all resources.
		/// </summary>
		/// <param name="ClientCertificates">If client certificates are not used, optional or required.</param>
		/// <param name="TrustClientCertificates">If client certificates should be trusted, even if they do not validate.</param>
		/// <param name="LockSettings">If client certificate settings should be locked.</param>
		public void ConfigureMutualTls(ClientCertificates ClientCertificates, bool TrustClientCertificates, bool LockSettings)
		{
			this.ConfigureMutualTls(ClientCertificates, TrustClientCertificates, null, LockSettings);
		}

		/// <summary>
		/// Configures Mutual-TLS capabilities of the server. Affects all connections, all resources.
		/// </summary>
		/// <param name="ClientCertificates">If client certificates are not used, optional or required.</param>
		/// <param name="TrustClientCertificates">If client certificates should be trusted, even if they do not validate.</param>
		/// <param name="PortSpecificSettings">Port-specific mTLS settings.</param>
		/// <param name="LockSettings">If client certificate settings should be locked.</param>
		public void ConfigureMutualTls(ClientCertificates ClientCertificates, bool TrustClientCertificates,
			Dictionary<int, KeyValuePair<ClientCertificates, bool>> PortSpecificSettings, bool LockSettings)
		{
			if (this.clientCertificateSettingsLocked)
				throw new InvalidOperationException("Mutual TLS settings locked.");

			this.clientCertificates = ClientCertificates;
			this.trustClientCertificates = TrustClientCertificates;
			this.portSpecificMTlsSettings = PortSpecificSettings;
			this.clientCertificateSettingsLocked = LockSettings;
		}

		/// <summary>
		/// If client certificates are not used by default, optional or required.
		/// </summary>
		public ClientCertificates ClientCertificates => this.clientCertificates;

		/// <summary>
		/// If client certificates should be trusted by default, even if they do not validate.
		/// </summary>
		public bool TrustClientCertificates => this.trustClientCertificates;

		/// <summary>
		/// Gets mTLS settings for a given port number.
		/// </summary>
		/// <param name="Port">Port number.</param>
		/// <param name="ClientCertificates">How to configure mTLS for the corresponding port number.</param>
		/// <param name="TrustClientCertificates">If client certificates are to be trusted by default.</param>
		public void GetMTlsSettings(int Port, out ClientCertificates ClientCertificates, out bool TrustClientCertificates)
		{
			if (!(this.portSpecificMTlsSettings is null) && this.portSpecificMTlsSettings.TryGetValue(Port,
				out KeyValuePair<ClientCertificates, bool> P))
			{
				ClientCertificates = P.Key;
				TrustClientCertificates = P.Value;
			}
			else
			{
				ClientCertificates = this.clientCertificates;
				TrustClientCertificates = this.trustClientCertificates;
			}
		}
#endif

		#endregion

		#region Connections

#if WINDOWS_UWP
		private void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
		{
			try
			{
				StreamSocket Client = args.Socket;

				this.Information("Connection accepted from " + Client.Information.RemoteAddress.ToString() + ":" + Client.Information.RemotePort + ".");

				BinaryTcpClient BinaryTcpClient = new BinaryTcpClient(Client);
				BinaryTcpClient.Bind(true);
				HttpClientConnection Connection = new HttpClientConnection(this, BinaryTcpClient, false, this.Sniffers);
				BinaryTcpClient.Continue();

				lock (this.connections)
				{
					this.connections[Connection.Id] = Connection;
				}
			}
			catch (SocketException)
			{
				// Ignore
			}
			catch (Exception ex)
			{
				if (this.listeners is null)
					return;

				Log.Exception(ex);
			}
		}
#else
		private async Task ListenForIncomingConnections(TcpListener Listener, bool Tls, ClientCertificates ClientCertificates,
			bool TrustCertificates)
		{
			try
			{
				while (!this.closed)
				{
					try
					{
						TcpClient Client;

						try
						{
							Client = await Listener.AcceptTcpClientAsync();
							if (this.closed)
								return;
						}
						catch (InvalidOperationException)
						{
							LinkedListNode<KeyValuePair<TcpListener, bool>> Node = this.listeners?.First;

							while (!(Node is null))
							{
								if (Node.Value.Key == Listener)
								{
									this.listeners.Remove(Node);
									break;
								}

								Node = Node.Next;
							}

							return;
						}

						if (!(Client is null))
						{
							await this.Information("Connection accepted from " + Client.Client.RemoteEndPoint.ToString() + ".");

							BinaryTcpClient BinaryTcpClient = new BinaryTcpClient(Client);
							BinaryTcpClient.Bind(true);

							if (Tls)
							{
								Task _ = this.SwitchToTls(BinaryTcpClient, ClientCertificates, TrustCertificates);
							}
							else
							{
								HttpClientConnection Connection = new HttpClientConnection(this, BinaryTcpClient, false, this.Sniffers);
								BinaryTcpClient.Continue();

								lock (this.connections)
								{
									this.connections[Connection.Id] = Connection;
								}
							}
						}
					}
					catch (SocketException)
					{
						// Ignore
					}
					catch (ObjectDisposedException)
					{
						// Ignore
					}
					catch (NullReferenceException)
					{
						// Ignore
					}
					catch (Exception ex)
					{
						if (this.closed || this.listeners is null)
							break;

						bool Found = false;

						foreach (KeyValuePair<TcpListener, bool> P in this.listeners)
						{
							if (P.Key == Listener)
							{
								Found = true;
								break;
							}
						}

						if (Found)
							Log.Exception(ex);
						else
							break;  // Removed, for instance due to network change
					}
				}
			}
			catch (Exception ex)
			{
				if (this.closed || this.listeners is null)
					return;

				Log.Exception(ex);
			}
		}

		private async Task SwitchToTls(BinaryTcpClient Client, ClientCertificates ClientCertificates, bool TrustCertificates)
		{
			string RemoteIpEndpoint;
			EndPoint EP = Client.Client.Client.RemoteEndPoint;

			if (EP is IPEndPoint IpEP)
				RemoteIpEndpoint = IpEP.Address.ToString();
			else
				RemoteIpEndpoint = EP.ToString();

			if (Security.LoginMonitor.LoginAuditor.CanStartTls(RemoteIpEndpoint))
			{
				try
				{
					if (this.HasSniffers)
					{
						await this.Information("Switching to TLS. (Client Certificates: " + ClientCertificates.ToString() +
							", Trust Certificates: " + TrustCertificates.ToString() + ")");
					}

					await Client.UpgradeToTlsAsServer(this.serverCertificate, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
						ClientCertificates, null, TrustCertificates);

					if (this.HasSniffers)
					{
						await this.Information("TLS established" +
							". Cipher Strength: " + Client.CipherStrength.ToString() +
							", Hash Strength: " + Client.HashStrength.ToString() +
							", Key Exchange Strength: " + Client.KeyExchangeStrength.ToString());

						if (!(Client.RemoteCertificate is null))
						{
							if (this.HasSniffers)
							{
								StringBuilder sb = new StringBuilder();

								sb.Append("Remote Certificate received. Valid: ");
								sb.Append(Client.RemoteCertificateValid.ToString());
								sb.Append(", Subject: ");
								sb.Append(Client.RemoteCertificate.Subject);
								sb.Append(", Issuer: ");
								sb.Append(Client.RemoteCertificate.Issuer);
								sb.Append(", S/N: ");
								sb.Append(Convert.ToBase64String(Client.RemoteCertificate.GetSerialNumber()));
								sb.Append(", Hash: ");
								sb.Append(Convert.ToBase64String(Client.RemoteCertificate.GetCertHash()));

								await this.Information(sb.ToString());
							}
						}
					}

					HttpClientConnection Connection = new HttpClientConnection(this, Client, true, this.Sniffers);

					if (this.HasSniffers)
					{
						foreach (ISniffer Sniffer in this.Sniffers)
							Connection.Add(Sniffer);
					}

					Client.Continue();

					lock (this.connections)
					{
						this.connections[Connection.Id] = Connection;
					}
				}
				catch (AuthenticationException ex)
				{
					await this.LoginFailure(ex, Client, RemoteIpEndpoint);
				}
				catch (Win32Exception ex)
				{
					await this.LoginFailure(ex, Client, RemoteIpEndpoint);
				}
				catch (SocketException)
				{
					Client.Dispose();
				}
				catch (IOException)
				{
					Client.Dispose();
				}
				catch (Exception ex)
				{
					Client.Dispose();
					Log.Exception(ex);
				}
			}
			else
				Client.Dispose();
		}

		private async Task LoginFailure(Exception ex, BinaryTcpClient Client, string RemoteIpEndpoint)
		{
			Exception ex2 = Log.UnnestException(ex);
			await Security.LoginMonitor.LoginAuditor.ReportTlsHackAttempt(RemoteIpEndpoint, "TLS handshake failed: " + ex2.Message, "HTTPS");

			Client.Dispose();
		}
#endif

		internal bool Remove(HttpClientConnection Connection)
		{
			lock (this.connections)
			{
				return this.connections.Remove(Connection.Id);
			}
		}

		internal HttpClientConnection[] GetConnections()
		{
			HttpClientConnection[] Connections;

			lock (this.connections)
			{
				Connections = new HttpClientConnection[this.connections.Count];
				this.connections.Values.CopyTo(Connections, 0);
			}

			return Connections;
		}

		/// <summary>
		/// <see cref="ISniffable.Add"/>
		/// </summary>
		public override void Add(ISniffer Sniffer)
		{
			base.Add(Sniffer);

			foreach (HttpClientConnection Connection in this.GetConnections())
			{
				if (!Connection.Disposed)
					Connection.Add(Sniffer);
			}
		}

		/// <summary>
		/// <see cref="ISniffable.AddRange"/>
		/// </summary>
		public override void AddRange(IEnumerable<ISniffer> Sniffers)
		{
			base.AddRange(Sniffers);

			foreach (HttpClientConnection Connection in this.GetConnections())
			{
				if (!Connection.Disposed)
					Connection.AddRange(Sniffers);
			}
		}

		/// <summary>
		/// <see cref="ISniffable.Remove"/>
		/// </summary>
		public override bool Remove(ISniffer Sniffer)
		{
			bool Result = base.Remove(Sniffer);

			foreach (HttpClientConnection Connection in this.GetConnections())
			{
				if (!Connection.Disposed)
					Connection.Remove(Sniffer);
			}

			return Result;
		}

		#endregion

		#region Resources

		/// <summary>
		/// By default, this property is null. If not null, or empty, every request made to the web server will
		/// be redirected to this resource.
		/// </summary>
		public string ResourceOverride
		{
			get => this.resourceOverride;
			set => this.resourceOverride = value;
		}

		/// <summary>
		/// If null, all resources are redirected to <see cref="ResourceOverride"/>, if provided.
		/// If not null, only resources matching this regular expression will be redirected to <see cref="ResourceOverride"/>, if provided.
		/// </summary>
		public string ResourceOverrideFilter
		{
			get { return this.resourceOverrideFilter?.ToString(); }
			set
			{
				if (value is null)
					this.resourceOverrideFilter = null;
				else
					this.resourceOverrideFilter = new Regex(value, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
			}
		}

		/// <summary>
		/// Reference to login-auditor to help remove malicious users from the server.
		/// </summary>
		public ILoginAuditor LoginAuditor
		{
			get => this.loginAuditor;
			set
			{
				if (this.loginAuditor is null || this.loginAuditor == value)
					this.loginAuditor = value;
				else
					throw new InvalidOperationException("Login Auditor already set.");
			}
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="Resource">Resource</param>
		/// <returns>Registered resource.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(HttpResource Resource)
		{
			lock (this.resources)
			{
				if (!this.resources.ContainsKey(Resource.ResourceName))
					this.resources[Resource.ResourceName] = Resource;
				else
					throw new Exception("Resource name already registered: " + Resource.ResourceName);
			}

			Resource.AddReference(this);

			return Resource;
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Registered resource.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, true, false, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Registered resource.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, bool Synchronous, params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, Synchronous, false, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="HandlesSubPaths">If sub-paths are handled.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Registered resource.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, bool Synchronous, bool HandlesSubPaths,
			params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, Synchronous, HandlesSubPaths, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="HandlesSubPaths">If sub-paths are handled.</param>
		/// <param name="UserSessions">If the resource uses user sessions.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Reference to generated HTTP resource object.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, bool Synchronous, bool HandlesSubPaths,
			bool UserSessions, params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(new HttpGetDelegateResource(ResourceName, GET, Synchronous, HandlesSubPaths, UserSessions, AuthenticationSchemes));
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="POST">PSOT method handler.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Reference to generated HTTP resource object.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, HttpMethodHandler POST, params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, POST, true, false, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="POST">PSOT method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Reference to generated HTTP resource object.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, HttpMethodHandler POST, bool Synchronous,
			params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, POST, Synchronous, false, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="POST">PSOT method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="HandlesSubPaths">If sub-paths are handled.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Reference to generated HTTP resource object.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, HttpMethodHandler POST, bool Synchronous, bool HandlesSubPaths,
			params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(ResourceName, GET, POST, Synchronous, HandlesSubPaths, false, AuthenticationSchemes);
		}

		/// <summary>
		/// Registers a resource with the server.
		/// </summary>
		/// <param name="ResourceName">Resource Name.</param>
		/// <param name="GET">GET method handler.</param>
		/// <param name="POST">PSOT method handler.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="HandlesSubPaths">If sub-paths are handled.</param>
		/// <param name="UserSessions">If the resource uses user sessions.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		/// <returns>Reference to generated HTTP resource object.</returns>
		/// <exception cref="Exception">If a resource with the same resource name is already registered.</exception>
		public HttpResource Register(string ResourceName, HttpMethodHandler GET, HttpMethodHandler POST, bool Synchronous, bool HandlesSubPaths,
			bool UserSessions, params HttpAuthenticationScheme[] AuthenticationSchemes)
		{
			return this.Register(new HttpGetPostDelegateResource(ResourceName, GET, POST, Synchronous, HandlesSubPaths, UserSessions, AuthenticationSchemes));
		}

		/// <summary>
		/// Unregisters a resource from the server.
		/// </summary>
		/// <param name="Resource">Resource to unregister.</param>
		/// <returns>If the resource was found and removed.</returns>
		public bool Unregister(HttpResource Resource)
		{
			if (Resource is null)
				return false;

			lock (this.resources)
			{
				if (this.resources.TryGetValue(Resource.ResourceName, out HttpResource Resource2) && Resource2 == Resource)
					this.resources.Remove(Resource.ResourceName);
				else
					return false;
			}

			Resource.RemoveReference(this);

			return true;
		}

		/// <summary>
		/// Tries to get a resource from the server.
		/// </summary>
		/// <param name="ResourceName">Full resource name.</param>
		/// <param name="Resource">Resource matching the full resource name.</param>
		/// <param name="SubPath">Trailing end of full resource name, relative to the best resource that was found.</param>
		/// <returns>If a resource was found matching the full resource name.</returns>
		public bool TryGetResource(string ResourceName, out HttpResource Resource, out string SubPath)
		{
			return this.TryGetResource(ResourceName, true, out Resource, out SubPath);
		}

		/// <summary>
		/// Tries to get a resource from the server.
		/// </summary>
		/// <param name="ResourceName">Full resource name.</param>
		/// <param name="PermitResourceOverride">If resource overrides should be considered.</param>
		/// <param name="Resource">Resource matching the full resource name.</param>
		/// <param name="SubPath">Trailing end of full resource name, relative to the best resource that was found.</param>
		/// <returns>If a resource was found matching the full resource name.</returns>
		public bool TryGetResource(string ResourceName, bool PermitResourceOverride, out HttpResource Resource, out string SubPath)
		{
			int i;

			if (PermitResourceOverride && !string.IsNullOrEmpty(this.resourceOverride))
			{
				if (this.resourceOverrideFilter is null || this.resourceOverrideFilter.IsMatch(ResourceName))
					ResourceName = this.resourceOverride;
			}

			SubPath = string.Empty;

			lock (this.resources)
			{
				while (true)
				{
					if (this.resources.TryGetValue(ResourceName, out Resource))
					{
						if (Resource.HandlesSubPaths || string.IsNullOrEmpty(SubPath))
							return true;
					}

					i = ResourceName.LastIndexOf('/');
					if (i < 0)
						break;

					SubPath = ResourceName.Substring(i) + SubPath;
					ResourceName = ResourceName.Substring(0, i);
				}
			}

			Resource = null;

			return false;
		}

		internal string CheckResourceOverride(string ResourceName)
		{
			if (!string.IsNullOrEmpty(this.resourceOverride))
			{
				if (this.resourceOverrideFilter is null || this.resourceOverrideFilter.IsMatch(ResourceName))
					return this.resourceOverride;
			}

			return ResourceName;
		}

		/// <summary>
		/// Gets registered resources of a specific type.
		/// </summary>
		/// <typeparam name="T">Type of resource to get.</typeparam>
		/// <returns>Registered resources of type <typeparamref name="T"/>.</returns>
		public T[] GetRegisteredResources<T>()
			where T : HttpResource
		{
			List<T> Result = new List<T>();

			lock (this.resources)
			{
				foreach (HttpResource Resource in this.resources.Values)
				{
					if (Resource is T TypedResource)
						Result.Add(TypedResource);
				}
			}

			return Result.ToArray();
		}

		#endregion

		#region Sessions

		/// <summary>
		/// Session timeout. Default is 20 minutes.
		/// </summary>
		public TimeSpan SessionTimeout
		{
			get => this.sessionTimeout;

			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("The session timeout must be positive.", nameof(value));

				this.sessionTimeout = value;
				this.sessions.MaxTimeUnused = value;
			}
		}

		/// <summary>
		/// Gets the set of session states corresponing to a given session ID. If no such session is known, a new is created.
		/// </summary>
		/// <param name="SessionId">Session ID</param>
		/// <returns>Session states.</returns>
		public Variables GetSession(string SessionId)
		{
			return this.GetSession(SessionId, true);
		}

		/// <summary>
		/// Gets the set of session states corresponing to a given session ID. If no such session is known, a new is created.
		/// </summary>
		/// <param name="SessionId">Session ID</param>
		/// <param name="CreateIfNotFound">If a sesion should be created if not found.</param>
		/// <returns>Session states, or null if not found and not crerated.</returns>
		public Variables GetSession(string SessionId, bool CreateIfNotFound)
		{
			if (this.sessions is null)
				return null;

			if (this.sessions.TryGetValue(SessionId, out Variables Result))
				return Result;

			if (CreateIfNotFound)
			{
				Result = CreateVariables();
				this.sessions.Add(SessionId, Result);
				return Result;
			}
			else
				return null;
		}

		/// <summary>
		/// Creates a new collection of variables, that contains access to the global set of variables.
		/// </summary>
		/// <returns>Variables collection.</returns>
		public static Variables CreateVariables()
		{
			return new Variables()
			{
				{ "Global", globalVariables }
			};
		}

		internal Variables SetSession(string SessionId, Variables Variables)
		{
			if (this.sessions.TryGetValue(SessionId, out Variables Variables2))
				return Variables2;
			else
			{
				this.sessions[SessionId] = Variables;
				return Variables;
			}
		}


		private async Task Sessions_Removed(object Sender, CacheItemEventArgs<string, Variables> e)
		{
			CacheItemEventHandler<string, Variables> h = this.SessionRemoved;
			if (!(h is null))
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a session has been closed.
		/// </summary>
		public event CacheItemEventHandler<string, Variables> SessionRemoved = null;

		/// <summary>
		/// Event raised before sending an error back to a client. Allows the server to prepare custom error pages.
		/// </summary>
		public event EventHandlerAsync<CustomErrorEventArgs> CustomError = null;

		internal bool HasCustomErrors => !(this.CustomError is null);

		internal Task CustomizeError(CustomErrorEventArgs e)
		{
			return this.CustomError.Raise(this, e);
		}

		#endregion

		#region Statistics

		/// <summary>
		/// Call this method when data has been received.
		/// </summary>
		/// <param name="NrRead">Number of bytes received.</param>
		internal void DataReceived(int NrRead)
		{
			lock (this.statSynch)
			{
				this.nrBytesRx += NrRead;
			}
		}

		/// <summary>
		/// Call this method when data has been written back to a client.
		/// </summary>
		/// <param name="NrWritten">Number of bytes transmitted.</param>
		internal void DataTransmitted(int NrWritten)
		{
			lock (this.statSynch)
			{
				this.nrBytesTx += NrWritten;
			}
		}

		/// <summary>
		/// Registers an incoming request.
		/// 
		/// Note: Each call to <see cref="RequestReceived"/> must be followed by a call to
		/// <see cref="RequestResponded"/>.
		/// </summary>
		/// <param name="Request">Request object.</param>
		/// <param name="ClientAddress">Address of client, from where the request was received.</param>
		/// <param name="Resource">Matching resource, if found, or null, if not found.</param>
		/// <param name="SubPath">Sub-path of request.</param>
		public void RequestReceived(HttpRequest Request, string ClientAddress, HttpResource Resource, string SubPath)
		{
			if (Request is null)
				return;

			HttpFieldUserAgent UserAgent;
			HttpFieldFrom From;

			lock (this.statSynch)
			{
				this.nrCalls++;

				this.IncLocked(Request.Header.Method, this.callsPerMethod);

				if (!(Resource is null))
					this.IncLocked(Resource.ResourceName, this.callsPerResource);

				if (!((UserAgent = Request.Header.UserAgent) is null))
					this.IncLocked(UserAgent.Value, this.callsPerUserAgent);

				if (!((From = Request.Header.From) is null))
					this.IncLocked(From.Value, this.callsPerFrom);
				else
				{
					string s = Request.RemoteEndPoint;
					int i = s.LastIndexOf(':');
					if (i > 0)
						s = s.Substring(0, i);

					this.IncLocked(s, this.callsPerFrom);
				}
			}

			RequestInfo Info = new RequestInfo()
			{
				ClientAddress = ClientAddress,
				Resource = Resource,
				SubPath = SubPath,
				ResourceStr = Request.Header.Resource,
				Method = Request.Header.Method
			};

			this.currentRequests?.Add(Request, Info);
		}

		private void IncLocked(string Key, Dictionary<string, Statistic> Stat)
		{
			if (!Stat.TryGetValue(Key, out Statistic Rec))
			{
				Rec = new Statistic(1);
				Stat[Key] = Rec;
			}
			else
				Rec.Inc();
		}

		/// <summary>
		/// Gets communication statistics since last call.
		/// </summary>
		/// <returns>Communication statistics.</returns>
		public CommunicationStatistics GetCommunicationStatisticsSinceLast()
		{
			CommunicationStatistics Result;
			DateTime TP = DateTime.Now;

			lock (this.statSynch)
			{
				Result = new CommunicationStatistics()
				{
					CallsPerMethod = this.callsPerMethod,
					CallsPerUserAgent = this.callsPerUserAgent,
					CallsPerFrom = this.callsPerFrom,
					CallsPerResource = this.callsPerResource,
					LastStat = this.lastStat,
					CurrentStat = TP,
					NrBytesRx = this.nrBytesRx,
					NrBytesTx = this.nrBytesTx,
					NrCalls = this.nrCalls
				};

				this.callsPerMethod = new Dictionary<string, Statistic>();
				this.callsPerUserAgent = new Dictionary<string, Statistic>();
				this.callsPerFrom = new Dictionary<string, Statistic>();
				this.callsPerResource = new Dictionary<string, Statistic>();
				this.lastStat = TP;
				this.nrBytesRx = 0;
				this.nrBytesTx = 0;
				this.nrCalls = 0;
			}

			return Result;
		}

		private class RequestInfo
		{
			public DateTime Received = DateTime.Now;
			public HttpResource Resource;
			public string ClientAddress;
			public string SubPath;
			public string Method;
			public string ResourceStr;
			public int? StatusCode = null;
		}

		/// <summary>
		/// Registers an outgoing response to a requesst.
		/// </summary>
		/// <param name="Request">Original request object.</param>
		/// <param name="StatusCode">Status code.</param>
		public void RequestResponded(HttpRequest Request, int StatusCode)
		{
			if (!(this.currentRequests is null) && !(Request is null))
			{
				if (this.currentRequests.TryGetValue(Request, out RequestInfo Info))
				{
					Info.StatusCode = StatusCode;
					this.currentRequests.Remove(Request);
				}
				else if (StatusCode != 0)
				{
					Log.Warning("Late response.", Request.Header.Resource,
						new KeyValuePair<string, object>("Response", StatusCode),
						new KeyValuePair<string, object>("Method", Request.Header.Method));
				}
			}
		}

		/// <summary>
		/// Keeps the request alive, without timing out
		/// </summary>
		/// <param name="Request">Request.</param>
		/// <returns>If request found among current requests.</returns>
		public bool PingRequest(HttpRequest Request)
		{
			return this.currentRequests?.TryGetValue(Request, out RequestInfo _) == false;
		}

		private Task CurrentRequests_Removed(object Sender, CacheItemEventArgs<HttpRequest, RequestInfo> e)
		{
			RequestInfo Info = e.Value;

			if (e.Reason != RemovedReason.Manual)
			{
				Log.Warning("HTTP request timed out.", Info.ResourceStr,
					new KeyValuePair<string, object>("From", Info.ClientAddress),
					new KeyValuePair<string, object>("Method", Info.Method));
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Request timeout. Default is 2 minutes.
		/// </summary>
		public TimeSpan RequestTimeout
		{
			get => this.requestTimeout;

			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("The request timeout must be positive.", nameof(value));

				this.requestTimeout = value;

				if (!(this.currentRequests is null))
					this.currentRequests.MaxTimeUnused = value;
			}
		}

		#endregion

		#region GET

		/// <summary>
		/// Performs an internal GET operation.
		/// </summary>
		/// <param name="LocalUrl">Local URL</param>
		/// <param name="Session">Session variables, if available, or null if not.</param>
		/// <returns>Status Code, Content-Type and binary representation of resource, if available.</returns>
		public async Task<Tuple<int, string, byte[]>> GET(string LocalUrl, Variables Session)
		{
			string ResourceName = LocalUrl;
			int i = ResourceName.IndexOf('?');
			if (i >= 0)
				ResourceName = ResourceName.Substring(0, i);

			if (this.TryGetResource(ResourceName, true, out HttpResource Resource, out string SubPath) &&
				Resource is IHttpGetMethod GetMethod)
			{
				using (MemoryStream ms = new MemoryStream())
				{
					HttpRequest Request = new HttpRequest(this,
						new HttpRequestHeader("GET " + LocalUrl + " HTTP/1.1", this.vanityResources, "http"), null, "unknown", "unknown")
					{
						Session = Session,
						SubPath = SubPath,
						Resource = Resource
					};

					InternalTransfer InternalTransfer = new InternalTransfer(ms);
					HttpResponse Response = new HttpResponse(InternalTransfer, this, Request);

					this.RequestReceived(Request, string.Empty, Resource, SubPath);
					await GetMethod.GET(Request, Response);

					await InternalTransfer.WaitUntilSent(10000);

					return new Tuple<int, string, byte[]>(Response.StatusCode, Response.ContentType, ms.ToArray());
				}
			}
			else
				return new Tuple<int, string, byte[]>(NotFoundException.Code, NotFoundException.StatusMessage, null);
		}

		/// <summary>
		/// Tries to get the full path of a file-based resource.
		/// </summary>
		/// <param name="LocalUrl">Local URL</param>
		/// <param name="FileName">File name, if found.</param>
		/// <returns>If the resource points to a file.</returns>
		public bool TryGetFileName(string LocalUrl, out string FileName)
		{
			return this.TryGetFileName(LocalUrl, true, out FileName);
		}

		/// <summary>
		/// Tries to get the full path of a file-based resource.
		/// </summary>
		/// <param name="LocalUrl">Local URL</param>
		/// <param name="MustExist">If file must exist.</param>
		/// <param name="FileName">File name, if found.</param>
		/// <returns>If the resource points to a file.</returns>
		public bool TryGetFileName(string LocalUrl, bool MustExist, out string FileName)
		{
			string ResourceName = LocalUrl;
			int i = ResourceName.IndexOf('?');
			if (i >= 0)
				ResourceName = ResourceName.Substring(0, i);

			if (this.TryGetResource(ResourceName, false, out HttpResource Resource, out string SubPath) &&
				Resource is HttpFolderResource Folder)
			{
				this.vanityResources.CheckVanityResource(ref SubPath);

				FileName = Folder.GetFullPath(SubPath, null, false, MustExist, out bool Exists);

				return Exists;
			}
			else
			{
				FileName = null;
				return false;
			}
		}

		#endregion

		#region Vanity resource names

		/// <summary>
		/// Registers a vanity resource.
		/// </summary>
		/// <param name="RegexPattern">Regular expression used to match incoming requests.</param>
		/// <param name="MapTo">Resources matching <paramref name="RegexPattern"/> will be mapped to resources of this type.
		/// Named group values found using the regular expression can be used in the map, between curly braces { and }.</param>
		public void RegisterVanityResource(string RegexPattern, string MapTo)
		{
			this.vanityResources.RegisterVanityResource(RegexPattern, MapTo);
		}

		/// <summary>
		/// Registers a vanity resource.
		/// </summary>
		/// <param name="RegexPattern">Regular expression used to match incoming requests.</param>
		/// <param name="MapTo">Resources matching <paramref name="RegexPattern"/> will be mapped to resources of this type.
		/// Named group values found using the regular expression can be used in the map, between curly braces { and }.</param>
		/// <param name="Tag">Tags the expression with an object. This tag can be used when
		/// unregistering all vanity resources tagged with the given tag.</param>
		public void RegisterVanityResource(string RegexPattern, string MapTo, object Tag)
		{
			this.vanityResources.RegisterVanityResource(RegexPattern, MapTo, Tag);
		}

		/// <summary>
		/// Unregisters a vanity resource.
		/// </summary>
		/// <param name="RegexPattern">Regular expression used to match incoming requests.</param>
		/// <returns>If a vanity resource matching the parameters was found, and consequently removed.</returns>
		public bool UnregisterVanityResource(string RegexPattern)
		{
			return this.vanityResources.UnregisterVanityResource(RegexPattern);
		}

		/// <summary>
		/// Unregisters vanity resources tagged with a specific object.
		/// </summary>
		/// <param name="Tag">Remove all vanity resources tagged with this object.</param>
		/// <returns>Number of vanity resources removed.</returns>
		public int UnregisterVanityResources(object Tag)
		{
			return this.vanityResources.UnregisterVanityResources(Tag);
		}

		/// <summary>
		/// Checks if a resource name is a vanity resource name. If so, it is expanded to the true resource name.
		/// </summary>
		/// <param name="ResourceName">Resource name.</param>
		/// <returns>If resource was a vanity resource, and has been updated to reflect the true resource name.</returns>
		public bool CheckVanityResource(ref string ResourceName)
		{
			return this.vanityResources.CheckVanityResource(ref ResourceName);
		}

		/// <summary>
		/// Vanity resources.
		/// </summary>
		public VanityResources VanityResources => this.vanityResources;

		/// <summary>
		/// Checks if a resource name needs to be mapped to an alternative resource.
		/// </summary>
		/// <param name="ResourceName">Resource name.</param>
		/// <returns>If resource is mapped, and has been updated to reflect the true resource name.</returns>
		public bool CheckResource(ref string ResourceName)
		{
			return this.vanityResources.CheckVanityResource(ref ResourceName);
		}

		#endregion

		// TODO: Web Service resources
	}
}
