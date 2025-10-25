using System;
using System.Security.Cryptography;

var rsa = RSA.Create(2048);
var privateKeyBytes = rsa.ExportRSAPrivateKey();
var base64Key = Convert.ToBase64String(privateKeyBytes);
Console.WriteLine(base64Key);
