﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TSR
{
    //Incomplete.

    public class TSR_File
    {

        public static byte[] XOR_TABLE = new byte[1024] 
        {
            0xE4, 0x6B, 0x8E, 0x6D, 0xA5, 0x70, 0xE5, 0x6C, 0xD5, 0x98, 0x83, 0x92, 0xA3, 0xA7, 0x2A, 0x67,
            0x0D, 0xF5, 0xDD, 0xAF, 0x50, 0x18, 0xF4, 0x34, 0xA3, 0xBC, 0x70, 0xC5, 0x25, 0xD5, 0x8B, 0x7F,
            0x13, 0xF9, 0x93, 0x4C, 0x3E, 0x08, 0xF9, 0xA8, 0xBC, 0x0E, 0xA5, 0xAB, 0xDE, 0x69, 0x8D, 0x44,
            0x63, 0x2E, 0x86, 0xC2, 0xEB, 0xC8, 0x5A, 0x9A, 0xAE, 0xE3, 0x1B, 0x64, 0x6E, 0x8A, 0xB5, 0x26,
            0xAA, 0x8C, 0xCA, 0xD5, 0x15, 0x25, 0xBB, 0x19, 0x47, 0xD8, 0x03, 0x51, 0xB1, 0xA2, 0xC9, 0xD7,
            0xD6, 0x4E, 0xB5, 0x85, 0xB8, 0x24, 0x03, 0x78, 0x91, 0xC5, 0xD4, 0x14, 0xC4, 0x5B, 0xCC, 0x48,
            0x14, 0xEC, 0xDA, 0x13, 0x13, 0x0F, 0x54, 0x46, 0xDB, 0xC3, 0x44, 0x8C, 0x05, 0x9B, 0x06, 0xA8,
            0xCE, 0x20, 0x12, 0xFE, 0xA0, 0x70, 0x15, 0x56, 0xB3, 0x2B, 0x47, 0xDC, 0x0F, 0x8F, 0xF8, 0x6B,
            0xB5, 0xE1, 0x6D, 0x0B, 0x1E, 0x10, 0xEA, 0xB7, 0xE4, 0x94, 0x11, 0x65, 0xBF, 0x9D, 0x6A, 0x40,
            0xB4, 0x6A, 0x43, 0x38, 0x89, 0xF6, 0xBA, 0xBC, 0x7C, 0xDA, 0x18, 0xC6, 0x35, 0x6F, 0x61, 0x19,
            0xF8, 0x33, 0x29, 0xC6, 0x1F, 0x6E, 0xA8, 0xF5, 0xC7, 0x15, 0x11, 0xE2, 0xCA, 0xED, 0x20, 0x26,
            0xEE, 0xF4, 0xF2, 0x38, 0x5D, 0xFE, 0x1A, 0x33, 0x54, 0x9D, 0xF1, 0xD9, 0x1F, 0x43, 0x2E, 0xD8,
            0x44, 0xA9, 0xB5, 0x4E, 0xFE, 0x72, 0xB4, 0x87, 0xEE, 0x0C, 0xEC, 0x0E, 0x0E, 0xD6, 0x4E, 0xE2,
            0xE6, 0x89, 0xC7, 0x08, 0x02, 0xD0, 0x5D, 0x43, 0xA3, 0x3A, 0x7A, 0x1F, 0xB5, 0x53, 0x87, 0x34,
            0x02, 0x0D, 0xBC, 0xA8, 0xA4, 0x64, 0x37, 0xF5, 0xC0, 0x41, 0x4C, 0xED, 0x71, 0x9F, 0x1D, 0xFC,
            0x03, 0xED, 0x6A, 0xAE, 0x62, 0xB4, 0xA9, 0x72, 0xD1, 0x7A, 0x5A, 0x9C, 0xDF, 0xE6, 0x95, 0xB0,
            0x97, 0x25, 0xE4, 0xDD, 0xF7, 0x8B, 0x59, 0xC8, 0xA4, 0x7E, 0xD7, 0x8B, 0xDC, 0x90, 0xB4, 0xFD,
            0xAC, 0xEA, 0x82, 0x35, 0x63, 0xF1, 0x29, 0x4A, 0x47, 0x25, 0x3A, 0x5C, 0x86, 0x46, 0x7F, 0xD7,
            0x6F, 0xB9, 0xD6, 0xF6, 0xE1, 0x31, 0x41, 0x87, 0x05, 0x89, 0x36, 0xEE, 0x38, 0xEF, 0x3C, 0x6D,
            0x4B, 0x49, 0xB7, 0xA2, 0xEE, 0xD1, 0x04, 0x52, 0x6B, 0x03, 0xC0, 0x65, 0x90, 0xB8, 0x6E, 0x31,
            0xEE, 0x93, 0x3A, 0xFA, 0x49, 0x9C, 0x17, 0xBA, 0x47, 0x2B, 0x10, 0x1F, 0x6B, 0x08, 0xDB, 0xD2,
            0x47, 0xD0, 0xB2, 0x00, 0xEC, 0x9B, 0x60, 0x12, 0xA6, 0xDB, 0x97, 0xBE, 0xE6, 0x87, 0x8A, 0x45,
            0x80, 0x7A, 0xB7, 0xF2, 0x17, 0x16, 0x04, 0xE9, 0xD4, 0x2C, 0x0E, 0x25, 0x5F, 0x20, 0xBC, 0xB6,
            0x08, 0x48, 0x1C, 0x54, 0x45, 0x96, 0x67, 0x13, 0x60, 0x77, 0x66, 0x72, 0x71, 0xFA, 0xF9, 0x9A,
            0x8A, 0x35, 0xF5, 0xE5, 0x33, 0xE4, 0x30, 0x9E, 0x16, 0x54, 0xD6, 0x08, 0xFA, 0x80, 0x07, 0xA1,
            0xF5, 0x79, 0x9A, 0xA7, 0xDE, 0x0B, 0x41, 0xDB, 0x02, 0x9D, 0xD4, 0x86, 0x18, 0x59, 0xE6, 0xBA,
            0x76, 0x8D, 0x9D, 0xDB, 0x84, 0x51, 0xC1, 0x5E, 0x72, 0x6B, 0x14, 0xCF, 0x26, 0x6F, 0xE0, 0x1A,
            0x79, 0x2B, 0xD5, 0x03, 0xA2, 0x42, 0x16, 0xF5, 0xF2, 0x16, 0x89, 0x05, 0xC2, 0xEB, 0x78, 0x2E,
            0xAA, 0x4A, 0x57, 0xDC, 0xF4, 0xA4, 0xE2, 0xB2, 0x51, 0x38, 0x6C, 0x85, 0xCA, 0x37, 0x73, 0xA8,
            0xF8, 0x25, 0x77, 0x6C, 0x78, 0x82, 0x0D, 0xE6, 0x9A, 0xA9, 0x2F, 0xF3, 0x59, 0xF9, 0xD5, 0x7A,
            0x90, 0x34, 0xCA, 0xF1, 0x6B, 0x25, 0xB9, 0x24, 0x1B, 0x84, 0x87, 0x31, 0xCD, 0x1E, 0xE5, 0xD5,
            0xDD, 0x30, 0x26, 0xED, 0x48, 0x15, 0x4E, 0x39, 0x61, 0x21, 0x6B, 0x5C, 0xC4, 0xCB, 0x28, 0x2A,
            0x8F, 0x13, 0x9F, 0x22, 0xCE, 0x1B, 0x6F, 0x39, 0x38, 0x18, 0x0F, 0xD8, 0x1A, 0x6C, 0x62, 0x29,
            0x90, 0x14, 0x8B, 0x8E, 0xFA, 0x41, 0x02, 0x73, 0xAE, 0x42, 0xE7, 0x46, 0xEB, 0xA9, 0x97, 0xC3,
            0x0F, 0xAD, 0x7E, 0x75, 0x09, 0xCE, 0x2C, 0x7A, 0x11, 0xB9, 0xAA, 0x86, 0x95, 0x6B, 0x0E, 0x2B,
            0x77, 0x99, 0x4D, 0x57, 0x77, 0x4E, 0x51, 0x1E, 0xEB, 0xD6, 0x4B, 0xB9, 0xB6, 0xDB, 0x4B, 0xCF,
            0x77, 0xCE, 0x0E, 0xF3, 0x01, 0x88, 0x17, 0x6F, 0x0C, 0x32, 0x01, 0x41, 0x2A, 0x62, 0x13, 0x63,
            0xFB, 0x86, 0x15, 0x4E, 0xA5, 0x86, 0x62, 0xC0, 0x7F, 0xA5, 0x3F, 0xBD, 0x0D, 0xA8, 0x6B, 0xD5,
            0x31, 0x3B, 0xF6, 0xA6, 0x9F, 0x90, 0x58, 0xA1, 0x92, 0x49, 0xBA, 0x11, 0xBD, 0x98, 0x98, 0x59,
            0x85, 0xA5, 0x8A, 0x7D, 0x6D, 0x30, 0x5E, 0xE3, 0xD1, 0x77, 0x69, 0x5C, 0xD8, 0x5A, 0x20, 0x5E,
            0xA4, 0xBC, 0xE2, 0x93, 0xCB, 0x2E, 0x18, 0x98, 0x0C, 0xC7, 0x80, 0xFE, 0x3A, 0x56, 0xC6, 0x95,
            0x7B, 0xBC, 0x55, 0xEA, 0xB7, 0x93, 0x6C, 0x10, 0x4C, 0x14, 0x74, 0x9B, 0x00, 0x37, 0x90, 0xF0,
            0x38, 0x1C, 0x77, 0xC4, 0x6E, 0xA9, 0x7F, 0xDA, 0xDF, 0x74, 0xF9, 0x12, 0x86, 0xE4, 0xC4, 0xA0,
            0x47, 0x94, 0x1D, 0xA0, 0x6C, 0xF8, 0xB5, 0xCB, 0x55, 0x43, 0x06, 0x83, 0x6A, 0x88, 0xE6, 0x16,
            0x55, 0x1F, 0x5D, 0x40, 0x6E, 0x4A, 0xB3, 0xF1, 0x77, 0x19, 0xCE, 0x51, 0x8A, 0x8A, 0xBB, 0x02,
            0x4F, 0xF4, 0x8B, 0xA5, 0x72, 0xA7, 0x60, 0x9F, 0x55, 0xCD, 0xC7, 0x1D, 0x02, 0x94, 0x49, 0x56,
            0x63, 0x8F, 0x3D, 0x10, 0xB5, 0x59, 0xDF, 0x65, 0x3A, 0x7B, 0xA6, 0xC6, 0x2F, 0x90, 0xD3, 0x42,
            0xFC, 0xA6, 0x47, 0x01, 0xB3, 0xE8, 0x96, 0x14, 0xB4, 0x7A, 0x61, 0x70, 0xAD, 0xA5, 0xDF, 0x37,
            0xCA, 0x34, 0xBD, 0x3A, 0x2A, 0x1E, 0x2A, 0xBC, 0x8F, 0x65, 0x2B, 0x79, 0x5B, 0x3D, 0x34, 0xE7,
            0xB7, 0x71, 0xF7, 0xBC, 0x16, 0x03, 0x7F, 0xB0, 0xDA, 0x13, 0x79, 0x84, 0x54, 0x02, 0xD3, 0x44,
            0xF2, 0xD6, 0x89, 0xC8, 0xB4, 0xE0, 0xBA, 0x80, 0xE0, 0x9D, 0x02, 0x71, 0xF7, 0xDA, 0x04, 0x7C,
            0xE7, 0x1D, 0x46, 0xDE, 0x82, 0x3F, 0x42, 0xFD, 0x30, 0x5D, 0xB9, 0x62, 0xDF, 0xF1, 0x4B, 0x02,
            0x44, 0x3D, 0x45, 0xC1, 0x3D, 0xE8, 0xBA, 0x39, 0x94, 0xEC, 0xD5, 0xB7, 0xEB, 0xAF, 0x6D, 0x86,
            0xF4, 0x70, 0xDA, 0x71, 0xE1, 0xE5, 0x08, 0x83, 0x1C, 0x23, 0xC9, 0x11, 0x38, 0xBD, 0x70, 0xFA,
            0x26, 0x30, 0x9B, 0x2E, 0xAB, 0x7F, 0x50, 0x6D, 0x13, 0x1A, 0x4C, 0x52, 0x20, 0x05, 0x97, 0x8F,
            0x46, 0x36, 0x5C, 0x7A, 0x1A, 0x3E, 0xF7, 0xC8, 0x07, 0x2B, 0x51, 0x99, 0x43, 0xAD, 0x69, 0xB5,
            0x02, 0x79, 0x33, 0x16, 0xE8, 0xEA, 0xA4, 0xA6, 0xC4, 0xEE, 0x0E, 0x4A, 0x7D, 0x21, 0xA9, 0x1F,
            0x45, 0x33, 0x73, 0x03, 0x15, 0x8F, 0x3B, 0x57, 0x5A, 0x3E, 0xF7, 0x03, 0xEA, 0x09, 0x5F, 0xBB,
            0x3E, 0xDD, 0xB3, 0x81, 0xDB, 0x75, 0xDF, 0x6B, 0x12, 0x33, 0xC3, 0xA7, 0xE9, 0x4E, 0xCC, 0xBC,
            0x59, 0x32, 0xC8, 0x13, 0xB9, 0x23, 0xF8, 0xB5, 0x7B, 0x25, 0x66, 0x56, 0x17, 0x19, 0x79, 0x92,
            0x44, 0x28, 0xC6, 0x77, 0x6C, 0x64, 0x2A, 0x45, 0x62, 0xAD, 0x15, 0x71, 0x4E, 0xD2, 0x29, 0xEF,
            0xEA, 0xF8, 0x03, 0xB1, 0xF0, 0x41, 0x58, 0x6C, 0xD3, 0xA5, 0x45, 0x9A, 0xAE, 0x24, 0xDF, 0xC4,
            0x7A, 0x1F, 0x13, 0x02, 0x83, 0x02, 0xA9, 0xBA, 0x1D, 0x26, 0xAA, 0xB1, 0x93, 0xF6, 0xE4, 0x42,
            0x60, 0x51, 0xCA, 0xE8, 0xA2, 0x30, 0x83, 0x03, 0xCB, 0x89, 0x3A, 0xD7, 0x9A, 0x73, 0xBB, 0xD8
        };
        

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Function")]
        public List<TSR_Function> Functions { get; set; }

        public static TSR_File Parse(string path, bool writeXml)
        {
            var rawBytes = File.ReadAllBytes(path);
            var bytes = rawBytes.ToList();
            var tsr = Parse(new TSR_Reader(path), bytes, rawBytes);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TSR_File));
                serializer.SerializeToFile(tsr, path + ".xml");
            }

            return tsr;
        }

        public static TSR_File Parse(byte[] rawBytes)
        {
            var bytes = rawBytes.ToList();
            var tsr = Parse(new TSR_Reader(rawBytes), bytes, rawBytes);
            
            return tsr;
        }

        public static TSR_File Parse(TSR_Reader tsrReader, List<byte> bytes, byte[] rawBytes)
        {
            TSR_File tsrFile = new TSR_File() { Functions = new List<TSR_Function>() };

            int functionCount = BitConverter.ToUInt16(rawBytes, 0);
            int functionOffset = 2 + (functionCount * 4);
            
            for(int i = 0; i < functionCount; i++)
            {
                int funcSize = BitConverter.ToInt32(rawBytes, 2 + (4 * i));
                tsrFile.Functions.Add(TSR_Function.Load(tsrReader, functionOffset));
                functionOffset += funcSize;
            }

            return tsrFile;
        }

        public static string DecodeString(List<byte> bytes, byte[] rawBytes, int offset, ref int enc_pos)
        {
            int size = BitConverter.ToInt32(rawBytes, offset);
            List<byte> encStringBytes = bytes.GetRange(offset + 4, size);

            if (enc_pos >= XOR_TABLE.Count())
                enc_pos = 0;

            for (int i = 0; i < size; i++)
            {
                byte b = (byte)(encStringBytes[i] ^ XOR_TABLE[enc_pos]);
                encStringBytes[i] = b;

                enc_pos++;
                if (enc_pos == XOR_TABLE.Count())
                    enc_pos = 0;

                if (b == 0 && i == (size - 1))
                    break;
            }

            return Utils.GetString(encStringBytes, 0, size);
        }
    }

    [YAXSerializeAs("Function")]
    public class TSR_Function
    {
        [YAXDontSerialize]
        public int Header { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ushort Unk { get; set; }

        public List<TSR_Tag> Tags { get; set; }
        public List<TSR_Sentence> Sentences { get; set; }

        [YAXDontSerialize]
        public List<object> Lines { get; set; }

        public static TSR_Function Load(TSR_Reader tsrReader, int offset)
        {
            tsrReader.Position = offset;
            TSR_Function function = new TSR_Function() { Tags = new List<TSR_Tag>(), Sentences = new List<TSR_Sentence>() };

            int enc_pos = 4;

            function.Header = tsrReader.ReadInt32();
            function.Name = tsrReader.DecodeString(ref enc_pos);
            function.Unk = tsrReader.ReadUInt16();

            //Tags
            int tagCount = tsrReader.ReadUInt16();

            for(int i = 0; i < tagCount; i++)
            {
                function.Tags.Add(TSR_Tag.Load(tsrReader, ref enc_pos));
            }

            //Sentence
            int sentenceCount = tsrReader.ReadUInt16();

            for(int i = 0; i < sentenceCount; i++)
            {
                function.Sentences.Add(TSR_Sentence.Load(tsrReader, ref enc_pos));
            }

            //Lines
            function.Lines = new List<object>();

            for(int i = 0; i < function.Sentences.Count; i++)
            {
                var tag = function.GetTag(i);

                if(tag != null)
                {
                    function.Lines.Add(tag);
                }

                function.Lines.Add(function.Sentences[i]);
            }

            return function;
        }
        
        private TSR_Tag GetTag(int index)
        {
            foreach(var tag in Tags)
            {
                if (tag.Index == index) return tag;
            }

            return null;
        }
    }


    public class TSR_Tag
    {
        [YAXAttributeForClass]
        public ushort Index { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXDontSerialize]
        public int Size
        {
            get
            {
                return 7 + Name.Length;
            }
        }

        public static TSR_Tag Load(TSR_Reader tsrReader, ref int enc_pos)
        {
            TSR_Tag tag = new TSR_Tag();

            tag.Index = tsrReader.ReadUInt16();
            tag.Name = tsrReader.DecodeString(ref enc_pos);

            return tag;
        }
    }

    public class TSR_Sentence
    {
        [YAXAttributeForClass]
        public string Value { get; set; }
        [YAXAttributeForClass]
        public int Unk { get; set; }

        [YAXDontSerialize]
        public string GetArgumentTypes
        {
            get
            {
                if(Arguments != null)
                {
                    if(Arguments.Count == 0) return "No arguments";
                    StringBuilder str = new StringBuilder();
                    bool first = true;

                    foreach(var args in Arguments)
                    {
                        if (first)
                        {
                            str.Append(String.Format("{0}", args.valueType));
                            first = false;
                        }
                        else
                        {
                            str.Append(String.Format(", {0}", args.valueType));
                        }
                    }

                    return str.ToString();
                }
                return "No arguments";
            }
        }

        [YAXDontSerializeIfNull]
        public List<TSR_SentenceArg> Arguments { get; set; }

        public static TSR_Sentence Load(TSR_Reader tsrReader, ref int enc_pos)
        {
            TSR_Sentence sentence = new TSR_Sentence() { Arguments = new List<TSR_SentenceArg>() };

            sentence.Value = tsrReader.DecodeString(ref enc_pos);
            sentence.Unk = tsrReader.ReadUInt16();

            //Arguments
            int argCount = tsrReader.ReadUInt16();

            for(int i = 0; i < argCount; i++)
            {
                sentence.Arguments.Add(TSR_SentenceArg.Load(tsrReader, ref enc_pos));
            }

            return sentence;
        }


    }
    
    public class TSR_SentenceArg
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ValueType valueType { get; set; }
        [YAXAttributeForClass]
        public string Value { get; set; }

        public static TSR_SentenceArg Load(TSR_Reader tsrReader, ref int enc_pos)
        {
            TSR_SentenceArg arg = new TSR_SentenceArg();

            arg.valueType = (ValueType)tsrReader.ReadUInt16();

            switch (arg.valueType)
            {
                case ValueType.Int:
                    arg.Value = tsrReader.ReadInt32().ToString();
                    break;
                case ValueType.Float:
                    arg.Value = tsrReader.ReadFloat().ToString();
                    break;
                case ValueType.Tag:
                case ValueType.Variable:
                case ValueType.String:
                case ValueType.Expression:
                    arg.Value = tsrReader.DecodeString(ref enc_pos);
                    break;
                default:
                    throw new InvalidDataException(String.Format("Unknown ValueType: {0}\nParse failed.", arg.valueType));
            }

            return arg;

        }
    }

    public enum ValueType : ushort
    {
        Int = 1,
        Float = 2,
        String = 3,
        Tag = 4,
        Variable = 5,
        Expression = 6
    }
}