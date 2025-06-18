using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using KhoaLuan.Models;

namespace KhoaLuan.Controllers
{
    public class DESMainController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.E = E;
            ViewBag.P = P;
            ViewBag.SBoxes = GetSBoxes();
            return View(new DesInputModel { Action = "Encrypt", InputFormat = "ASCII", KeyFormat = "ASCII" });
        }

        [HttpPost]
        public ActionResult Index(DesInputModel model)
        {
            bool isEncryption = model.Action == "Encrypt";
            string inputFieldName = isEncryption ? "PlainText" : "CipherText";
            string inputValue = isEncryption ? model.PlainText : model.CipherText;

            if (string.IsNullOrEmpty(inputValue))
            {
                ViewBag.ErrorMessage = $"Vui lòng nhập {inputFieldName}!";
                ViewBag.E = E;
                ViewBag.P = P;
                ViewBag.SBoxes = GetSBoxes();
                return View(model);
            }

            if (string.IsNullOrEmpty(model.InputFormat))
            {
                ViewBag.ErrorMessage = "Vui lòng chọn định dạng đầu vào!";
                ViewBag.E = E;
                ViewBag.P = P;
                ViewBag.SBoxes = GetSBoxes();
                return View(model);
            }

            if (string.IsNullOrEmpty(model.KeyFormat))
            {
                ViewBag.ErrorMessage = "Vui lòng chọn định dạng khóa!";
                ViewBag.E = E;
                ViewBag.P = P;
                ViewBag.SBoxes = GetSBoxes();
                return View(model);
            }
            //kiểm tra lựa chọn đầu vào và chuyển đến hàm xử lý dữ liệu
            List<string> binaryBlocks = isEncryption
                ? ConvertInputToBinaryBlocks(model.PlainText, model.InputFormat)
                : ConvertCipherTextToBinaryBlocks(model.CipherText, model.InputFormat);

            if (binaryBlocks == null || binaryBlocks.Count == 0)
            {
                ViewBag.ErrorMessage = isEncryption
                    ? $"PlainText không hợp lệ! Vui lòng nhập đúng định dạng {model.InputFormat}."
                    : $"CipherText không hợp lệ! Vui lòng nhập đúng định dạng {model.InputFormat}.";
                ViewBag.E = E;
                ViewBag.P = P;
                ViewBag.SBoxes = GetSBoxes();
                return View(model);
            }

            string binaryKey = ProcessKey(model.Key, model.KeyFormat);
            if (string.IsNullOrEmpty(binaryKey))
            {
                ViewBag.ErrorMessage = $"Khóa không hợp lệ! Vui lòng nhập đúng định dạng {model.KeyFormat}.";
                ViewBag.E = E;
                ViewBag.P = P;
                ViewBag.SBoxes = GetSBoxes();
                return View(model);
            }

            string[] subKeys = isEncryption
                ? GenerateSubKeys(binaryKey)
                : GenerateSubKeys(binaryKey).Reverse().ToArray();

            var stepDetails = new List<List<string>>();
            var finalOutputs = new List<string>();

            foreach (var block in binaryBlocks)
            {
                var steps = ProcessDesFullRounds(block, subKeys, isEncryption);
                stepDetails.Add(steps);
                string finalOutput = steps.Last().Substring("18. Final Permutation (FP): ".Length);
                finalOutputs.Add(finalOutput);
            }

            string combinedBinaryOutput = string.Join("", finalOutputs);
            string hexOutput = BinaryToHex(combinedBinaryOutput);
            string unicodeOutput = BinaryToUnicode(combinedBinaryOutput);

            ViewBag.Blocks = binaryBlocks;
            ViewBag.Key = binaryKey;
            ViewBag.SubKeys = subKeys;
            ViewBag.StepDetails = stepDetails;
            ViewBag.CombinedBinaryOutput = combinedBinaryOutput;
            ViewBag.HexOutput = hexOutput;
            ViewBag.FinalOutput = unicodeOutput;
            ViewBag.E = E;
            ViewBag.P = P;
            ViewBag.SBoxes = GetSBoxes();
            ViewBag.IsEncryption = isEncryption;

            return View(model);
        }

        private List<string> ConvertInputToBinaryBlocks(string input, string inputFormat)
        {
            if (string.IsNullOrEmpty(input)) return new List<string>();

            string binary = "";
            if (inputFormat == "Binary")
            {
                if (!Regex.IsMatch(input, @"^[01]+$"))
                    return new List<string>();
                binary = input;
            }
            else if (inputFormat == "Hex")
            {
                if (!Regex.IsMatch(input, @"^[0-9A-Fa-f]+$"))
                    return new List<string>();
                try
                {
                    binary = string.Concat(input.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
                }
                catch
                {
                    return new List<string>();
                }
            }
            else if (inputFormat == "ASCII")
            {
                if (!Regex.IsMatch(input, @"^[ -~]+$"))
                    return new List<string>();
                binary = string.Concat(Encoding.ASCII.GetBytes(input)
                    .Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            }
            else
            {
                return new List<string>();
            }

            List<string> blocks = new List<string>();
            int index = 0;
            while (index < binary.Length)
            {
                string block = binary.Substring(index, Math.Min(64, binary.Length - index));
                if (block.Length < 64)
                    block = block.PadRight(64, '0');
                blocks.Add(block);
                index += 64;
            }

            return blocks;
        }

        private List<string> ConvertCipherTextToBinaryBlocks(string cipherText, string inputFormat)
        {
            if (string.IsNullOrEmpty(cipherText)) return new List<string>();

            string binary;
            if (inputFormat == "Binary")
            {
                if (!Regex.IsMatch(cipherText, @"^[01]+$") || cipherText.Length % 64 != 0)
                    return new List<string>();
                binary = cipherText;
            }
            else if (inputFormat == "Hex")
            {
                if (!Regex.IsMatch(cipherText, @"^[0-9A-Fa-f]+$") || cipherText.Length % 2 != 0)
                    return new List<string>();
                try
                {
                    binary = string.Concat(cipherText.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
                }
                catch
                {
                    return new List<string>();
                }
            }
            else
            {
                return new List<string>();
            }

            List<string> blocks = new List<string>();
            int index = 0;
            while (index < binary.Length)
            {
                string block = binary.Substring(index, Math.Min(64, binary.Length - index));
                if (block.Length < 64)
                    block = block.PadRight(64, '0');
                blocks.Add(block);
                index += 64;
            }

            return blocks;
        }

        private string ProcessKey(string keyInput, string keyFormat)
        {
            if (string.IsNullOrEmpty(keyInput)) return null;

            string binaryKey;
            if (keyFormat == "Binary")
            {
                if (!Regex.IsMatch(keyInput, @"^[01]+$") || keyInput.Length > 64)
                    return null;
                binaryKey = keyInput.Length < 64 ? keyInput.PadRight(64, '0') : keyInput;
            }
            else if (keyFormat == "Hex")
            {
                if (!Regex.IsMatch(keyInput, @"^[0-9A-Fa-f]+$") || keyInput.Length > 16)
                    return null;
                try
                {
                    binaryKey = string.Concat(keyInput.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
                    binaryKey = binaryKey.Length < 64 ? binaryKey.PadRight(64, '0') : binaryKey.Substring(0, 64);
                }
                catch
                {
                    return null;
                }
            }
            else if (keyFormat == "ASCII")
            {
                if (!Regex.IsMatch(keyInput, @"^[ -~]+$"))
                    return null;
                binaryKey = string.Concat(Encoding.ASCII.GetBytes(keyInput)
                    .Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                binaryKey = binaryKey.Length < 64 ? binaryKey.PadRight(64, '0') : binaryKey.Substring(0, 64);
            }
            else
            {
                return null;
            }

            return binaryKey;
        }

        private string BinaryToUnicode(string binary)
        {
            try
            {
                int length = binary.Length - (binary.Length % 8);
                if (length == 0) return "[Không thể chuyển đổi thành Unicode]";

                byte[] bytes = new byte[length / 8];
                for (int i = 0; i < length; i += 8)
                {
                    string byteString = binary.Substring(i, 8);
                    bytes[i / 8] = Convert.ToByte(byteString, 2);
                }
                string result = Encoding.ASCII.GetString(bytes);
                if (result.Contains("�"))
                {
                    return "[Kết quả không phải văn bản có nghĩa]";
                }
                return result;
            }
            catch
            {
                return "[Không thể chuyển đổi thành Unicode]";
            }
        }

        private string BinaryToHex(string binary)
        {
            int length = binary.Length - (binary.Length % 4);
            if (length == 0) return "";

            StringBuilder hex = new StringBuilder();
            for (int i = 0; i < length; i += 4)
            {
                string fourBits = binary.Substring(i, 4);
                int value = Convert.ToInt32(fourBits, 2);
                hex.Append(value.ToString("X"));
            }
            return hex.ToString();
        }

        private string[] GenerateSubKeys(string key)
        {
            int[] PC1 = {
                57, 49, 41, 33, 25, 17, 9,
                1, 58, 50, 42, 34, 26, 18,
                10, 2, 59, 51, 43, 35, 27,
                19, 11, 3, 60, 52, 44, 36,
                63, 55, 47, 39, 31, 23, 15,
                7, 62, 54, 46, 38, 30, 22,
                14, 6, 61, 53, 45, 37, 29,
                21, 13, 5, 28, 20, 12, 4
            };
            string permutedKey = string.Concat(PC1.Select(i => key[i - 1]));

            string C = permutedKey.Substring(0, 28);
            string D = permutedKey.Substring(28, 28);

            int[] shifts = { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };
            string[] subKeys = new string[16];

            int[] PC2 = {
                14, 17, 11, 24, 1, 5,
                3, 28, 15, 6, 21, 10,
                23, 19, 12, 4, 26, 8,
                16, 7, 27, 20, 13, 2,
                41, 52, 31, 37, 47, 55,
                30, 40, 51, 45, 33, 48,
                44, 49, 39, 56, 34, 53,
                46, 42, 50, 36, 29, 32
            };

            for (int i = 0; i < 16; i++)
            {
                C = C.Substring(shifts[i]) + C.Substring(0, shifts[i]);
                D = D.Substring(shifts[i]) + D.Substring(0, shifts[i]);
                string combined = C + D;
                subKeys[i] = string.Concat(PC2.Select(j => combined[j - 1]));
            }

            return subKeys;
        }

        private string XOR(string a, string b)
        {
            var result = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
                result.Append(a[i] == b[i] ? '0' : '1');
            return result.ToString();
        }

        private List<string> ProcessDesFullRounds(string block, string[] subKeys, bool isEncryption)
        {
            var steps = new List<string>();

            int[] IP = {
                58, 50, 42, 34, 26, 18, 10, 2,
                60, 52, 44, 36, 28, 20, 12, 4,
                62, 54, 46, 38, 30, 22, 14, 6,
                64, 56, 48, 40, 32, 24, 16, 8,
                57, 49, 41, 33, 25, 17, 9, 1,
                59, 51, 43, 35, 27, 19, 11, 3,
                61, 53, 45, 37, 29, 21, 13, 5,
                63, 55, 47, 39, 31, 23, 15, 7
            };

            string permuted = string.Concat(IP.Select(i => block[i - 1]));
            steps.Add("1. IP: " + permuted);

            string L = permuted.Substring(0, 32);
            string R = permuted.Substring(32, 32);
            steps.Add("2. L0: " + L);
            steps.Add("   R0: " + R);

            for (int round = 0; round < 16; round++)
            {
                int keyIndex = isEncryption ? round + 1 : 16 - round;
                steps.Add($"Round {round + 1}:");

                string expandedR = string.Concat(E.Select(i => R[i - 1]));
                steps.Add($"   {round + 1}.1 Expansion E(R{round}): " + expandedR);

                steps.Add($"   {round + 1}.2 Subkey K{keyIndex}: " + subKeys[round]);

                string xorWithKey = XOR(expandedR, subKeys[round]);
                steps.Add($"   {round + 1}.3 XOR(E(R{round}), K{keyIndex}): " + xorWithKey);

                string sboxOutput = "";
                var sboxes = GetSBoxes();
                for (int i = 0; i < 8; i++)
                {
                    string sixBits = xorWithKey.Substring(i * 6, 6);
                    int row = Convert.ToInt32("" + sixBits[0] + sixBits[5], 2);
                    int col = Convert.ToInt32(sixBits.Substring(1, 4), 2);
                    int val = sboxes[i, row, col];
                    string fourBits = Convert.ToString(val, 2).PadLeft(4, '0');
                    sboxOutput += fourBits;
                    steps.Add($"   {round + 1}.{i + 4} S-box {i + 1} input: {sixBits} → row {row}, col {col} → output: {fourBits}");
                }
                steps.Add($"   {round + 1}.12 S-box output (32 bit): " + sboxOutput);

                string permutedSBoxOutput = string.Concat(P.Select(i => sboxOutput[i - 1]));
                steps.Add($"   {round + 1}.13 P(S-box output): " + permutedSBoxOutput);

                string newR = XOR(permutedSBoxOutput, L);
                steps.Add($"   {round + 1}.14 XOR(P(S-box), L{round}) = R{round + 1}: " + newR);

                L = R;
                R = newR;
                steps.Add($"   {round + 1}.15 L{round + 1} = R{round}: " + L);
            }

            string preFinal = R + L;
            steps.Add("17. Pre-final (R16 || L16): " + preFinal);

            int[] FP = {
                40, 8, 48, 16, 56, 24, 64, 32,
                39, 7, 47, 15, 55, 23, 63, 31,
                38, 6, 46, 14, 54, 22, 62, 30,
                37, 5, 45, 13, 53, 21, 61, 29,
                36, 4, 44, 12, 52, 20, 60, 28,
                35, 3, 43, 11, 51, 19, 59, 27,
                34, 2, 42, 10, 50, 18, 58, 26,
                33, 1, 41, 9, 49, 17, 57, 25
            };
            string finalOutput = string.Concat(FP.Select(i => preFinal[i - 1]));
            steps.Add("18. Final Permutation (FP): " + finalOutput);

            return steps;
        }

        private static readonly int[] E = {
            32, 1, 2, 3, 4, 5,
            4, 5, 6, 7, 8, 9,
            8, 9, 10, 11, 12, 13,
            12, 13, 14, 15, 16, 17,
            16, 17, 18, 19, 20, 21,
            20, 21, 22, 23, 24, 25,
            24, 25, 26, 27, 28, 29,
            28, 29, 30, 31, 32, 1
        };

        private static readonly int[] P = {
            16, 7, 20, 21, 29, 12, 28, 17,
            1, 15, 23, 26, 5, 18, 31, 10,
            2, 8, 24, 14, 32, 27, 3, 9,
            19, 13, 30, 6, 22, 11, 4, 25
        };

        private int[,,] GetSBoxes()
        {
            return new int[8, 4, 16]
            {
                {
                    {14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7},
                    {0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8},
                    {4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0},
                    {15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13}
                },
                {
                    {15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10},
                    {3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5},
                    {0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15},
                    {13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9}
                },
                {
                    {10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8},
                    {13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1},
                    {13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7},
                    {1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12}
                },
                {
                    {7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15},
                    {13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9},
                    {10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4},
                    {3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14}
                },
                {
                    {2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9},
                    {14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6},
                    {4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14},
                    {11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3}
                },
                {
                    {12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11},
                    {10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8},
                    {9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6},
                    {4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13}
                },
                {
                    {4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1},
                    {13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6},
                    {1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2},
                    {6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12}
                },
                {
                    {13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7},
                    {1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2},
                    {7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8},
                    {2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11}
                }
            };
        }
    }
}