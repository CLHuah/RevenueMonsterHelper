using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

public static class PemKeyHelper
{
    private const string PrivatePemHeader = "-----BEGIN RSA PRIVATE KEY-----";
    private const string PrivatePemFooter = "-----END RSA PRIVATE KEY-----";
    private const string PublicPemHeader = "-----BEGIN PUBLIC KEY-----";
    private const string PublicPemFooter = "-----END PUBLIC KEY-----";

    private const bool Verbose = false;

    public static RSACryptoServiceProvider GetRSAProviderFromPemFile(string pemStr)
    {
        bool isPrivateKeyFile = true;
        pemStr = pemStr.Trim();
        if (pemStr.StartsWith(PublicPemHeader) && pemStr.EndsWith(PublicPemFooter))
            isPrivateKeyFile = false;

        var pemKey = isPrivateKeyFile ? DecodeOpenSSLPrivateKey(pemStr) : DecodeOpenSSLPublicKey(pemStr);

        if (pemKey == null) return null;

        return isPrivateKeyFile ? DecodeRSAPrivateKey(pemKey) : DecodeX509PublicKey(pemKey);
    }

    //--------   Get the binary RSA PUBLIC key   --------
    private static byte[] DecodeOpenSSLPublicKey(string inStr)
    {
        var pemStr = inStr.Trim();
        byte[] binKey;
        if (!pemStr.StartsWith(PublicPemHeader) || !pemStr.EndsWith(PublicPemFooter))
            return null;
        var sb = new StringBuilder(pemStr);
        sb.Replace(PublicPemHeader, "");  //remove headers/footers, if present
        sb.Replace(PublicPemFooter, "");

        var pubStr = sb.ToString().Trim();   //get string after removing leading/trailing whitespace

        try
        {
            binKey = Convert.FromBase64String(pubStr);
        }
        catch (FormatException)
        {       //if can't b64 decode, data is not valid
            return null;
        }
        return binKey;
    }

