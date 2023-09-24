using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
//using Unsafe = System.Runtime.CompilerServices.Unsafe;

///<remarks>  Attribution
/// Work copied from SnowflakePowered/vcdiff
/// https://github.com/SnowflakePowered/vcdiff/blob/master/LICENSE
///</remarks>

namespace MSetGeneratorPrototype
{
	internal class VecBuffer : IDisposable
	{
		private MemoryHandle? _byteHandle;
		private unsafe byte* _bytePtr;
		private int _length;
		private int _offset;

		public unsafe VecBuffer(byte[] bytes)
		{
			_offset = 0;
			var memory = bytes != null ? new Memory<byte>(bytes) : Memory<byte>.Empty;
			_byteHandle = memory.Pin();
			CreateFromPointer((byte*)_byteHandle.Value.Pointer, memory.Length);
		}

		/// <summary/>
		public unsafe VecBuffer(Memory<byte> bytes)
		{
			_offset = 0;
			_byteHandle = bytes.Pin();
			CreateFromPointer((byte*)_byteHandle.Value.Pointer, bytes.Length);
		}

		/// <summary/>
		public unsafe VecBuffer(Span<byte> bytes)
		{
			_offset = 0;

			// Using GetPinnableReference because length of 0 means out of bound exception.
			CreateFromPointer((byte*)Unsafe.AsPointer(ref bytes.GetPinnableReference()), bytes.Length);
		}

		/// <summary/>
		public unsafe VecBuffer(byte* bytes, int length)
		{
			_offset = 0;
			CreateFromPointer(bytes, length);
		}

		private unsafe void CreateFromPointer(byte* pointer, int length)
		{
			_bytePtr = pointer;
			_length = length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(_bytePtr), _length);

		/// <summary>
		/// Dangerously gets the byte pointer.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte* DangerousGetBytePointer() => _bytePtr;

		/// <summary>
		/// Dangerously retrieves the byte pointer at the current position and then increases the _offset after.
		/// </summary>
		/// <param name="read"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte* DangerousGetBytePointerAtCurrentPositionAndIncrease_offsetAfter(int read)
		{
			byte* ptr = _bytePtr + _offset;
			_offset += read;
			return ptr;
		}

		/// <summary>
		/// Dangerously retrieves the byte pointer at the current position and then increases the _offset after.
		/// </summary>
		/// <param name="read"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte* GetBytePointer(int offset)
		{
			byte* ptr = _bytePtr + offset;
			return ptr;
		}



		public bool CanRead
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _offset < _length;
		}

		public int Position
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _offset;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			// We used to check, but this is never true in calls. if (value > length || value < 0) return;
			set => _offset = value;
		}

		public int Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte PeekByte() => *(_bytePtr + _offset);

		[SkipLocalsInit]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Span<byte> PeekBytes(int len)
		{
			int sliceLen = _offset + len > _length ? _length - _offset : len;
			return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(_bytePtr + _offset), sliceLen);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> ReadBytesToSpan(Span<byte> data)
		{
			var result = PeekBytes(data.Length);
			result.CopyTo(data);
			return result.Slice(0, result.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte ReadByte() => _bytePtr[_offset++];

		[SkipLocalsInit]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> ReadBytesAsSpan(int len)
		{
			var slice = PeekBytes(len);
			_offset += len;
			return slice;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory<byte> ReadBytes(int len)
		{
			var slice = PeekBytes(len);
			_offset += len;
			return slice.ToArray();
		}

		public void Next() => _offset++;

		public void Dispose()
		{
			_byteHandle?.Dispose();
			GC.SuppressFinalize(this);
		}

	}
}
