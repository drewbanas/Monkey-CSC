namespace token
{
    using TokenType = System.String;

    class token
    {
        public const TokenType ILLEGAL = "ILLEGAL";
        public const TokenType EOF = "EOF";

        // Identifiers + literals
        public const TokenType IDENT = "IDENT";   // add, foobar, x, y, ...
        public const TokenType INT = "INT";       // 1343456
        public const TokenType STRING = "STRING"; // "foobar"

        // Operators
        public const TokenType ASSIGN = "=";
        public const TokenType PLUS = "+";
        public const TokenType MINUS = "-";
        public const TokenType BANG = "!";
        public const TokenType ASTERISK = "*";
        public const TokenType SLASH = "/";

        public const TokenType LT = "<";
        public const TokenType GT = ">";

        public const TokenType EQ = "==";
        public const TokenType NOT_EQ = "!=";

        // Delimeters
        public const TokenType COMMA = ",";
        public const TokenType SEMICOLON = ";";
        public const TokenType COLON = ":";

        public const TokenType LPAREN = "(";
        public const TokenType RPAREN = ")";
        public const TokenType LBRACE = "{";
        public const TokenType RBRACE = "}";
        public const TokenType LBRACKET = "[";
        public const TokenType RBRACKET = "]";

        // Keywords
        public const TokenType FUNCTION = "FUNCTION";
        public const TokenType LET = "LET";
        public const TokenType TRUE = "TRUE";
        public const TokenType FALSE = "FALSE";
        public const TokenType IF = "IF";
        public const TokenType ELSE = "ELSE";
        public const TokenType RETURN = "RETURN";

        public struct Token
        {
            public TokenType Type;
            public string Literal;

            public override string ToString()
            {
                return "{Type:" + Type + " Literal:" + Literal + "}";
            }
        }

        static System.Collections.Generic.Dictionary<string, TokenType> keywords =
                new System.Collections.Generic.Dictionary<string, TokenType>
                {
                    {"fn", FUNCTION},
                    {"let", LET},
                    {"true", TRUE},
                    {"false", FALSE},
                    {"if", IF},
                    {"else", ELSE},
                    {"return", RETURN}
                };

        public static TokenType LookupIdent(string ident)
        {
            TokenType tok;
            if (keywords.TryGetValue(ident, out tok))
            {
                return tok;
            }
            return IDENT;
        }
    }
}
