// 
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Mircea-Cristian Racasan <darx_kies@gmx.net>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

using System;
using System.Collections.Generic;
using System.Text;
using SharpOS.AOT.IR.Operands;
using Mono.Cecil;


namespace SharpOS.AOT.IR.Instructions {
	/// <summary>
	/// 
	/// </summary>
	public class Ldfld : Instruction {
		/// <summary>
		/// Initializes a new instance of the <see cref="Ldfld"/> class.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <param name="value">The value.</param>
		public Ldfld (Register result, FieldOperand value)
			: base ("Ldfld", result, new Operand [] { value })
		{
		}

		/// <summary>
		/// Processes the specified method.
		/// </summary>
		/// <param name="method">The method.</param>
		public override void Process (Method method)
		{
			base.LdfldProcess (method);
		}
	}
}