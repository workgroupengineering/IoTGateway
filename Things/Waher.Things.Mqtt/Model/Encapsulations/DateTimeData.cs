﻿using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Content.Xml;
using Waher.Networking.MQTT;
using Waher.Networking.Sniffers;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Things.ControlParameters;
using Waher.Things.SensorData;

namespace Waher.Things.Mqtt.Model.Encapsulations
{
	/// <summary>
	/// Represents an MQTT topic with Date &amp; Time data.
	/// </summary>
	public class DateTimeData : MqttData
	{
		private DateTime value;

		/// <summary>
		/// Represents an MQTT topic with Date &amp; Time data.
		/// </summary>
		public DateTimeData()
			: base()
		{
		}

		/// <summary>
		/// Represents an MQTT topic with Date &amp; Time data.
		/// </summary>
		public DateTimeData(MqttTopic Topic, DateTime Value)
			: base(Topic)
		{
			this.value = Value;
		}

		/// <summary>
		/// Called when new data has been published.
		/// </summary>
		public override bool DataReported(MqttContent Content)
		{
			if (DateTime.TryParse(Content.DataString, out DateTime Value) || XML.TryParse(Content.DataString, out Value))
			{
				this.value = Value;
				this.Timestamp = DateTime.UtcNow;
				this.QoS = Content.Header.QualityOfService;
				this.Retain = Content.Header.Retain;

				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override Task<string> GetTypeName(Language Language)
		{
			return Language.GetStringAsync(typeof(MqttTopicNode), 33, "Date and Time");
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override void StartReadout(ThingReference ThingReference, ISensorReadout Request, string Prefix, bool Last)
		{
			if (this.value.TimeOfDay == TimeSpan.Zero)
			{
				Request.ReportFields(Last, new DateField(ThingReference, this.Timestamp, this.Append(Prefix, "Value"),
					this.value, FieldType.Momentary, FieldQoS.AutomaticReadout));
			}
			else
			{
				Request.ReportFields(Last, new DateTimeField(ThingReference, this.Timestamp, this.Append(Prefix, "Value"),
					this.value, FieldType.Momentary, FieldQoS.AutomaticReadout));
			}
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
				new DateTimeControlParameter("Value", "Publish", "Value:", "Date & time value of topic.", null, null,
					(n) => Task.FromResult<DateTime?>(this.value),
					(n, v) =>
					{
						this.value = v;
						this.Topic.MqttClient.PUBLISH(this.Topic.FullTopic, this.QoS, this.Retain, Encoding.UTF8.GetBytes(v.ToString()));
						return Task.CompletedTask;
					})
			};
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override void SnifferOutput(ISniffable Output)
		{
			this.Information(Output, this.value.ToString());
		}

		/// <summary>
		/// Default support.
		/// </summary>
		public override Grade DefaultSupport => Grade.Perfect;

		/// <summary>
		/// Creates a new instance of the data.
		/// </summary>
		/// <param name="Topic">MQTT Topic node</param>
		/// <param name="Content">MQTT Content</param>
		/// <returns>New object instance.</returns>
		public override IMqttData CreateNew(MqttTopic Topic, MqttContent Content)
		{
			IMqttData Result = new DateTimeData(Topic, default);
			Result.DataReported(Content);
			return Result;
		}
	}
}
