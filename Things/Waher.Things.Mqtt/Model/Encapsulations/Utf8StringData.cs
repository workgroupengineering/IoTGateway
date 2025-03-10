﻿using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking;
using Waher.Networking.MQTT;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Things.ControlParameters;
using Waher.Things.SensorData;

namespace Waher.Things.Mqtt.Model.Encapsulations
{
    /// <summary>
    /// Represents an MQTT topic with string data.
    /// </summary>
    public class Utf8StringData : MqttData
	{
		private string value;

		/// <summary>
		/// Represents an MQTT topic with string data.
		/// </summary>
		public Utf8StringData()
			: base()
		{
		}

		/// <summary>
		/// Represents an MQTT topic with string data.
		/// </summary>
		/// <param name="Topic">MQTT Topic</param>
		/// <param name="Value">Data value</param>
		public Utf8StringData(MqttTopic Topic, string Value)
			: base(Topic)
		{
			this.value = Value;
		}

		/// <summary>
		/// Called when new data has been published.
		/// </summary>
		/// <param name="Topic">MQTT Topic Node. If null, synchronous result should be returned.</param>
		/// <param name="Content">Published MQTT Content</param>
		/// <returns>Data processing result</returns>
		public override Task<DataProcessingResult> DataReported(MqttTopic Topic, MqttContent Content)
		{
			string s = Content.DataString;
			byte[] Bin = Encoding.UTF8.GetBytes(s);
			byte[] Data = Content.Data;
			int i, c = Bin.Length;

			if (c != Data.Length)
				return Task.FromResult(DataProcessingResult.Incompatible);

			for (i = 0; i < c; i++)
			{
				if (Bin[i] != Data[i])
					return Task.FromResult(DataProcessingResult.Incompatible);
			}

			this.value = s;
			this.Timestamp = DateTime.UtcNow;
			this.QoS = Content.Header.QualityOfService;
			this.Retain = Content.Header.Retain;

			return Task.FromResult(DataProcessingResult.ProcessedNewMomentaryValues);
		}

		/// <summary>
		/// Type name representing data.
		/// </summary>
		public override Task<string> GetTypeName(Language Language)
		{
			return Language.GetStringAsync(typeof(MqttTopicNode), 40, "String");
		}

		/// <summary>
		/// Starts a readout of the data.
		/// </summary>
		/// <param name="ThingReference">Thing reference.</param>
		/// <param name="Request">Sensor-data request</param>
		/// <param name="Prefix">Field-name prefix.</param>
		/// <param name="Last">If the last readout call for request.</param>
		public override Task StartReadout(ThingReference ThingReference, ISensorReadout Request, string Prefix, bool Last)
		{
			Request.ReportFields(Last, new StringField(ThingReference, this.Timestamp, this.Append(Prefix, "Value"),
				this.value, FieldType.Momentary, FieldQoS.AutomaticReadout));
			return Task.CompletedTask;
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override bool IsControllable => true;

		/// <summary>
		/// TODO
		/// </summary>
		public override ControlParameter[] GetControlParameters()
		{
			return new ControlParameter[]
			{
				new StringControlParameter("Value", "Publish", "Value:", "String value of topic.",
					(n) => Task.FromResult<string>(this.value),
					(n, v) =>
					{
						this.value = v;
						this.Topic.MqttClient.PUBLISH(this.Topic.FullTopic, this.QoS, this.Retain, Encoding.UTF8.GetBytes(v));
						return Task.CompletedTask;
					})
			};
		}

		/// <summary>
		/// Outputs the parsed data to the sniffer.
		/// </summary>
		public override void SnifferOutput(ICommunicationLayer Output)
		{
			this.Information(Output, this.value.ToString());
		}

		/// <summary>
		/// Default support.
		/// </summary>
		public override Grade DefaultSupport => Grade.Ok;

		/// <summary>
		/// Creates a new instance of the data.
		/// </summary>
		/// <param name="Topic">MQTT Topic</param>
		/// <param name="Content">MQTT Content</param>
		/// <returns>New object instance.</returns>
		public override IMqttData CreateNew(MqttTopic Topic, MqttContent Content)
		{
			IMqttData Result = new Utf8StringData(Topic, default);
			Result.DataReported(Topic, Content);
			return Result;
		}
	}
}
