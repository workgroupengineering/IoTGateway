﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Waher.Networking.HTTP.TransferEncodings
{
	/// <summary>
	/// Transfer encoding for internal transfers of content
	/// </summary>
	public class InternalTransfer : TransferEncoding
	{
		private readonly MemoryStream ms;
		private readonly TaskCompletionSource<bool> task;
		private Timer timeoutTimer = null;

		/// <summary>
		/// Transfer encoding for internal transfers of content
		/// </summary>
		/// <param name="ms">Content will be output to this stream.</param>
		public InternalTransfer(MemoryStream ms)
			: base()
		{
			this.ms = ms;
			this.task = new TaskCompletionSource<bool>();
		}

		/// <summary>
		/// Is called when the content has all been sent to the encoder. The method sends any cached data to the client.
		/// </summary>
		public override Task<bool> ContentSentAsync()
		{
			this.task.TrySetResult(true);
			this.timeoutTimer?.Dispose();
			this.timeoutTimer = null;
			return Task.FromResult(true);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			this.timeoutTimer?.Dispose();
			this.timeoutTimer = null;

			base.Dispose();
		}

		/// <summary>
		/// Waits for all of the data to be returned.
		/// </summary>
		/// <param name="TimeoutMilliseconds">Maximum time to wait, in milliseconds</param>
		public Task WaitUntilSent(int TimeoutMilliseconds)
		{
			this.timeoutTimer?.Dispose();
			this.timeoutTimer = null;

			this.timeoutTimer = new Timer((P) =>
			{
				this.task.TrySetException(new TimeoutException("Timed out while waiting for response."));
			}, null, TimeoutMilliseconds, Timeout.Infinite);

			return this.task.Task;
		}

		/// <summary>
		/// Is called when new binary data has been received that needs to be decoded.
		/// </summary>
		/// <param name="ConstantBuffer">If the contents of the buffer remains constant (true),
		/// or if the contents in the buffer may change after the call (false).</param>
		/// <param name="Data">Data buffer.</param>
		/// <param name="Offset">Offset where binary data begins.</param>
		/// <param name="NrRead">Number of bytes read.</param>
		/// <returns>
		/// Bits 0-31: >Number of bytes accepted by the transfer encoding. If less than <paramref name="NrRead"/>, the rest is part of a separate message.
		/// Bit 32: If decoding has completed.
		/// Bit 33: If transmission to underlying stream failed.
		/// </returns>
		public override Task<ulong> DecodeAsync(bool ConstantBuffer, byte[] Data, int Offset, int NrRead)
		{
			return Task.FromResult<ulong>(0);
		}

		/// <summary>
		/// Is called when new binary data is to be sent and needs to be encoded.
		/// </summary>
		/// <param name="ConstantBuffer">If the contents of the buffer remains constant (true),
		/// or if the contents in the buffer may change after the call (false).</param>
		/// <param name="Data">Data buffer.</param>
		/// <param name="Offset">Offset where binary data begins.</param>
		/// <param name="NrBytes">Number of bytes to encode.</param>
		/// <param name="LastData">If no more data is expected.</param>
		public override Task<bool> EncodeAsync(bool ConstantBuffer, byte[] Data, int Offset, int NrBytes, bool LastData)
		{
			this.ms.Write(Data, Offset, NrBytes);
			return Task.FromResult(true);
		}

		/// <summary>
		/// Sends any remaining data to the client.
		/// </summary>
		/// <param name="EndOfData">If no more data is expected.</param>
		public override Task<bool> FlushAsync(bool EndOfData)
		{
			this.ms.Flush();
			return Task.FromResult(true);
		}
	}
}
