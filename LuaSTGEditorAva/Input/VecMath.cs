using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;using Avalonia;using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
namespace LuaSTGEditorAva.Input
{
    public sealed class VecRow
    {
        public string X { get; set; } = "";
        public string Y { get; set; } = "";

        public string Display => "(" + X + ", " + Y + ")";
    }

    public static class VecMath
    {
        public static double ScrXToLstgX(double x) => x - 224;
        public static double ScrYToLstgY(double y) => 240 - y;

        public static double ScrXToLstgX(double x, bool? clip)
        {
            if (clip == null) return x - 224;
            if (clip == false) return Convert.ToInt32(x - 224);
            return Convert.ToInt32((x - 224) / 10) * 10;
        }

        public static double ScrYToLstgY(double y, bool? clip)
        {
            if (clip == null) return 240 - y;
            if (clip == false) return Convert.ToInt32(240 - y);
            return Convert.ToInt32((240 - y) / 10) * 10;
        }

        public static double LstgXToScrX(double x) => x + 224;
        public static double LstgYToScrY(double y) => 240 - y;

        public static List<string> SeparatePolynomial(string s)
        {
            try
            {
                List<string> vs = new List<string>();
                int lastlocptr = 0;
                char[] c = s.Trim().ToCharArray();
                Stack<char> expr = new Stack<char>();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == '(' || c[i] == '[' || c[i] == '{')
                    {
                        expr.Push(c[i]);
                    }
                    else if (c[i] == ')' || c[i] == ']' || c[i] == '}')
                    {
                        if (expr.Peek() == '(' && c[i] == ')') expr.Pop();
                        else if (expr.Peek() == '[' && c[i] == ']') expr.Pop();
                        else if (expr.Peek() == '{' && c[i] == '}') expr.Pop();
                        else throw new InvalidOperationException();
                    }
                    else if (c[i] == '+' && i != 0)
                    {
                        if (expr.Count == 0)
                        {
                            vs.Add(new string(c, lastlocptr, i - lastlocptr));
                            lastlocptr = i + 1;
                        }
                    }
                    else if (c[i] == '-' && i != 0)
                    {
                        if (expr.Count == 0)
                        {
                            vs.Add(new string(c, lastlocptr, i - lastlocptr));
                            lastlocptr = i;
                        }
                    }
                }
                vs.Add(new string(c, lastlocptr, c.Length - lastlocptr));
                return vs;
            }
            catch (InvalidOperationException)
            {
                return new List<string>() { s };
            }
        }

        public static string MergeComponent(IList<VecRow> rows, bool isX)
        {
            char[] trimType = new char[] { '+', ' ' };
            int indexlast0s;
            for (indexlast0s = rows.Count - 1; indexlast0s >= 0; indexlast0s--)
            {
                string temp = (isX ? rows[indexlast0s].X : rows[indexlast0s].Y).Trim(trimType);
                if (!string.IsNullOrEmpty(temp) && temp != "0") break;
            }
            string merged = "";
            for (int i = 0; i <= indexlast0s; i++)
            {
                string temp = (isX ? rows[i].X : rows[i].Y).Trim(trimType);
                if (!string.IsNullOrEmpty(temp))
                    merged += temp[0] != '-' ? "+" + temp : temp;
                else
                    merged += "+0";
            }
            return merged.Trim(trimType);
        }

        private static bool IsEmpty(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            try
            {
                if (Convert.ToInt32(s) == 0) return true;
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static bool? SyncDirection(string curX, string curY, bool focusIsY)
        {
            if (IsEmpty(curX)) return IsEmpty(curY) ? (bool?)null : true;
            if (IsEmpty(curY)) return false;
            return focusIsY;
        }

        public static string SyncXY(string from, bool toX)
        {
            if (toX)
            {
                string t = Regex.Replace(from, @"(?<![a-zA-Z])x\b", "____TEMPy___");
                t = Regex.Replace(t, @"(?<![a-zA-Z])y\b", "x");
                return Regex.Replace(t, @"(?<![a-zA-Z])____TEMPy___\b", "y");
            }
            string u = Regex.Replace(from, @"(?<![a-zA-Z])x\b", "____TEMPy___");
            u = Regex.Replace(u, @"(?<![a-zA-Z])y\b", "x");
            return Regex.Replace(u, @"(?<![a-zA-Z])____TEMPy___\b", "y");
        }

        public static string SyncTri(string from, bool toX)
        {
            if (toX)
            {
                string t = Regex.Replace(from, "\\bcos\\(", "____TEMPsin__(");
                t = Regex.Replace(t, "\\bsin\\(", "cos(");
                return Regex.Replace(t, "____TEMPsin__\\(", "sin(");
            }
            string u = Regex.Replace(from, "\\bcos\\(", "____TEMPsin__(");
            u = Regex.Replace(u, "\\bsin\\(", "cos(");
            return Regex.Replace(u, "____TEMPsin__\\(", "sin(");
        }
    }
}
