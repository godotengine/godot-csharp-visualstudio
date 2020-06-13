using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GodotAddinVS
{
    // Incomplete implementation of the Godot's Variant encoder. Add missing parts as needed.

    public class GodotVariantEncoder
    {
        private readonly List<byte> _buffer = new List<byte>();

        public int Length => _buffer.Count;

        public byte[] ToArray() => _buffer.ToArray();

        public void AddBytes(params byte[] bytes) =>
            _buffer.AddRange(bytes);

        public void AddInt(int value) =>
            AddBytes(BitConverter.GetBytes(value));

        public void AddType(GodotVariant.Type type) =>
            AddInt((int) type);

        public void AddString(string value)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);

            AddType(GodotVariant.Type.String);
            AddInt(utf8Bytes.Length);
            AddBytes(utf8Bytes);
            AddBytes(0); // Godot's UTF8 converter adds a trailing whitespace

            while (_buffer.Count % 4 != 0)
                _buffer.Add(0);
        }

        public void AddArray(List<GodotVariant> array)
        {
            AddType(GodotVariant.Type.Array);
            AddInt(array.Count);

            foreach (var element in array)
            {
                if (element.VariantType == GodotVariant.Type.String)
                    AddString(element.Get<string>());
                else
                    throw new NotImplementedException();
            }
        }

        public static void Encode(GodotVariant variant, Stream stream)
        {
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), leaveOpen: true))
            {
                var encoder = new GodotVariantEncoder();
                switch (variant.VariantType)
                {
                    case GodotVariant.Type.String:
                        encoder.AddString((string) variant.Value);
                        break;
                    case GodotVariant.Type.Array:
                        encoder.AddArray((List<GodotVariant>) variant.Value);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // ReSharper disable once RedundantCast
                writer.Write((int) encoder.Length);
                writer.Write(encoder.ToArray());
            }
        }
    }

    public class GodotVariant
    {
        public enum Type
        {
            Nil = 0,
            Bool = 1,
            Int = 2,
            Real = 3,
            String = 4,
            Vector2 = 5,
            Rect2 = 6,
            Vector3 = 7,
            Transform2d = 8,
            Quat = 10,
            Aabb = 11,
            Basis = 12,
            Transform = 13,
            Color = 14,
            NodePath = 15,
            Rid = 16,
            Object = 17,
            Dictionary = 18,
            Array = 19,
            RawArray = 20,
            IntArray = 21,
            RealArray = 22,
            StringArray = 23,
            Vector2Array = 24,
            Vector3Array = 25,
            ColorArray = 26,
            Max = 27
        }

        public object Value { get; }
        public Type VariantType { get; }

        public T Get<T>()
        {
            return (T) Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public GodotVariant(string value)
        {
            Value = value;
            VariantType = Type.String;
        }

        public GodotVariant(List<GodotVariant> value)
        {
            Value = value;
            VariantType = Type.Array;
        }

        public static implicit operator GodotVariant(string value) => new GodotVariant(value);
        public static implicit operator GodotVariant(List<GodotVariant> value) => new GodotVariant(value);
    }
}
