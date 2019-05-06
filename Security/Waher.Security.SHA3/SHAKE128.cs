﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Security.SHA3
{
	/// <summary>
	/// Implements the SHA3 SHAKE128 extendable-output functions, as defined in 
	/// section 6.2 in the NIST FIPS 202: 
	/// https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.202.pdf
	/// </summary>
	public class SHAKE128 : Keccak
	{
		/// <summary>
		/// Implements the SHA3 SHAKE128 extendable-output functions, as defined in 
		/// section 6.2 in the NIST FIPS 202: 
		/// https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.202.pdf
		/// </summary>
		/// <param name="DigestSize">Digest size.</param>
		public SHAKE128(int DigestSize)
			: base(BitSize.BitSize1600, 256, 0b1111, DigestSize)
		{
		}
	}
}
