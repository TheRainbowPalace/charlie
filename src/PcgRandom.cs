using System;
using System.Security.Cryptography;

namespace Optimization
{
  /// <summary>
  /// Source:
  /// https://codereview.stackexchange.com/questions/161682/generating-unifor
  /// mly-distributed-random-numbers
  /// </summary>
  public class PcgRandom
  {
    private const double INVERSE_32_BIT = 2.32830643653869628906e-010d;
    private const double INVERSE_52_BIT = 2.22044604925031308085e-016d;

    private static RandomNumberGenerator Rng = new RNGCryptoServiceProvider();

    private ulong m_state;
    private readonly ulong m_stream;

    public PcgRandom(ulong state, ulong stream)
    {
      m_state = state;
      m_stream = stream | 1UL;
    }

    public PcgRandom(ulong state) : this(state, GetSeed()) {}

    public PcgRandom() : this(GetSeed(), GetSeed()) {}

    /// <summary>
    /// Generates a uniformly distributed double between the range (0, 1).
    /// </summary>
    public double GetDouble()
    {
      return CreateDouble(GetInt32(), GetInt32());
    }

    public double GetDouble(double left, double right)
    {
      return GetDouble() * Math.Abs(right - left) + left;
    }

    public static double Double()
    {
      return new PcgRandom().GetDouble();
    }
    
    public static double Double(double left, double right)
    {
      return new PcgRandom().GetDouble(left, right);
    }

    /// <summary>
    /// Generates a uniformly distributed 32-bit signed integer between the
    /// range of int.MaxValue and int.MinValue.
    /// </summary>
    public int GetInt32()
    {
      return (int) GetUInt32();
    }

    /// <summary>
    /// Generates a uniformly distributed 32-bit signed integer between the
    /// range [min, max].
    /// </summary>
    public int GetInt32(int x, int y)
    {
      var min = Math.Min(x, y);
      var max = Math.Max(x, y);
      var range = max + 1L - min;

      if (uint.MaxValue > range)
      {
        return ((int) (GetUInt32((uint) range) + min));
      }

      return GetInt32();
    }

    /// <summary>
    /// Generates a uniformly distributed 32-bit unsigned integer between the
    /// range of uint.MaxValue and uint.MinValue.
    /// </summary>
    public uint GetUInt32()
    {
      return Pcg32(ref m_state, m_stream);
    }

    /// <summary>
    /// Generates a uniformly distributed 32-bit unsigned integer between the
    /// range [min, max].
    /// </summary>
    public uint GetUInt32(uint x, uint y)
    {
      var min = Math.Min(x, y);
      var max = Math.Max(x, y);
      var range = max + 1UL - min;

      if (uint.MaxValue > range)
      {
        return GetUInt32((uint) range) + min;
      }

      return GetUInt32();
    }

    private uint GetUInt32(uint exclusiveHigh)
    {
      var threshold =
        (uint) ((0x100000000UL - exclusiveHigh) % exclusiveHigh);
      var sample = GetUInt32();

      while (sample < threshold)
      {
        sample = GetUInt32();
      }

      return sample % exclusiveHigh;
    }

    private static double CreateDouble(int x, int y)
    {
      // reference: https://www.doornik.com/research/randomdouble.pdf
      return 0.5d + INVERSE_52_BIT / 2 + x * INVERSE_32_BIT +
             (y & 0x000FFFFF) * INVERSE_52_BIT;
    }

    private static ulong GetSeed()
    {
      var buffer = new byte[sizeof(ulong)];

      Rng.GetBytes(buffer);

      return BitConverter.ToUInt64(buffer, 0);
    }

    private static uint Pcg32(ref ulong state, ulong stream)
    {
      // reference: http://www.pcg-random.org/paper.html
      state = unchecked(state * 6364136223846793005UL + stream);

      return RotateRight((uint) (((state >> 18) ^ state) >> 27),
        (int) (state >> 59));
    }

    private static uint RotateRight(uint value, int count)
    {
      return ((value >> count) | (value << ((-count) & 31)));
    }
  }
}