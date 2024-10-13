﻿using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Things.Ieee1451.Ieee1451_0.Messages;
using Waher.Things.Mqtt;
using Waher.Things.Mqtt.Model;

namespace Waher.Things.Ieee1451.Ieee1451_1_6
{
	/// <summary>
	/// Topic node representing an IEEE 1451.0 Channel.
	/// </summary>
	public class MqttChannelTopicNode : MqttTopicNode
	{
		/// <summary>
		/// Topic node representing an IEEE 1451.0 Channel.
		/// </summary>
		public MqttChannelTopicNode()
		{
		}

		/// <summary>
		/// Local ID
		/// </summary>
		public override string LocalId => this.NodeId;

		/// <summary>
		/// Diaplayable type name for node.
		/// </summary>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(Ieee1451Parser), 7, "IEEE 1451.0 Channel");
		}

		/// <summary>
		/// If the node accepts a given parent.
		/// </summary>
		public override Task<bool> AcceptsParentAsync(INode Parent)
		{
			return Task.FromResult(Parent is MqttTimTopicNode);
		}

		/// <summary>
		/// If the node accepts a given child.
		/// </summary>
		public override Task<bool> AcceptsChildAsync(INode Child)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// How well the topic node supports an MQTT topic
		/// </summary>
		/// <param name="Topic">Topic being processed.</param>
		/// <returns>How well the node supports a given topic, from the segment index presented.</returns>
		public override Grade Supports(MqttTopicRepresentation Topic)
		{
			if (Topic.SegmentIndex > 0 &&
				Topic.CurrentParentTopic.Node is MqttTimTopicNode &&
				int.TryParse(Topic.CurrentSegment, out _))
			{
				return Grade.Excellent;
			}
			else
				return Grade.NotAtAll;
		}

		/// <summary>
		/// Creates a new node of the same type.
		/// </summary>
		/// <param name="Topic">MQTT Topic being processed.</param>
		/// <returns>New node instance.</returns>
		public override async Task<IMqttTopicNode> CreateNew(MqttTopicRepresentation Topic)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("Channel-");
			sb.Append(Topic.Segments[Topic.SegmentIndex - 1]);
			sb.Append('#');
			sb.Append(Topic.CurrentSegment);

			return new MqttChannelTopicNode()
			{
				NodeId = await GetUniqueNodeId(sb.ToString()),
				LocalTopic = Topic.CurrentSegment
			};
		}

		/// <summary>
		/// A transducer access command has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TransducerAccessCommand(TransducerAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A transducer access reply has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task TransducerAccessReply(TransducerAccessMessage Message)
		{
			MqttTopic Topic = await this.GetTopic();
			if (Topic.Data is ChannelData Data)
				Data.DataReported(Message);
			else
				Topic.SetData(new ChannelData(Message));
		}

		/// <summary>
		/// A transducer access announcement has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TransducerAccessAnnouncement(TransducerAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A transducer access notification has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TransducerAccessNotification(TransducerAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A transducer access callback has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TransducerAccessCallback(TransducerAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A TEDS access command has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TedsAccessCommand(TedsAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A TEDS access reply has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task TedsAccessReply(TedsAccessMessage Message)
		{
			MqttTopic Topic = await this.GetTopic();
			if (Topic.Data is ChannelData Data)
				Data.DataReported(Message);
			else
				Topic.SetData(new ChannelData(Message));
		}

		/// <summary>
		/// A TEDS access announcement has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TedsAccessAnnouncement(TedsAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A TEDS access notification has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TedsAccessNotification(TedsAccessMessage Message)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A TEDS access callback has been received.
		/// </summary>
		/// <param name="Message">Message</param>
		public Task TedsAccessCallback(TedsAccessMessage Message)
		{
			return Task.CompletedTask;
		}

	}
}