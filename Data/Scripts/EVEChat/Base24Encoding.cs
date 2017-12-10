//文件：Base24Encoding.cs
//作者：blueqt
//日期：2010-01-17
//版本：1.0
using System;

namespace Douxt
{
    /// <summary>
    /// 表示 base 24 编码
    /// </summary>
    public class Base24Encoding
    {
        /// <summary>
        /// 表示 base 24 编码的默认实现使用的字符映射表：BCDFGHJKMPQRTVWXY2346789
        /// </summary>
        public const string DefaultMap = "9BCDFGHJKMPQRTVWXY234678";

        private static Base24Encoding defaultInstance;

        private string map;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Base24Encoding()
        {
            this.map = DefaultMap;
        }

        /// <summary>
        /// 获取 base 24 编码的默认实现
        /// </summary>
        public static Base24Encoding Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new Base24Encoding();
                }
                return defaultInstance;
            }
        }

        /// <summary>
        /// 获取或设置进行 base 24 编码的字符映射表
        /// </summary>
        /// <exception cref="ArgumentException">map不符合规范：长度24不允许重复字符</exception>
        public string Map
        {
            get { return this.map; }
            set
            {
                if (value == null || value.Length != 24)
                {
                    throw new ArgumentException("map必须是长度24的字符串");
                }
                for (byte i = 1; i < 24; i++)
                {
                    for (byte j = 0; j < i; j++)
                    {
                        if (value[i] == value[j])
                        {
                            throw new ArgumentException("map中不能含有重复字符");
                        }
                    }
                }
                this.map = value;
            }
        }

        /// <summary>
        /// 将 8 位无符号整数数组的值转换为它的等效 System.String 表示形式（使用 base 24 数字编码）。
        /// </summary>
        /// <param name="bytes">一个 8 位无符号整数数组。</param>
        /// <returns>bytes 内容的 System.String 表示形式，以基数为 24 的数字表示。</returns>
        /// <exception cref="ArgumentNullException">bytes 为 null。</exception>
        public string GetString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            string text = string.Empty;
            for (int i = bytes.Length - 1; i >= 0; i -= 4)
            {
                uint data = 0;
                for (int j = i < 3 ? i : 3; j >= 0; j--)
                {
                    data = (data << 8) + bytes[i - j];
                }

                string t = string.Empty;
                while (data > 0)
                {
                    int d = (int)(data % 24);
                    t = map[d] + t;
                    data = data / 24;
                }
                text = t.PadLeft(7, map[0]) + text;
            }
            return text;
        }

        /// <summary>
        /// 将指定的 System.String（它将二进制数据编码为 base 24 数字）转换成等效的 8 位无符号整数数组。
        /// </summary>
        /// <param name="text">System.String。</param>
        /// <returns>等效于 text 的 8 位无符号整数数组。</returns>
        /// <exception cref="ArgumentNullException">text 为 null。</exception>
        /// <exception cref="FormatException">text 包含非 base 24 字符 或 text 包含不可转换的 base 24 字符序列</exception>
        public byte[] GetBytes(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int len = text.Length / 7 + (text.Length % 7 == 0 ? 0 : 1);
            byte[] bytes = new byte[len * 4];
            int pos = bytes.Length - 1;
            for (int i = text.Length - 1; i >= 0; i -= 7, pos -= 4)
            {
                uint data = 0;
                for (int j = i < 6 ? i : 6; j >= 0; j--)
                {
                    int d = map.IndexOf(text[i - j]);
                    if (d == -1)
                    {
                        //throw new FormatException("text 包含非 base 24 字符");
                        return null;
                    }
                    try
                    {
                        data = checked(data * 24 + (uint)d);
                    }
                    catch (Exception)
                    {
                        //throw new FormatException("text 包含不可转换的 base 24 字符序列");
                    }
                }

                byte[] t = BitConverter.GetBytes(data);
                for (int j = 0; j < 4; j++)
                {
                    bytes[pos - j] = t[j];
                }
            }
            return bytes;
        }
    }
}
