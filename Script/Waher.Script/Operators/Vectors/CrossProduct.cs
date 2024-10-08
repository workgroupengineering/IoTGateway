﻿using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects.VectorSpaces;

namespace Waher.Script.Operators.Vectors
{
	/// <summary>
	/// Cross-product operator.
	/// </summary>
	public class CrossProduct : BinaryVectorOperator
	{
		/// <summary>
		/// Cross-product operator.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public CrossProduct(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the operator on vector operands.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override IElement EvaluateVector(IVector Left, IVector Right, Variables Variables)
		{
			if (Left.Dimension != 3 || Right.Dimension != 3)
				throw new ScriptRuntimeException("Cross product works on vectors of dimesion 3.", this);

			if (Left is DoubleVector dv1 && Right is DoubleVector dv2)
			{
				double[] d1 = dv1.Values;
				double[] d2 = dv2.Values;

				return new DoubleVector(new double[] { d1[1] * d2[2] - d1[2] * d2[1], d1[2] * d2[0] - d1[0] * d2[2], d1[0] * d2[1] - d1[1] * d2[0] });
			}

			IElement[] v1 = new IElement[3];
			Left.VectorElements.CopyTo(v1, 0);

			IElement[] v2 = new IElement[3];
			Right.VectorElements.CopyTo(v2, 0);

			return VectorDefinition.Encapsulate(new IElement[]
			{
				Arithmetics.Subtract.EvaluateSubtraction(
					Arithmetics.Multiply.EvaluateMultiplication(v1[1], v2[2], this),
					Arithmetics.Multiply.EvaluateMultiplication(v1[2], v2[1], this), this),
				Arithmetics.Subtract.EvaluateSubtraction(
					Arithmetics.Multiply.EvaluateMultiplication(v1[2], v2[0], this), 
					Arithmetics.Multiply.EvaluateMultiplication(v1[0], v2[2], this), this),
				Arithmetics.Subtract.EvaluateSubtraction(
					Arithmetics.Multiply.EvaluateMultiplication(v1[0], v2[1], this), 
					Arithmetics.Multiply.EvaluateMultiplication(v1[1], v2[0], this), this)
			}, false, this);
		}

	}
}
