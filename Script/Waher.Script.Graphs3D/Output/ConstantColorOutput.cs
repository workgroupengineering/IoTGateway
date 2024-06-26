﻿using System;
using System.Text;
using Waher.Runtime.Inventory;
using Waher.Script.Output;

namespace Waher.Script.Graphs3D.Output
{
	/// <summary>
	/// Converts values of type <see cref="ConstantColor"/> to expression strings.
	/// </summary>
	public class ConstantColorOutput : ICustomStringOutput
	{
		/// <summary>
		/// If the interface understands objects such as <paramref name="Object"/>.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>How well objects of this type are supported.</returns>
		public Grade Supports(Type Object) => Object == typeof(ConstantColor) ? Grade.Ok : Grade.NotAtAll;

		/// <summary>
		/// Gets a string representing a value.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>Expression string.</returns>
		public string GetString(object Value)
		{
			ConstantColor Shader = (ConstantColor)Value;
			StringBuilder sb = new StringBuilder();

			sb.Append("ConstantColor(");
			sb.Append(Expression.ToString(Shader.Color));
			sb.Append(')');

			return sb.ToString();
		}
	}
}
