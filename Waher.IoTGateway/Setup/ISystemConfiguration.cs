﻿using System;
using System.Threading.Tasks;
using Waher.Networking.HTTP;

namespace Waher.IoTGateway.Setup
{
	/// <summary>
	/// Interface for system configurations. The gateway will scan all module for system configuration classes.
	/// If it finds configuration objects that have not been properly configured, it redirects request to the appropriate
	/// resource for the configuration to be performed.
	/// </summary>
    public interface ISystemConfiguration
    {
		/// <summary>
		/// If the configuration is complete.
		/// </summary>
		bool Complete
		{
			get;
			set;
		}

		/// <summary>
		/// When the object was created.
		/// </summary>
		DateTime Created
		{
			get;
			set;
		}

		/// <summary>
		/// When the object was updated.
		/// </summary>
		DateTime Updated
		{
			get;
			set;
		}

		/// <summary>
		/// When the configuration was completed.
		/// </summary>
		DateTime Completed
		{
			get;
			set;
		}

		/// <summary>
		/// Resource to be redirected to, to perform the configuration.
		/// </summary>
		string Resource
		{
			get;
		}

		/// <summary>
		/// Priority of the setting. Configurations are sorted in ascending order.
		/// </summary>
		int Priority
		{
			get;
		}

		/// <summary>
		/// Sets the static instance of the configuration.
		/// </summary>
		/// <param name="Configuration">Configuration object</param>
		void SetStaticInstance(ISystemConfiguration Configuration);

		/// <summary>
		/// Is called during startup to configure the system.
		/// </summary>
		Task ConfigureSystem();

		/// <summary>
		/// Initializes the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		Task InitSetup(HttpServer WebServer);

		/// <summary>
		/// Unregisters the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		Task UnregisterSetup(HttpServer WebServer);

		/// <summary>
		/// Waits for the user to provide configuration.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		/// <returns>If all system configuration objects must be reloaded from the database.</returns>
		Task<bool> SetupConfiguration(HttpServer WebServer);

		/// <summary>
		/// Cleans up after configuration has been performed.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		Task CleanupAfterConfiguration(HttpServer WebServer);

		/// <summary>
		/// Simplified configuration by configuring simple default values.
		/// </summary>
		/// <returns>If the configuration was changed, and can be considered completed.</returns>
		Task<bool> SimplifiedConfiguration();

		/// <summary>
		/// Environment configuration by configuring values available in environment variables.
		/// </summary>
		/// <returns>If the configuration was changed, and can be considered completed.</returns>
		Task<bool> EnvironmentConfiguration();

	}
}
