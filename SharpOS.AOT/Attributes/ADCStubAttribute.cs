// 
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	William Lahti <xfurious@gmail.com>
//
// Licensed under the terms of the GNU GPL License version 2.
//

using System;
using System.Text;
using SharpOS.AOT;

namespace SharpOS.AOT.Attributes {

	/// <summary>
	/// Methods which carry this attribute act as stubs into
	/// the ADC layer in use at AOT compile-time.
	/// </summary>
	[AttributeUsage (AttributeTargets.Method)]
	public class ADCStubAttribute : Attribute {
	}
	
}
