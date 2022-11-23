using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    public class Hash {
        public static byte[] GetHash(byte[] input) {

            // Convert the input string to a byte array and compute the hash.
            using SHA256 hash = SHA256.Create();
            return hash.ComputeHash(input);
        }

        public static string HashToHex(byte[] data) {
            StringBuilder sBuilder = new();
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

    }
}
