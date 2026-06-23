using HVAC.EnergyMonitor.Models.Enums;
using System;
using System.Buffers.Binary;

namespace HVAC.EnergyMonitor.Infrastructure.Helpers;

public static class ByteOrderConverter
{
    public static float ToFloat(ushort high, ushort low, ByteOrder order)
    {
        Span<byte> bytes = stackalloc byte[4];
        switch (order)
        {
            case ByteOrder.BigEndian:
                BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(0, 2), high);
                BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(2, 2), low);
                break;
            case ByteOrder.LittleEndian:
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(0, 2), low);
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(2, 2), high);
                break;
            case ByteOrder.BigEndianSwap:
                BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(0, 2), low);
                BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(2, 2), high);
                break;
            case ByteOrder.LittleEndianSwap:
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(0, 2), high);
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(2, 2), low);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(order));
        }
        return BitConverter.ToSingle(bytes);
    }
}
