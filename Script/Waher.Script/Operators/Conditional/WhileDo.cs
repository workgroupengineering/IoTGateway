﻿using System;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Conditional
{
    /// <summary>
    /// WHILE-DO operator.
    /// </summary>
    public class WhileDo : BinaryOperator
    {
        /// <summary>
        /// WHILE-DO operator.
        /// </summary>
        /// <param name="Condition">Condition.</param>
        /// <param name="Statement">Statement.</param>
        /// <param name="Start">Start position in script expression.</param>
        /// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
        public WhileDo(ScriptNode Condition, ScriptNode Statement, int Start, int Length, Expression Expression)
            : base(Condition, Statement, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override IElement Evaluate(Variables Variables)
        {
            IElement Last = null;
            BooleanValue Condition;

            Condition = this.left.Evaluate(Variables) as BooleanValue;
            if (Condition is null)
                throw new ScriptRuntimeException("Condition must evaluate to a boolean value.", this);

            while (Condition.Value)
            {
                Last = this.right.Evaluate(Variables);

                Condition = this.left.Evaluate(Variables) as BooleanValue;
                if (Condition is null)
                    throw new ScriptRuntimeException("Condition must evaluate to a boolean value.", this);
            }

            if (Last is null)
                return ObjectValue.Null;
            else
                return Last;
        }

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override async Task<IElement> EvaluateAsync(Variables Variables)
        {
            if (!this.isAsync)
                return this.Evaluate(Variables);

            IElement Last = null;
            BooleanValue Condition;

            Condition = await this.left.EvaluateAsync(Variables) as BooleanValue;
            if (Condition is null)
                throw new ScriptRuntimeException("Condition must evaluate to a boolean value.", this);

            while (Condition.Value)
            {
                Last = await this.right.EvaluateAsync(Variables);

                Condition = await this.left.EvaluateAsync(Variables) as BooleanValue;
                if (Condition is null)
                    throw new ScriptRuntimeException("Condition must evaluate to a boolean value.", this);
            }

            if (Last is null)
                return ObjectValue.Null;
            else
                return Last;
        }
    }
}
