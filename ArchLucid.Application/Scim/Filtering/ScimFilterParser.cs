using System.Globalization;

using ArchLucid.Core.Scim.Filtering;

namespace ArchLucid.Application.Scim.Filtering;

/// <summary>Hand-rolled SCIM v2 filter parser (RFC 7644 §3.4.2.2) for flat user attributes only.</summary>
public static class ScimFilterParser
{
    public static ScimFilterNode? Parse(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return null;

        ScimFilterCursor c = new(filter.Trim());
        ScimFilterNode node = c.ParseFilter();
        c.SkipWs();

        if (!c.Eof)
            throw new ScimFilterParseException($"Unexpected trailing input at position {c.Position}.");

        return node;
    }

    private ref struct ScimFilterCursor
    {
        private readonly ReadOnlySpan<char> _s;
        private int _i;

        public ScimFilterCursor(ReadOnlySpan<char> s)
        {
            _s = s;
            _i = 0;
        }

        public int Position
        {
            get => _i;
        }

        public bool Eof
        {
            get => _i >= _s.Length;
        }

        public ScimFilterNode ParseFilter()
        {
            SkipWs();

            if (PeekKeyword("not"))
            {
                ConsumeKeyword("not");
                SkipWs();

                if (!TryConsume('('))
                    throw new ScimFilterParseException("Expected '(' after 'not'.");

                ScimFilterNode inner = ParseFilter();

                if (!TryConsume(')'))
                    throw new ScimFilterParseException("Expected ')' to close 'not'.");

                return new ScimNotNode(inner);
            }

            ScimFilterNode left = ParseTerm();
            SkipWs();

            while (true)
            {
                if (PeekKeyword("and"))
                {
                    ConsumeKeyword("and");
                    SkipWs();
                    ScimFilterNode right = ParseTerm();
                    left = new ScimAndNode(left, right);
                    SkipWs();

                    continue;
                }

                if (PeekKeyword("or"))
                {
                    ConsumeKeyword("or");
                    SkipWs();
                    ScimFilterNode right = ParseTerm();
                    left = new ScimOrNode(left, right);
                    SkipWs();

                    continue;
                }

                break;
            }

            return left;
        }

        private ScimFilterNode ParseTerm()
        {
            SkipWs();

            if (TryConsume('('))
            {
                ScimFilterNode inner = ParseFilter();

                if (!TryConsume(')'))
                    throw new ScimFilterParseException("Expected ')'.");

                return inner;
            }

            return ParseAttrExpression();
        }

        private ScimFilterNode ParseAttrExpression()
        {
            string path = ReadAttributePath();
            SkipWs();

            if (PeekKeyword("pr"))
            {
                ConsumeKeyword("pr");

                return new ScimPresentNode(path);
            }

            string op = ReadCompareOp();
            SkipWs();
            string value = ReadCompareValue();

            return new ScimComparisonNode(path, op, value);
        }

        private string ReadAttributePath()
        {
            if (Eof || (!char.IsLetter(_s[_i]) && _s[_i] != '_'))
                throw new ScimFilterParseException($"Attribute path expected at {Position}.");

            int start = _i;

            while (!Eof)
            {
                char ch = _s[_i];

                if (char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')
                {
                    _i++;

                    continue;
                }

                break;
            }

            return _s[start.._i].ToString();
        }

        private string ReadCompareOp()
        {
            if (_i + 1 >= _s.Length)
                throw new ScimFilterParseException("Compare operator expected.");

            ReadOnlySpan<char> two = _s.Slice(_i, 2);
            string[] ops = ["eq", "ne", "co", "sw", "ew", "gt", "lt", "ge", "le"];

            foreach (string op in ops)
            {
                if (two.Equals(op.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    _i += 2;

                    return op.ToLowerInvariant();
                }
            }

            throw new ScimFilterParseException($"Unknown compare operator at {Position}.");
        }

        private string ReadCompareValue()
        {
            SkipWs();

            if (Eof)
                throw new ScimFilterParseException("Compare value expected.");

            if (_s[_i] == '"')
                return ReadQuotedString();

            int start = _i;

            while (!Eof)
            {
                char ch = _s[_i];

                if (char.IsWhiteSpace(ch) || ch is ')')
                    break;

                _i++;
            }

            return _s[start.._i].ToString();
        }

        private string ReadQuotedString()
        {
            if (!TryConsume('"'))
                throw new ScimFilterParseException("Opening '\"' expected.");

            System.Text.StringBuilder sb = new();

            while (!Eof)
            {
                char ch = _s[_i];

                if (ch == '\\')
                {
                    _i++;

                    if (Eof)
                        throw new ScimFilterParseException("Unterminated escape in string literal.");

                    sb.Append(_s[_i]);
                    _i++;

                    continue;
                }

                if (ch == '"')
                {
                    _i++;

                    return sb.ToString();
                }

                sb.Append(ch);
                _i++;
            }

            throw new ScimFilterParseException("Unterminated string literal.");
        }

        public void SkipWs()
        {
            while (!Eof && char.IsWhiteSpace(_s[_i]))
                _i++;
        }

        private bool TryConsume(char ch)
        {
            SkipWs();

            if (Eof || _s[_i] != ch)
                return false;

            _i++;

            return true;
        }

        private bool PeekKeyword(string word)
        {
            SkipWs();

            if (_i + word.Length > _s.Length)
                return false;

            return _s.Slice(_i, word.Length).Equals(word.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                   (_i + word.Length == _s.Length || !char.IsLetterOrDigit(_s[_i + word.Length]));
        }

        private void ConsumeKeyword(string word)
        {
            if (!PeekKeyword(word))
                throw new ScimFilterParseException($"Expected keyword '{word}' at {Position}.");

            SkipWs();
            _i += word.Length;
        }
    }
}
