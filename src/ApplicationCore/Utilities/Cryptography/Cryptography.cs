using System.Security.Cryptography;
using System.Text;

namespace ApplicationCore.Utilities.Cryptography
{
    public class Cryptography
    {
        private const string AES_IV = "d0d258088b14d29e"; // 16 bits
        //const string key = "12345678900000000000000000000"; // 32 bits

        /// <summary>
        /// algoritmo de cifrado AES
        /// </summary>
        /// <param name = "input"> cadena de texto sin formato </ param>
        /// <param name = "key"> clave (32 bits) </ param>
        /// <devoluciones> cadena </ devuelve>
        public static string EncryptByAES(string input, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.Substring(0, 32));
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = Encoding.UTF8.GetBytes(AES_IV.Substring(0, 16));

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(input);
                        }
                        byte[] bytes = msEncrypt.ToArray();
                        return ByteArrayToHexString(bytes);
                    }
                }
            }
        }

        /// <summary>
        /// descifrado AES
        /// </summary>
        /// <param name = "input"> La matriz de bytes de texto cifrado </ param>
        /// <param name = "key"> clave (32 bits) </ param>
        /// <returns> devuelve la cadena descifrada </ return>
        public static string DecryptByAES(string input, string key)
        {
            byte[] inputBytes = HexStringToByteArray(input);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.Substring(0, 32));
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = Encoding.UTF8.GetBytes(AES_IV.Substring(0, 16));

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream(inputBytes))
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srEncrypt = new StreamReader(csEncrypt))
                        {
                            return srEncrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convierte la cadena hexadecimal especificada en una matriz de bytes
        /// </summary>
        /// <param name = "s"> cadena hexadecimal (como: "7F 2C 4A" o "7F2C4A" está bien) </ param>
        /// <retornos> matriz de bytes correspondiente a la cadena hexadecimal </ devuelve>
        public static byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        /// <summary>
        /// convierte una matriz de bytes en una cadena hexadecimal formateada
        /// </summary>
        /// <param name = "data"> matriz de bytes </ param>
        /// <devoluciones> cadena hexadecimal formateada </ devuelve>
        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                // dígitos hexadecimales
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
                // Los dígitos hexadecimales están separados por espacios
                //sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            return sb.ToString().ToUpper();
        }
    }
}