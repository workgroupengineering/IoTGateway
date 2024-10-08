﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SkiaSharp;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Graphs.Functions.Colors
{
	/// <summary>
	/// Blends colors `c1` and `c2` together using a blending factor 0&lt;=`p`&lt;=1. Any or both of `c1` and `c2` can be an image.
	/// </summary>
	public class Blend : FunctionMultiVariate
	{
		/// <summary>
		/// Blends colors `c1` and `c2` together using a blending factor 0&lt;=`p`&lt;=1. Any or both of `c1` and `c2` can be an image.
		/// </summary>
		/// <param name="c1">First color, or image.</param>
		/// <param name="c2">Second color, or image.</param>
		/// <param name="p">Blending factor in [0,1].</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Blend(ScriptNode c1, ScriptNode c2, ScriptNode p, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { c1, c2, p }, FunctionMultiVariate.argumentTypes3Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames
		{
			get
			{
				return new string[] { "c1", "c2", "p" };
			}
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName
		{
			get
			{
				return "Blend";
			}
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			object x1 = Arguments[0].AssociatedObjectValue;
			object x2 = Arguments[1].AssociatedObjectValue;
			double p = Expression.ToDouble(Arguments[2].AssociatedObjectValue);
			PixelInformation Img1, Img2;
			SKColor c1, c2;

			if (x1 is SKColor C1)
			{
				c1 = C1;
				Img1 = null;
			}
			else if (x1 is Graph G1)
			{
				Img1 = G1.CreatePixels(Variables);
				c1 = SKColor.Empty;
			}
			else if (x1 is SKImage I1)
			{
				Img1 = PixelInformation.FromImage(I1);
				c1 = SKColor.Empty;
			}
			else
			{
				Img1 = null;
				c1 = Graph.ToColor(x1);
			}

			if (x2 is SKColor C2)
			{
				c2 = C2;
				Img2 = null;
			}
			else if (x2 is Graph G2)
			{
				Img2 = G2.CreatePixels(Variables);
				c2 = SKColor.Empty;
			}
			else if (x2 is SKImage I2)
			{
				Img2 = PixelInformation.FromImage(I2);
				c2 = SKColor.Empty;
			}
			else
			{
				Img2 = null;
				c2 = Graph.ToColor(x2);
			}

			if (Img1 is null && Img2 is null)
				return new ObjectValue(BlendColors(c1, c2, p));
			else if (Img1 is null)
				return new GraphBitmap(Variables, BlendColors(Img2, c1, 1 - p));
			else if (Img2 is null)
				return new GraphBitmap(Variables, BlendColors(Img1, c2, p));
			else
				return new GraphBitmap(Variables, BlendColors(Img1, Img2, p));
		}

		/// <summary>
		/// Blends two colors using a blending factor.
		/// </summary>
		/// <param name="c1">Color 1.</param>
		/// <param name="c2">Color 2.</param>
		/// <param name="p">Blending factor (0=<paramref name="c1"/>, 1=<paramref name="c2"/>).</param>
		/// <returns>Blended color.</returns>
		public static SKColor BlendColors(SKColor c1, SKColor c2, double p)
		{
			int R = (int)(c1.Red * (1 - p) + c2.Red * p + 0.5);
			int G = (int)(c1.Green * (1 - p) + c2.Green * p + 0.5);
			int B = (int)(c1.Blue * (1 - p) + c2.Blue * p + 0.5);
			int A = (int)(c1.Alpha * (1 - p) + c2.Alpha * p + 0.5);

			if (R < 0)
				R = 0;
			else if (R > 255)
				R = 255;

			if (G < 0)
				G = 0;
			else if (G > 255)
				G = 255;

			if (B < 0)
				B = 0;
			else if (B > 255)
				B = 255;

			if (A < 0)
				A = 0;
			else if (A > 255)
				A = 255;

			return new SKColor((byte)R, (byte)G, (byte)B, (byte)A);
		}

		/// <summary>
		/// Blends an image with a fixed color using a blending factor.
		/// </summary>
		/// <param name="Pixels">Image pixels</param>
		/// <param name="Color">Color</param>
		/// <param name="p">Blending factor (0=<paramref name="Pixels"/>, 1=<paramref name="Color"/>).</param>
		/// <returns>Blended image.</returns>
		public static PixelInformation BlendColors(PixelInformation Pixels, SKColor Color, double p)
		{
			PixelInformationRaw Raw = Pixels.GetRaw();
			byte[] Bin = (byte[])Raw.Binary.Clone();
			int i, j, c = Raw.Binary.Length;
			byte R = Color.Red;
			byte G = Color.Green;
			byte B = Color.Blue;
			byte A = Color.Alpha;

			for (i = 0; i < c; i++)
			{
				j = (int)(Bin[i] * (1 - p) + B * p + 0.5);
				if (j < 0)
					Bin[i] = 0;
				else if (j > 255)
					Bin[i] = 255;
				else
					Bin[i] = (byte)j;

				j = (int)(Bin[++i] * (1 - p) + G * p + 0.5);
				if (j < 0)
					Bin[i] = 0;
				else if (j > 255)
					Bin[i] = 255;
				else
					Bin[i] = (byte)j;

				j = (int)(Bin[++i] * (1 - p) + R * p + 0.5);
				if (j < 0)
					Bin[i] = 0;
				else if (j > 255)
					Bin[i] = 255;
				else
					Bin[i] = (byte)j;

				j = (int)(Bin[++i] * (1 - p) + A * p + 0.5);
				if (j < 0)
					Bin[i] = 0;
				else if (j > 255)
					Bin[i] = 255;
				else
					Bin[i] = (byte)j;
			}

			return new PixelInformationRaw(Raw.ColorType, Bin, Raw.Width, Raw.Height, Raw.BytesPerRow);
		}

		/// <summary>
		/// Blends two images of the same size using a blending factor.
		/// </summary>
		/// <param name="Image1">Image 1</param>
		/// <param name="Image2">Image 2</param>
		/// <param name="p">Blending factor (0=<paramref name="Image1"/>, 1=<paramref name="Image2"/>).</param>
		/// <returns>Blended image.</returns>
		public static PixelInformation BlendColors(PixelInformation Image1, PixelInformation Image2, double p)
		{
			if (Image1.Width != Image2.Width || Image1.Height != Image2.Height)
				throw new ArgumentException("Images not of the same size.", nameof(Image2));

			PixelInformationRaw Raw1 = Image1.GetRaw();
			PixelInformationRaw Raw2 = Image2.GetRaw();
			byte[] Bin1 = (byte[])Raw1.Binary.Clone();
			byte[] Bin2 = Raw2.Binary;
			int i, j, c = Bin1.Length;

			if (Bin2.Length != c)
				throw new ArgumentException("Images not of the same size.", nameof(Image2));

			for (i = 0; i < c; i++)
			{
				j = (int)(Bin1[i] * (1 - p) + Bin2[i] * p + 0.5);
				if (j < 0)
					Bin1[i] = 0;
				else if (j > 255)
					Bin1[i] = 255;
				else
					Bin1[i] = (byte)j;
			}

			return new PixelInformationRaw(Raw1.ColorType, Bin1, Raw1.Width, Raw1.Height, Raw1.BytesPerRow);
		}

	}
}
