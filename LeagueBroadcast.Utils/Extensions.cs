using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public static class Extensions
    {
        #region Array
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            T[] result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }
        #endregion

        #region Byte
        public static byte[] GetSubArray(this byte[] source, int offset, int size)
        {
            byte[] res = new byte[size];
            Buffer.BlockCopy(source, offset, res, 0, size);
            return res;
        }

        public static int ToInt(this byte[] source, int offset = 0)
        {
            return BitConverter.ToInt32(source, offset);
        }

        public static uint ToUInt(this byte[] source, int offset = 0)
        {
            return BitConverter.ToUInt32(source, offset);
        }

        public static short ToShort(this byte[] source, int offset = 0)
        {
            return BitConverter.ToInt16(source, offset);
        }

        public static float ToFloat(this byte[] input, int offset = 0)
        {
            return BitConverter.ToSingle(input, offset);
        }

        public static bool IsASCII(this byte[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 127)
                {
                    return false;
                }
            }
            return true;
        }

        public static string DecodeAscii(this byte[] buffer)
        {
            int count = Array.IndexOf<byte>(buffer, 0, 0);
            if (count < 0) count = buffer.Length;
            return Encoding.ASCII.GetString(buffer, 0, count);
        }

        public static void Write(this byte[] dest, byte[] input) => Write(dest, 0, input);

        public static void Write(this byte[] dest, int offset, byte[] input)
        {
            Array.Copy(input, 0, dest, offset, input.Length);
        }

        public static void Write(this char[] dest, byte[] input)
        {
            Write(dest, 0, input);
        }

        public static void Write(this char[] dest, int offset, byte[] input)
        {
            Array.Copy(input, 0, dest, offset, input.Length);
        }

        #endregion

        #region Color
        public static string ToSerializedString(this Color c)
        {
            return $"rgb({c.R},{c.G},{c.B})";
        }
        #endregion

        #region Dictionary
        public static T? KeyByValue<T, W>(this Dictionary<T, W> dict, W val)
        {
            T? key = default;
            foreach (KeyValuePair<T, W> pair in dict)
            {
                if (EqualityComparer<W>.Default.Equals(pair.Value, val))
                {
                    key = pair.Key;
                    break;
                }
            }
            return key;
        }
        #endregion

        #region Directory
        public static void Empty(this DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
            {
                subDirectory.Delete(true);
            }
        }
        #endregion

        #region Enum
        public static T Next<T>(this T src) where T : Enum
        {
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }

        #endregion

        #region HttpClient

        public static async Task<HttpStatusCode> DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            long? contentLength = response.Content.Headers.ContentLength;

            using Stream? download = await response.Content.ReadAsStreamAsync(cancellationToken);

            // Ignore progress reporting when no progress reporter was 
            // passed or when the content length is unknown
            if (progress == null || !contentLength.HasValue)
            {
                await download.CopyToAsync(destination, cancellationToken);
                return response.StatusCode;
            }

            // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
            Progress<long>? relativeProgress = new(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
            // Use extension method to report progress while downloading
            await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
            progress.Report(1);
            return response.StatusCode;
        }

        #endregion

        #region Json
        public static object? ToObject(this JsonElement element, Type returnType, JsonSerializerOptions? options = null)
        {
            ArrayBufferWriter<byte>? bufferWriter = new();
            using (Utf8JsonWriter? writer = new(bufferWriter))
            {
                element.WriteTo(writer);
            }

            return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, returnType, options);
        }

        public static T? ToObject<T>(this JsonElement element)
        {
            string? json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json);
        }
        public static T? ToObject<T>(this JsonDocument document)
        {
            string? json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json);
        }
        #endregion

        #region Object

        //https://stackoverflow.com/a/50128943
        public static bool ArePropertiesNotNull<T>(this T obj)
        {
            return PropertyCache<T>.PublicProperties.All(propertyInfo => propertyInfo.GetValue(obj) != null);
        }

        #endregion

        #region Stream
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!source.CanRead)
            {
                throw new ArgumentException("Has to be readable", nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!destination.CanWrite)
            {
                throw new ArgumentException("Has to be writable", nameof(destination));
            }

            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }

        #endregion

        #region String
        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", nameof(value));
            List<int> indexes = new();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        public static Color ToColor(this string str)
        {
            string[]? cleanedColor = str.Replace("rgb(", "").Replace(")", "").Split(",");
            return Color.FromArgb(
                byte.Parse(cleanedColor[0]),
                byte.Parse(cleanedColor[1]),
                byte.Parse(cleanedColor[2])
                );
        }
        #endregion

        #region Type
        //https://stackoverflow.com/a/19317229
        public static bool ImplementsInterface(this Type type, Type ifaceType)
        {
            Type[] intf = type.GetInterfaces();
            for (int i = 0; i < intf.Length; i++)
            {
                if (intf[i] == ifaceType)
                {
                    return true;
                }
            }

            return false;
        }

        //https://stackoverflow.com/a/63069236
        public static TR? Method<TR>(this Type t, string method, object? obj = null, params object[] parameters)
        {
            return (TR?)t.GetMethod(method)?.Invoke(obj, parameters);
        }
        #endregion
    }
}
