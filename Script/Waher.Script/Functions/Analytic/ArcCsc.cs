﻿using System;
using System.Numerics;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Arithmetics;

namespace Waher.Script.Functions.Analytic
{
	/// <summary>
	/// ArcCsc(x)
	/// </summary>
	public class ArcCsc : FunctionOneScalarVariable, IDifferentiable
	{
		/// <summary>
		/// ArcCsc(x)
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ArcCsc(ScriptNode Argument, int Start, int Length, Expression Expression)
			: base(Argument, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ArcCsc);

		/// <summary>
		/// Optional aliases. If there are no aliases for the function, null is returned.
		/// </summary>
		public override string[] Aliases
		{
			get { return new string[] { "acsc" }; }
		}

		/// <summary>
		/// Differentiates a script node, if possible.
		/// </summary>
		/// <param name="VariableName">Name of variable to differentiate on.</param>
		/// <param name="Variables">Collection of variables.</param>
		/// <returns>Differentiated node.</returns>
		public ScriptNode Differentiate(string VariableName, Variables Variables)
		{
			if (VariableName == this.DefaultVariableName)
			{
				int Start = this.Start;
				int Len = this.Length;
				Expression Exp = this.Expression;

				return this.DifferentiationChainRule(VariableName, Variables, this.Argument,
					new Invert(
						new Negate(
							new Multiply(
								new Scalar.Abs(this.Argument, Start, Len, Exp),
								new Sqrt(
									new Subtract(
										new Square(this.Argument, Start, Len, Exp),
										new ConstantElement(DoubleNumber.OneElement, Start, Len, Exp),
										Start, Len, Exp),
									Start, Len, Exp),
								Start, Len, Exp),
							Start, Len, Exp),
						Start, Len, Exp));
			}
			else
				return new ConstantElement(DoubleNumber.ZeroElement, this.Start, this.Length, this.Expression);
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(double Argument, Variables Variables)
		{
			return new DoubleNumber(Math.Asin(1.0 / Argument));
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(Complex Argument, Variables Variables)
		{
			return new ComplexNumber(Complex.Asin(Complex.Reciprocal(Argument)));
		}
	}
}