    private static RSACryptoServiceProvider DecodeX509PublicKey(byte[] x509Key)
    {
        // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
        byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
        // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
        using (var memoryStream = new MemoryStream(x509Key))
        {
            using (var binaryReader = new BinaryReader(memoryStream))    //wrap Memory Stream with BinaryReader for easy reading
            {
                try
                {
                    var twoBytes = binaryReader.ReadUInt16();
                    switch (twoBytes)
                    {
                        case 0x8130:
                            binaryReader.ReadByte();    //advance 1 byte
                            break;
                        case 0x8230:
                            binaryReader.ReadInt16();   //advance 2 bytes
                            break;
                        default:
                            return null;
                    }

                    var seq = binaryReader.ReadBytes(15);
                    if (!CompareByteArrays(seq, seqOid))  //make sure Sequence for OID is correct
                        return null;

                    twoBytes = binaryReader.ReadUInt16();
                    if (twoBytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
                        binaryReader.ReadByte();    //advance 1 byte
                    else if (twoBytes == 0x8203)
                        binaryReader.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    var bt = binaryReader.ReadByte();
                    if (bt != 0x00)     //expect null byte next
                        return null;

                    twoBytes = binaryReader.ReadUInt16();
                    if (twoBytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binaryReader.ReadByte();    //advance 1 byte
                    else if (twoBytes == 0x8230)
                        binaryReader.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    twoBytes = binaryReader.ReadUInt16();
                    byte lowByte = 0x00;
                    byte highByte = 0x00;

                    if (twoBytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                        lowByte = binaryReader.ReadByte();  // read next bytes which is bytes in modulus
                    else if (twoBytes == 0x8202)
                    {
                        highByte = binaryReader.ReadByte(); //advance 2 bytes
                        lowByte = binaryReader.ReadByte();
                    }
                    else
                        return null;
                    byte[] modInt = { lowByte, highByte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                    int modSize = BitConverter.ToInt32(modInt, 0);

                    byte firstByte = binaryReader.ReadByte();
                    binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);

                    if (firstByte == 0x00)
                    {   //if first byte (highest order) of modulus is zero, don't include it
                        binaryReader.ReadByte();    //skip this null byte
                        modSize -= 1;   //reduce modulus buffer size by 1
                    }

                    byte[] modulus = binaryReader.ReadBytes(modSize); //read the modulus bytes

                    if (binaryReader.ReadByte() != 0x02)            //expect an Integer for the exponent data
                        return null;
                    int expBytes = binaryReader.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                    byte[] exponent = binaryReader.ReadBytes(expBytes);

                    // We don't really need to print anything but if we insist to...
                    //showBytes("\nExponent", exponent);
                    //showBytes("\nModulus", modulus);

                    // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
                    RSAParameters rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);
                    return rsa;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }

    //------- Parses binary ans.1 RSA private key; returns RSACryptoServiceProvider  ---
    private static RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privateKey)
    {
        byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;

        // ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
        MemoryStream memoryStream = new MemoryStream(privateKey);
        BinaryReader binaryReader = new BinaryReader(memoryStream);    //wrap Memory Stream with BinaryReader for easy reading
        byte bt = 0;
        ushort twobytes = 0;
        int elems = 0;
        try
        {
            twobytes = binaryReader.ReadUInt16();
            if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                binaryReader.ReadByte();    //advance 1 byte
            else if (twobytes == 0x8230)
                binaryReader.ReadInt16();   //advance 2 bytes
            else
                return null;

            twobytes = binaryReader.ReadUInt16();
            if (twobytes != 0x0102) //version number
                return null;
            bt = binaryReader.ReadByte();
            if (bt != 0x00)
                return null;


            //------  all private key components are Integer sequences ----
            elems = GetIntegerSize(binaryReader);
            MODULUS = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            E = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            D = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            P = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            Q = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            DP = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            DQ = binaryReader.ReadBytes(elems);

            elems = GetIntegerSize(binaryReader);
            IQ = binaryReader.ReadBytes(elems);

            Console.WriteLine("showing components ..");
            if (Verbose)
            {
                ShowBytes("\nModulus", MODULUS);
                ShowBytes("\nExponent", E);
                ShowBytes("\nD", D);
                ShowBytes("\nP", P);
                ShowBytes("\nQ", Q);
                ShowBytes("\nDP", DP);
                ShowBytes("\nDQ", DQ);
                ShowBytes("\nIQ", IQ);
            }

            // ------- create RSACryptoServiceProvider instance and initialize with public key -----
            var rsa = new RSACryptoServiceProvider(2048);
            var rsaParameters = new RSAParameters
            {
                Modulus = MODULUS,
                Exponent = E,
                D = D,
                P = P,
                Q = Q,
                DP = DP,
                DQ = DQ,
                InverseQ = IQ
            };
            rsa.ImportParameters(rsaParameters);
            return rsa;
        }
        catch (Exception)
        {
            return null;
        }
        finally { binaryReader.Close(); }
    }

    private static int GetIntegerSize(BinaryReader binaryReader)
    {
        byte bt = 0;
        byte lowBytes = 0x00;
        byte highBytes = 0x00;
        int count = 0;
        bt = binaryReader.ReadByte();
        if (bt != 0x02) //expect integer
            return 0;
        bt = binaryReader.ReadByte();

        if (bt == 0x81)
            count = binaryReader.ReadByte(); // data size in next byte
        else if (bt == 0x82)
        {
            highBytes = binaryReader.ReadByte(); // data size in next 2 bytes
            lowBytes = binaryReader.ReadByte();
            byte[] modInt = {lowBytes, highBytes, 0x00, 0x00};
            count = BitConverter.ToInt32(modInt, 0);
        }
        else
        {
            count = bt; // we already have the data size
        }

        while (binaryReader.ReadByte() == 0x00)
        {
            //remove high order zeros in data
            count -= 1;
        }

        binaryReader.BaseStream.Seek(-1,
            SeekOrigin.Current); //last ReadByte wasn't a removed zero, so back up a byte
        return count;
    }

    //-----  Get the binary RSA PRIVATE key, decrypting if necessary ----
    private static byte[] DecodeOpenSSLPrivateKey(string instr)
    {
        var pemString = instr.Trim();
        byte[] binKey;
        if (!pemString.StartsWith(PrivatePemHeader) || !pemString.EndsWith(PrivatePemFooter))
            return null;

        var sb = new StringBuilder(pemString);
        sb.Replace(PrivatePemHeader, "");  //remove headers/footers, if present
        sb.Replace(PrivatePemFooter, "");

        var privateKeyString = sb.ToString().Trim();   //get string after removing leading/trailing whitespace

        try
        {        // if there are no PEM encryption info lines, this is an UNencrypted PEM private key
            binKey = Convert.FromBase64String(privateKeyString);
            return binKey;
        }
        catch (System.FormatException)
        {       //if can't b64 decode, it must be an encrypted private key
            //Console.WriteLine("Not an unencrypted OpenSSL PEM private key");  
        }

        StringReader str = new StringReader(privateKeyString);
        //-------- read PEM encryption info. lines and extract salt -----
        if (!str.ReadLine().StartsWith("Proc-Type: 4,ENCRYPTED"))
            return null;
        String saltline = str.ReadLine();
        if (!saltline.StartsWith("DEK-Info: DES-EDE3-CBC,"))
            return null;
        String saltstr = saltline.Substring(saltline.IndexOf(",") + 1).Trim();
        byte[] salt = new byte[saltstr.Length / 2];
        for (int i = 0; i < salt.Length; i++)
            salt[i] = Convert.ToByte(saltstr.Substring(i * 2, 2), 16);
        if (!(str.ReadLine() == ""))
            return null;

        //------ remaining b64 data is encrypted RSA key ----
        String encryptedstr = str.ReadToEnd();

        try
        {   //should have b64 encrypted RSA key now
            binKey = Convert.FromBase64String(encryptedstr);
        }
        catch (System.FormatException)
        {  // bad b64 data.
            return null;
        }

        //------ Get the 3DES 24 byte key using PDK used by OpenSSL ----
        SecureString despswd = GetSecPswd("Enter password to derive 3DES key==>");
        //Console.Write("\nEnter password to derive 3DES key: ");
        //String pswd = Console.ReadLine();
        byte[] deskey = GetOpenSSL3deskey(salt, despswd, 1, 2);    // count=1 (for OpenSSL implementation); 2 iterations to get at least 24 bytes
        if (deskey == null)
            return null;
        //showBytes("3DES key", deskey) ;

        //------ Decrypt the encrypted 3des-encrypted RSA private key ------
        byte[] rsakey = DecryptKey(binKey, deskey, salt); //OpenSSL uses salt value in PEM header also as 3DES IV
        if (rsakey != null)
            return rsakey;  //we have a decrypted RSA private key
        else
        {
            Console.WriteLine("Failed to decrypt RSA private key; probably wrong password.");
            return null;
        }
    }


    // ----- Decrypt the 3DES encrypted RSA private key ----------
    private static byte[] DecryptKey(byte[] cipherData, byte[] desKey, byte[] IV)
    {
        MemoryStream memst = new MemoryStream();
        TripleDES alg = TripleDES.Create();
        alg.Key = desKey;
        alg.IV = IV;
        try
        {
            CryptoStream cs = new CryptoStream(memst, alg.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherData, 0, cipherData.Length);
            cs.Close();
        }
        catch (Exception exc)
        {
            Console.WriteLine(exc.Message);
            return null;
        }
        byte[] decryptedData = memst.ToArray();
        return decryptedData;
    }

    //-----   OpenSSL PBKD uses only one hash cycle (count); miter is number of iterations required to build sufficient bytes ---
    private static byte[] GetOpenSSL3deskey(byte[] salt, SecureString secpswd, int count, int miter)
    {
        IntPtr unmanagedPswd = IntPtr.Zero;
        int HASHLENGTH = 16;    //MD5 bytes
        byte[] keymaterial = new byte[HASHLENGTH * miter];     //to store contatenated Mi hashed results


        byte[] psbytes = new byte[secpswd.Length];
        unmanagedPswd = Marshal.SecureStringToGlobalAllocAnsi(secpswd);
        Marshal.Copy(unmanagedPswd, psbytes, 0, psbytes.Length);
        Marshal.ZeroFreeGlobalAllocAnsi(unmanagedPswd);

        //UTF8Encoding utf8 = new UTF8Encoding();
        //byte[] psbytes = utf8.GetBytes(pswd);

        // --- contatenate salt and pswd bytes into fixed data array ---
        byte[] data00 = new byte[psbytes.Length + salt.Length];
        Array.Copy(psbytes, data00, psbytes.Length);      //copy the pswd bytes
        Array.Copy(salt, 0, data00, psbytes.Length, salt.Length); //concatenate the salt bytes

        // ---- do multi-hashing and contatenate results  D1, D2 ...  into keymaterial bytes ----
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] result = null;
        byte[] hashtarget = new byte[HASHLENGTH + data00.Length];   //fixed length initial hashtarget

        for (int j = 0; j < miter; j++)
        {
            // ----  Now hash consecutively for count times ------
            if (j == 0)
                result = data00;    //initialize 
            else
            {
                Array.Copy(result, hashtarget, result.Length);
                Array.Copy(data00, 0, hashtarget, result.Length, data00.Length);
                result = hashtarget;
                //Console.WriteLine("Updated new initial hash target:") ;
                //showBytes(result) ;
            }

            for (int i = 0; i < count; i++)
                result = md5.ComputeHash(result);
            Array.Copy(result, 0, keymaterial, j * HASHLENGTH, result.Length);  //contatenate to keymaterial
        }
        //showBytes("Final key material", keymaterial);
        byte[] deskey = new byte[24];
        Array.Copy(keymaterial, deskey, deskey.Length);

        Array.Clear(psbytes, 0, psbytes.Length);
        Array.Clear(data00, 0, data00.Length);
        Array.Clear(result, 0, result.Length);
        Array.Clear(hashtarget, 0, hashtarget.Length);
        Array.Clear(keymaterial, 0, keymaterial.Length);

        return deskey;
    }

    private static SecureString GetSecPswd(String prompt)
    {
        SecureString password = new SecureString();

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(prompt);
        Console.ForegroundColor = ConsoleColor.Magenta;

        while (true)
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Enter)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                return password;
            }
            else if (cki.Key == ConsoleKey.Backspace)
            {
                // remove the last asterisk from the screen...
                if (password.Length > 0)
                {
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    Console.Write(" ");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    password.RemoveAt(password.Length - 1);
                }
            }
            else if (cki.Key == ConsoleKey.Escape)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                return password;
            }
            else if (Char.IsLetterOrDigit(cki.KeyChar) || Char.IsSymbol(cki.KeyChar))
            {
                if (password.Length < 20)
                {
                    password.AppendChar(cki.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    Console.Beep();
                }
            }
            else
            {
                Console.Beep();
            }
        }
    }

    private static bool CompareByteArrays(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        var i = 0;
        foreach (var c in a)
        {
            if (c != b[i])
                return false;
            i++;
        }

        return true;
    }

    private static void ShowBytes(string info, byte[] data)
    {
        Console.WriteLine("{0}  [{1} bytes]", info, data.Length);
        for (var i = 1; i <= data.Length; i++)
        {
            Console.Write("{0:X2}  ", data[i - 1]);
            if (i % 16 == 0)
                Console.WriteLine();
        }

        Console.WriteLine("\n\n");
    }
}