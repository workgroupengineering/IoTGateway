﻿using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Functions.Strings
{
	/// <summary>
	/// StartsWith(String,Substring)
	/// </summary>
	public class StartsWith : FunctionTwoScalarVariables
	{
		/// <summary>
		/// StartsWith(String,Substring)
		/// </summary>
		/// <param name="String">String.</param>
		/// <param name="Substring">Delimiter</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public StartsWith(ScriptNode String, ScriptNode Substring, int Start, int Length, Expression Expression)
			: base(String, Substring, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(StartsWith);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "String", "Substring" };

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument1">String.</param>
		/// <param name="Argument2">Delimiter</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(string Argument1, string Argument2, Variables Variables)
		{
			return Argument1.StartsWith(Argument2) ? BooleanValue.True : BooleanValue.False;
		}
	}
}
