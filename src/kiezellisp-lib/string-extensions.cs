#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    public struct Location
    {
        public Location(int b, int e)
        {
            Begin = b;
            End = e;
        }
        public int Begin { get; }
        public int End { get; }
        public int Span { get { return End - Begin; } }
    }

    public class Kwarg
    {
        #region Fields

        public object Value;

        #endregion Fields

        #region Constructors

        public Kwarg(object value)
        {
            Value = value;
        }

        #endregion Constructors
    }

    public class RegexPlus : Regex, IApply, IDynamicMetaObjectProvider
    {
        #region Constructors

        public RegexPlus(string pattern, RegexOptions options)
            : base(pattern, options)
        {
        }

        #endregion Constructors

        #region Private Methods

        object IApply.Apply(object[] args)
        {
            if (args == null || args.Length != 1 || !(args[0] is string))
            {
                throw new ArgumentException();
            }
            var str = (string)args[0];
            return str.RegexMatch(this);
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new GenericApplyMetaObject<RegexPlus>(parameter, this);
        }

        #endregion Private Methods
    }

    public partial class Runtime
    {
        #region Public Methods

        public static object[] RegexBind(Match match)
        {
            if (match.Success)
            {
                var v = new Vector();

                foreach (Group group in match.Groups)
                {
                    v.Add(group.Value);
                }

                return AsArray(v);
            }
            else {
                return new object[0];
            }
        }

        #endregion Public Methods

        #region Other

        public class FunctionMatcher
        {
            #region Fields

            private readonly IApply transform;

            #endregion Fields

            #region Constructors

            public FunctionMatcher(IApply transform)
            {
                this.transform = transform;
            }

            #endregion Constructors

            #region Public Methods

            public string Evaluate(Match match)
            {
                string result = MakeString(transform.Apply(RegexBind(match)));
                return result;
            }

            #endregion Public Methods
        }

        public class StringMatcher
        {
            #region Fields

            private readonly string replacement;

            #endregion Fields

            #region Constructors

            public StringMatcher(string replacement)
            {
                this.replacement = replacement;
            }

            #endregion Constructors

            #region Public Methods

            public string Evaluate(Match match)
            {
                string result = replacement;

                for (var i = 0; i < match.Groups.Count; ++i)
                {
                    string v = match.Groups[i].Value;
                    string t = Symbols.NumberedVariables[i].Name;
                    result = result.Replace(t, v);
                }

                return result;
            }

            #endregion Public Methods
        }

        #endregion Other
    }

    public static class StringExtensions
    {
        #region Private Methods

        static bool WordWrapBreak(char ch)
        {
            return char.IsWhiteSpace(ch);
        }

        #endregion Private Methods

        #region Public Methods

        public static string Capitalize(this string str)
        {
            var chars = new char[str.Length];
            var makeUpper = true;

            for (var i = 0; i < str.Length; ++i)
            {
                char ch = str[i];

                if (char.IsLetterOrDigit(ch))
                {
                    if (makeUpper)
                    {
                        chars[i] = char.ToUpper(ch);
                        makeUpper = false;
                    }
                    else {
                        chars[i] = char.ToLower(ch);
                    }
                }
                else {
                    chars[i] = ch;
                    makeUpper = true;
                }
            }

            var t = new string(chars);
            return t;
        }

        public static string ConvertToExternalLineEndings(this string str)
        {
            if (str.IndexOf('\n') != -1)
            {
                var str2 = str.ConvertToInternalLineEndings();
                if (Environment.NewLine != "\n")
                {
                    return str2.Replace("\n", Environment.NewLine);
                }
                else {
                    return str2;
                }
            }
            else {
                return str;
            }
        }

        public static string ConvertToInternalLineEndings(this string str)
        {
            if (str.IndexOf('\r') != -1)
            {
                return str.Replace("\r\n", "\n");
            }
            else {
                return str;
            }
        }

        public static Regex GetRegex(object pattern)
        {
            if (pattern is string)
            {
                return new Regex((string)pattern);
            }
            else {
                return (Regex)pattern;
            }
        }

        public static string HtmlDecode(this string str)
        {
            return HttpUtility.HtmlDecode(str);
        }

        public static string HtmlEncode(this string str)
        {
            return HttpUtility.HtmlEncode(str);
        }

        public static string Indent(this string text, string prefix)
        {
            var lines = new List<string>(text.Split('\n'));
            var emptyLine = lines.Count > 0 && lines[lines.Count - 1] == "";
            if (emptyLine)
            {
                lines.RemoveAt(lines.Count - 1);
            }
            if (lines.Count != 0)
            {
                var lines2 = lines.Select(s => prefix + s);
                return string.Join("\n", lines2) + (emptyLine ? "\n" : "");
            }
            else {
                return prefix;
            }
        }

        public static string IndentWithLineNumbers(this string text, int lineNumber, int width, string separator)
        {
            var lines = new List<string>(text.Split('\n'));
            var emptyLine = lines.Count > 0 && lines[lines.Count - 1] == "";
            if (emptyLine)
            {
                lines.RemoveAt(lines.Count - 1);
            }
            var output = new StringWriter();

            for (var i = 0; i < lines.Count; ++i)
            {
                var s = (lineNumber + i).ToString().PadLeft(width) + separator + lines[i];
                output.WriteLine(s);
            }

            return output.ToString();
        }

        [Lisp("join")]
        public static string Join(this string separator, IEnumerable seq)
        {
            var buf = new StringWriter();
            var first = true;
            foreach (object item in Runtime.ToIter(seq))
            {
                if (!first)
                {
                    buf.Write(separator);
                }
                buf.Write(Runtime.MakeString(item));
                first = false;
            }
            return buf.ToString();
        }

        public static object JsonDecode(this string str)
        {
            return new JsonDecoder().Decode(str);
        }

        [Extends(typeof(string))]
        public static string JsonEncode(object value)
        {
            return new JsonEncoder().Encode(value);
        }

        public static string LatexEncode(this string s)
        {
            var SpecialChars = "#$%&_^{}";
            var SpecialCharsArray = SpecialChars.ToCharArray();

            s = s.Replace(@"\", @"\backslash ");

            int i = s.IndexOfAny(SpecialCharsArray);

            if (i != -1)
            {
                var buf = new StringBuilder(i > 0 ? s.Substring(0, i) : "");

                for (var j = i; j < s.Length; ++j)
                {
                    if (SpecialChars.IndexOf(s[j]) != -1)
                    {
                        buf.Append(@"\");
                    }
                    buf.Append(s[j]);
                }

                s = buf.ToString();
            }

            return s;
        }

        public static string Left(this string s, int count)
        {
            return s.Substring(0, Math.Min(count, s.Length));
        }

        public static string LispName(this string name)
        {
            var buf = new StringBuilder();
            var prevch = 'A';

            foreach (char ch in name)
            {
                if (char.IsUpper(ch) && char.IsLower(prevch))
                {
                    if (buf.Length == 0 || buf[buf.Length - 1] != '-')
                    {
                        buf.Append('-');
                    }
                    buf.Append(char.ToLower(ch));
                    prevch = ch;
                }
                else if (ch == '_')
                {
                    if (buf.Length == 0 || buf[buf.Length - 1] != '-')
                    {
                        buf.Append('-');
                    }
                    prevch = ch;
                }
                else if (ch == '`')
                {
                    buf.Append('^');
                    prevch = ch;
                }
                else {
                    buf.Append(char.ToLower(ch));
                    prevch = ch;
                }
            }

            return buf.ToString();
        }

        public static string LispToCamelCaseName(this string name)
        {
            var buf = new StringBuilder();
            var toUpper = false;
            foreach (char ch in name)
            {
                if (ch == '-')
                {
                    toUpper = true;
                }
                else if (toUpper)
                {
                    buf.Append(char.ToUpper(ch));
                    toUpper = false;
                }
                else {
                    buf.Append(char.ToLower(ch));
                }
            }
            return buf.ToString();
        }

        public static string LispToPascalCaseName(this string name)
        {
            //if ( name == "" || Char.IsUpper( name, 0 ) )
            //{
            //    return name;
            //}

            var buf = new StringBuilder();
            var toUpper = true;
            foreach (var ch in name)
            {
                if (ch == '-')
                {
                    toUpper = true;
                }
                else if (toUpper)
                {
                    buf.Append(char.ToUpper(ch));
                    toUpper = false;
                }
                else {
                    buf.Append(char.ToLower(ch));
                }
            }
            return buf.ToString();
        }

        public static Cons MakeMatchResult(Match match)
        {
            Cons result = null;
            if (match.Success)
            {
                for (var i = match.Groups.Count - 1; i >= 0; --i)
                {
                    var group = match.Groups[i];
                    result = new Cons(group.Value, result);
                }
            }
            return result;
        }

        public static string MakeWildcardRegexString(string pattern)
        {
            var star = "<<sadaskdSTARadfgjkdlf>>";
            var qm = "<<sdkjlfQUESTIONMARKsdalwe>>";

            var pattern2 = "^" + pattern
                                .Replace("*", star)
                                .Replace("?", qm)
                                .RegexEncode()
                                .Replace(star, "(.*?)")
                                .Replace(qm, "(.*)") + "$";

            return pattern2;
        }

        public static string Next(this string str)
        {
            return Runtime.IncrementString(str);
        }

        public static string PadLeft(this string str, int width, string chr)
        {
            if (string.IsNullOrEmpty(chr))
            {
                return str.PadLeft(width);
            }
            else if (chr.Length == 1)
            {
                return str.PadLeft(width, chr[0]);
            }
            else {
                throw new LispException("Too many padding characters");
            }
        }

        public static string PadRight(this string str, int width, string chr)
        {
            if (string.IsNullOrEmpty(chr))
            {
                return str.PadRight(width);
            }
            else if (chr.Length == 1)
            {
                return str.PadRight(width, chr[0]);
            }
            else {
                throw new LispException("Too many padding characters");
            }
        }

        public static object ParseDate(this string str, params object[] kwargs)
        {
            object val = TryParseDate(str, kwargs);

            if (val == null)
            {
                throw new LispException("Could not convert to date: \"{0}\"", str);
            }

            return val;
        }

        public static object ParseNumber(this string str, params object[] kwargs)
        {
            var result = TryParseNumber(str, kwargs);
            if (result == null)
            {
                throw new LispException("Cannot convert to number: \"{0}\"", str);
            }
            return result;
        }

        public static string Prev(this string str)
        {
            return Runtime.DecrementString(str);
        }

        public static string RegexEncode(this string str)
        {
            return Regex.Escape(str);
        }

        public static Cons RegexMatch(this string str, object pattern)
        {
            Match match = GetRegex(pattern).Match(str);
            return MakeMatchResult(match);
        }

        public static Cons RegexMatchAll(this string str, object pattern)
        {
            var matches = GetRegex(pattern).Matches(str);
            return Runtime.Map(x => MakeMatchResult((Match)x), matches);
        }

        public static string RegexReplace(this string str, object pattern, string replacement)
        {
            var evaluator = new MatchEvaluator(new Runtime.StringMatcher(replacement).Evaluate);
            string val = GetRegex(pattern).Replace(str, evaluator);
            return val;
        }

        public static string RegexReplace(this string str, object pattern, IApply transform)
        {
            var evaluator = new MatchEvaluator(new Runtime.FunctionMatcher(transform).Evaluate);
            string val = GetRegex(pattern).Replace(str, evaluator);
            return val;
        }

        public static string[] RegexSplit(this string str, object pattern)
        {
            return GetRegex(pattern).Split(str);
        }

        public static string Repeat(this string str, int count)
        {
            var buf = new StringBuilder();
            for (var i = 0; i < count; ++i)
            {
                buf.Append(str);
            }
            return buf.ToString();
        }

        public static string Right(this string s, int count)
        {
            return s.Substring(s.Length - Math.Min(count, s.Length));
        }

        public static string Shorten(this string str, int maxLength, string insert = "...")
        {
            int extra = insert == null ? 0 : insert.Length;
            if (str.Length > maxLength)
            {
                return str.Substring(0, maxLength - extra) + insert;
            }
            else {
                return str;
            }
        }

        public static string[] Split(this string str)
        {
            return str.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] Split(this string str, string separators)
        {
            return str.Split(separators.ToCharArray());
        }

        public static string[] Split(this string str, IEnumerable separators)
        {
            var list = new List<string>();
            foreach (object s in separators)
            {
                list.Add(Runtime.MakeString(s));
            }
            return str.Split(list.ToArray(), StringSplitOptions.None);
        }

        public static string[] Split(this string str, int count)
        {
            return str.Split(new char[0], count, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] Split(this string str, string separators, int count)
        {
            return str.Split(separators.ToCharArray(), count);
        }

        public static string[] Split(this string str, IEnumerable separators, int count)
        {
            var list = new List<string>();
            foreach (object s in separators)
            {
                list.Add(Runtime.MakeString(s));
            }
            return str.Split(list.ToArray(), count, StringSplitOptions.None);
        }

        public static string Trim(this string str, string chars)
        {
            return str.Trim(chars.ToCharArray());
        }

        public static string TrimEnd(this string str, string chars)
        {
            return str.TrimEnd(chars.ToCharArray());
        }

        public static string TrimStart(this string str, string chars)
        {
            return str.TrimStart(chars.ToCharArray());
        }

        public static object TryParseDate(this string str, params object[] kwargs)
        {
            object[] args = Runtime.ParseKwargs(kwargs, new string[] { "culture", "format" });
            var culture = Runtime.GetCultureInfo(args[0]);
            var format = (string)args[1];
            DateTime date;
            if (format == null)
            {
                if (DateTime.TryParse(str, culture, 0, out date))
                {
                    return date;
                }
                else {
                    return null;
                }
            }
            else {
                if (DateTime.TryParseExact(str, format, culture, 0, out date))
                {
                    return date;
                }
                else {
                    return null;
                }
            }
        }

        public static object TryParseNumber(this string str, params object[] kwargs)
        {
            object[] args = Runtime.ParseKwargs(kwargs, new string[] { "culture", "base", "decimal-point-is-comma" });
            var culture = Runtime.GetCultureInfo(args[0]);
            var numberBase = Convert.ToInt32(args[1] ?? "0");
            var decimalPointIsComma = Runtime.ToBool(args[2]);
            return Number.TryParse(str, culture, numberBase, decimalPointIsComma);
        }

        public static object TryParseTime(this string str)
        {
            TimeSpan ts;
            if (TimeSpan.TryParse(str, out ts))
            {
                return ts;
            }
            else {
                return null;
            }
        }

        public static string UrlDecode(this string str)
        {
            return HttpUtility.UrlDecode(str);
        }

        public static string UrlEncode(this string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public static Cons WildcardMatch(this string str, string pattern)
        {
            return RegexMatch(str, MakeWildcardRegexString(pattern));
        }

        public static string WordWrap(this string text, int width)
        {
            // Wraps on white space boundary
            var buf = new StringBuilder();
            var pos = 0;
            while (pos + width < text.Length)
            {
                if (WordWrapBreak(text[pos]))
                {
                    ++pos;
                    continue;
                }
                var end = pos + width;
                if (!WordWrapBreak(text[end]))
                {
                    while (!WordWrapBreak(text[end - 1]))
                    {
                        --end;
                        if (end == pos)
                        {
                            end = pos + width;
                            break;
                        }
                    }

                }
                buf.Append(text.Substring(pos, end - pos));
                buf.Append("\n");
                pos = end;
            }
            buf.Append(text.Substring(pos));
            return buf.ToString();
        }

        #endregion Public Methods
    }
}